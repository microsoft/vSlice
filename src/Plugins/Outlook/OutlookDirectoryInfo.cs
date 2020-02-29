using Microsoft.Office.Interop.Outlook;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Outlook = Microsoft.Office.Interop.Outlook;

namespace VSlice
{
    public class OutlookDirectoryInfo : IDirectoryInfo
    {
        static Outlook.Application oOutlook;
        static Outlook.NameSpace oNameSpace;
        public static Outlook.OlExchangeConnectionMode ConnectionMode { get; private set; }

        public string Name { get; private set; }
        public string FullName { get; private set; }

        private Outlook.Folder folder;

        public static ColumnInfo Columns { get; private set; } = new ColumnInfo(new[] { ITEMSIZE }, new[] { ITEMDATE });
        public const string ITEMSIZE = "ItemSize";
        public const string ITEMDATE = "ItemDate";

        public OutlookDirectoryInfo(string seed)
        {
            Outlook.Store store = oOutlook.Session.GetStoreFromID(seed);
            folder = (Outlook.Folder) store.GetRootFolder();
            Name = folder.Name;
            FullName = folder.FullFolderPath;
        }

        private OutlookDirectoryInfo()
        {
            
        }

        public IItemData[] GetFiles()
        {
            var outputFiles = new List<IItemData>();

            Outlook.Table folderTable = folder.GetTable("", Outlook.OlTableContents.olUserItems);
            folderTable.Columns.RemoveAll();
            // For property names, see: https://stackoverflow.com/questions/50576645/outlook-mapi-message-class-metadata-in-outlook-2016
            // Open Outlook script editor in developer mode and Press F2 to browse classes and fields
            // https://docs.microsoft.com/en-us/office/vba/outlook/concepts/forms/outlook-fields-and-equivalent-properties
            folderTable.Columns.Add("MessageClass");
            folderTable.Columns.Add("Subject");
            folderTable.Columns.Add("Size");
            folderTable.Columns.Add("LastModificationTime");
            while (!folderTable.EndOfTable)
            {
                try
                {              
                    Outlook.Row row = folderTable.GetNextRow();
                    if (row["subject"] == null) continue;
                    var messageClass = row["MessageClass"].ToString();

                    var pathParts = new List<string>( this.FullName.Split(new char[] { '/', '\\' }));
                    pathParts.Add(row["Subject"].ToString());
                    var newItem = new ColumnarItemData(pathParts.ToArray(), Columns.ColumnLookup);
                    newItem.SetValue(ITEMSIZE, (int)row["Size"]);
                    var lastModificationTime = row["LastModificationTime"];
                    newItem.SetValue(ITEMDATE, lastModificationTime.ToString());
                    outputFiles.Add(newItem);

                }
                catch(System.Exception e)
                {
                    Debug.WriteLine("Mesage Error: " + e.Message);
                }
            }

            return outputFiles.ToArray();
        }

        public IDirectoryInfo[] GetDirectories()
        {
            List<OutlookDirectoryInfo> outputDirectories = new List<OutlookDirectoryInfo>();

            foreach (Outlook.Folder subFolder in folder.Folders)
            {
                try
                {
                    OutlookDirectoryInfo newDir = new OutlookDirectoryInfo();
                    newDir.folder = subFolder;
                    newDir.Name = subFolder.Name;
                    newDir.FullName = subFolder.FullFolderPath;

                    outputDirectories.Add(newDir);
                }
                catch(System.Exception e)
                {
                    Debug.WriteLine(e.ToString());

#if DEBUG
                    Debugger.Break();
#endif

                }
            }
            return outputDirectories.ToArray();
        }

        internal static void Init()
        {
            try
            {
                oOutlook = new Outlook.Application();
                oNameSpace = oOutlook.GetNamespace("MAPI");
                ConnectionMode = oNameSpace.ExchangeConnectionMode;
            }
            catch (System.Exception)
            {
                ConnectionMode = Outlook.OlExchangeConnectionMode.olNoExchange;
            }
        }

        internal static Dictionary<string, string> GetStoreIdsAndNames()
        {
            if (oOutlook == null) return null; // || ConnectionMode == Outlook.OlExchangeConnectionMode.olNoExchange) return null;

            Dictionary<string, string> storeIdsAndNames = new Dictionary<string, string>();
            Outlook.Stores stores = oOutlook.Session.Stores;
            foreach (Outlook.Store store in stores)
            {
                if (!storeIdsAndNames.ContainsKey(store.StoreID))
                {
                    storeIdsAndNames.Add(store.StoreID, store.DisplayName);
                }
            }

            return storeIdsAndNames;

        }
    }
}
