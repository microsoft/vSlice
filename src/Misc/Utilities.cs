using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using IWshRuntimeLibrary;

namespace VSlice
{
    public class Utilities
    {
        public static string GetFileNameFromDropObject(IDataObject dataObject)
        {
            string[] fileNames = (string[])dataObject.GetData("FileNameW");
            string fileName = null;
            if (fileNames != null) fileName = fileNames[0];
            if (fileName == null) return null;
            if (fileName.EndsWith(".lnk"))
            {
                WshShell shell = new WshShell();
                WshShortcut shortcut = (WshShortcut)shell.CreateShortcut(fileName);
                
                fileName = shortcut.TargetPath;
            }
            else if (fileName.EndsWith(".website"))
            {
                fileName = GetUrlFromWebsiteFile(fileName);
            }
            return fileName;
        }

        private static string GetUrlFromWebsiteFile(string fileName)
        {
            string[] contents = System.IO.File.ReadAllLines(fileName);
            foreach (var line in contents)
            {
                if (line.StartsWith("URL=")) return line.Substring(4);
            }

            throw new ApplicationException("No URL Found in " + fileName);
        }
    }
}
