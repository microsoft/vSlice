using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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

        public static ColumnInfo Columns { get; private set; } = new ColumnInfo(new[] { ITEMSIZE }, null);
        public const string ITEMSIZE = "ItemSize";

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
            folderTable.Columns.Add("MessageClass");
            folderTable.Columns.Add("Subject");
            folderTable.Columns.Add("Size");
            while (!folderTable.EndOfTable)
            {
                Outlook.Row row = folderTable.GetNextRow();
                if (row["subject"] == null) continue;

                var newItem = new ColumnarItemData(new string[] { row["Subject"].ToString() }, Columns.ColumnLookup);
                newItem.SetValue(ITEMSIZE, (int)row["Size"]);
                outputFiles.Add(newItem);
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
                catch(Exception e)
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
            catch (Exception)
            {
                ConnectionMode = Outlook.OlExchangeConnectionMode.olNoExchange;
            }
        }

        internal static Dictionary<string, string> GetStoreIdsAndNames()
        {
            if(oOutlook == null || ConnectionMode == Outlook.OlExchangeConnectionMode.olNoExchange) return null;

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
