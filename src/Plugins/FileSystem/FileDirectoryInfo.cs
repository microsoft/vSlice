using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;

namespace VSlice
{

    // ******************************************************************************
    /// <summary>
    /// Use this instead of a DirectoryInfo Object
    /// </summary>
    // ******************************************************************************
    public class FileDirectoryInfo : IDirectoryInfo
    {
        #region fields and properties

        public string Name { get; private set; }
        public string FullName { get; private set; }
        private List<IItemData> files;
        private List<IItemData> directories;

        #endregion

        public static ColumnInfo ColumnInfo { get; private set; } = new ColumnInfo(new[] { FILESIZE }, new[] { FILETIME, ATTRIBUTES });
        public const string FILESIZE = "FileSize";
        public const string ATTRIBUTES = "Attributes";
        public const string FILETIME = "FileTime";

        // ******************************************************************************
        /// <summary>
        /// String path Constructor
        /// </summary>
        /// <param name="path">Name of the folder to scan</param>
        // ******************************************************************************
        public FileDirectoryInfo(string path)
        {
            Name = Path.GetFileName(path);
            FullName = path;
            ScanDirectory(path, out files, out directories);
        }

        // ******************************************************************************
        /// <summary>
        /// Interface for DirectoryInfo.GetFiles()
        /// </summary>
        // ******************************************************************************
        public IItemData[] GetFiles()
        {
            return files.ToArray();
        }

        // ******************************************************************************
        /// <summary>
        /// Replacement for DirectoryInfo.GetDirectories()
        /// </summary>
        /// <returns>Array of SliceDirectoryInfo objects</returns>
        // ******************************************************************************
        public IDirectoryInfo[] GetDirectories()
        {
            FileDirectoryInfo[] array = new FileDirectoryInfo[directories.Count];
            for (int i = 0; i < directories.Count; i++)
            {
                array[i] = new FileDirectoryInfo(directories[i].FullName);
            }

            return array;
        }


        // ******************************************************************************
        /// <summary>
        /// Scans the current directory for files and other directories
        /// </summary>
        /// <returns>Array of SliceDirectoryInfo objects</returns>
        // ******************************************************************************
        static NativeMethods.WIN32_FIND_DATA findData;
        internal void ScanDirectory(string path, out List<IItemData> newFiles, out List<IItemData> directories)
        {
            directories = new List<IItemData>();
            newFiles = new List<IItemData>();
            string file = Path.Combine(path, "*");
            IntPtr fileHandle = NativeMethods.FindFirstFile(file, out findData);
            if (fileHandle != NativeMethods.FILE_INVALIDHANDLE) // Check for invalid file handle
            {
                do
                {
                    var fileInfo = new ColumnarItemData(Path.Combine(path, findData.cFileName).Split('\\', '/'), ColumnInfo.ColumnLookup);

                    long length64 = findData.nFileSizeHigh;
                    fileInfo.SetValue(FILESIZE, findData.nFileSizeLow + (length64 << 32));

                    long fileTime = ((long)(uint)findData.ftLastWriteTime.dwHighDateTime) << 32;
                    fileTime |= (long)(uint)findData.ftLastWriteTime.dwLowDateTime;
                    fileInfo.SetValue(FILETIME, DateTime.FromFileTimeUtc(fileTime));
                    fileInfo.SetValue(ATTRIBUTES, GetAttributesText( findData.dwFileAttributes));
                    if ((findData.dwFileAttributes & NativeMethods.FILE_ATTRIBUTE_DIRECTORY) == 0)
                    {
                        newFiles.Add(fileInfo);
                    }
                    else if (!findData.cFileName.Equals(".") && 
                        !findData.cFileName.Equals("..") &&
                        (findData.dwFileAttributes & NativeMethods.FILE_ATTRIBUTE_REPARSE_POINT) == 0)
                    {
                        directories.Add(fileInfo);
                    }

                } while (NativeMethods.FindNextFile(fileHandle, out findData));
                NativeMethods.FindClose(fileHandle);
            }
        }

        // ******************************************************************************
        /// <summary>
        /// Stringify the attributes of a file
        /// </summary>
        // ******************************************************************************
        static string GetAttributesText(uint bits)
        {
            var output = new StringBuilder();
            foreach(var bit in Enum.GetValues(typeof(FileAttributes)))
            {
                if((bits & (int)bit) > 0)
                {
                    if (output.Length > 0) output.Append(";");
                    output.Append(bit.ToString());
                }
            }
            return output.ToString();
        }

    }

}
