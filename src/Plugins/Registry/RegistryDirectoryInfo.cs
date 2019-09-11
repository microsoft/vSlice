using System.Diagnostics;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Security;
using System.Text;

namespace VSlice
{
    // ******************************************************************************
    /// <summary>
    /// This is an abstact of the Microsoft.win32.registryKey class
    /// </summary>
    // ******************************************************************************
    public class RegistryDirectoryInfo : IDirectoryInfo
    {
        #region fields and properties

        public string Name { get; private set; }
        public string FullName { get; private set; }
        private List<IItemData> values;
        private List<IItemData> keys;

        #endregion

        // ******************************************************************************
        /// <summary>
        /// String path Constructor
        /// </summary>
        /// <param name="path">Name of the RegistryKey to scan</param>
        // ******************************************************************************
        public RegistryDirectoryInfo(string path)
        {
            string[] test = path.Split('\\');
            Name = test[test.Length - 1];
            FullName = path;
            ScanRegistry(path, out values, out keys);
        }

        // ******************************************************************************
        /// <summary>
        /// Interface for RegistryKey.GetValues()
        /// </summary>
        // ******************************************************************************
        public IItemData[] GetFiles()
        {
            return values.ToArray();
        }

        // ******************************************************************************
        /// <summary>
        /// Replacement for RegistryKey.GetKeys()
        /// </summary>
        /// <returns>Array of SliceDirectoryInfo objects</returns>
        // ******************************************************************************
        public IDirectoryInfo[] GetDirectories()
        {
            RegistryDirectoryInfo[] array = new RegistryDirectoryInfo[keys.Count];
            for (int i = 0; i < keys.Count; i++)
            {
                array[i] = new RegistryDirectoryInfo(keys[i].FullName);
            }

            return array;
        }

        public static ColumnInfo Columns { get; private set; } = new ColumnInfo(new[] { ITEMSIZE }, new [] { "ItemType" });
        public const string ITEMSIZE = "ItemSize";

        // ******************************************************************************
        /// <summary>
        /// Scans a registry hive for subkeys and Values.
        /// </summary>
        /// <returns>Array of SliceDirectoryInfo objects</returns>
        // ******************************************************************************
        internal void ScanRegistry(string path, out List<IItemData> newValues, out List<IItemData> newKeys)
        {
            newKeys = new List<IItemData>();
            newValues = new List<IItemData>();

            try
            {
                string delimStr = "\\";
                char[] delimiter = delimStr.ToCharArray();
                string[] rootPathPair = path.Split(delimiter, 2);
                string[] registryValueList = null;

                RegistryKey currentKey = null;

                //Connect to one of the well known registry roots.
                switch (rootPathPair[0])
                {
                    case "ClassesRoot":
                        currentKey = Registry.ClassesRoot;
                        break;
                    case "CurrentUser":
                        currentKey = Registry.CurrentUser;
                        break;
                    case "LocalMachine":
                        currentKey = Registry.LocalMachine;
                        break;
                    case "Users":
                        currentKey = Registry.Users;
                        break;
                    case "PerformanceData":
                        currentKey = Registry.PerformanceData;
                        break;
                    case "CurrentConfig":
                        currentKey = Registry.CurrentConfig;
                        break;
                }

                //Using a string path for the registry Hive\Key is nice in that it mimics the same format of the 
                //FileTreehanlder, but does cause a small problem for processing the information.  If we are at the
                //Root, all we need to do is open the key and the rooPathPair will contain exactly 1 string.
                //If there is a second string in the pair, then that is the Subkey we want to open.
                if (rootPathPair.Length > 1)
                {
                    currentKey = currentKey.OpenSubKey(rootPathPair[1]);
                }

                registryValueList = currentKey.GetValueNames();
                foreach (string currentRegistryValue in registryValueList)
                {
                    var fullName = string.Format("{0}\\{1}", path, currentRegistryValue.ToString());
                    var reginfo = new ColumnarItemData(fullName.Split('\\'), Columns.ColumnLookup);
                    var itemType = currentKey.GetValueKind(currentRegistryValue).ToString();
                    reginfo.SetValue("ItemType", itemType );

                    //The calculation for the size of the registry value depends on the TYPE of the 
                    //Value.
                    reginfo.SetValue(ITEMSIZE, 0);
                    switch (itemType)
                    {
                        case "Binary":
                            byte[] bytes = (byte[])currentKey.GetValue(currentRegistryValue);
                            reginfo.SetValue(ITEMSIZE, bytes.Length);
                            break;
                        case "DWord":
                            reginfo.SetValue(ITEMSIZE, sizeof(UInt32));
                            break;
                        case "QWord":
                            reginfo.SetValue(ITEMSIZE, sizeof(UInt64));
                            break;
                        //For the registry String object types we need to add 1 to the length of the string
                        //(accounting for the \n char) to get the true size in bytes of the string.
                        case "String":
                            string valueSz = (string)currentKey.GetValue(currentRegistryValue);
                            reginfo.SetValue(ITEMSIZE, (valueSz.Length + 1) * sizeof(char));
                            break;
                        case "ExpandString":
                            string value = (string)currentKey.GetValue(currentRegistryValue);
                            reginfo.SetValue(ITEMSIZE, (value.Length + 1) * sizeof(char));
                            break;
                        case "MultiString":
                            string[] multiString = (string[])currentKey.GetValue(currentRegistryValue);
                            foreach (string sz in multiString)
                            {
                                reginfo.SetValue(ITEMSIZE, (sz.Length + 1) * sizeof(char));
                            }
                            break;
                        //The type of Unknown & None represent 0 length binary values.
                        case "Unknown":
                        case "None":
                            break;
                        default:
                            Debugger.Break();
                            break;
                    }
                    newValues.Add(reginfo);
                }

                string[] RegSubKeys = currentKey.GetSubKeyNames();
                foreach (string regSubKey in RegSubKeys)
                {
                    var valueinfo = new ColumnarItemData(string.Format("{0}\\{1}", path, regSubKey).Split('\\', '/'), Columns.ColumnLookup);
                    newKeys.Add(valueinfo);
                }
                currentKey.Close();
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error: " + e.Message);
            }
        }
    }

}
