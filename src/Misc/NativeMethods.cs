using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace VSlice
{
    class NativeMethods
    {
        public static IntPtr FILE_INVALIDHANDLE = new IntPtr(-1);
        public const UInt32 FILE_ATTRIBUTE_HIDDEN = 0x00000002;
        public const UInt32 FILE_ATTRIBUTE_SYSTEM = 0x00000004;
        public const UInt32 FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
        public const UInt32 FILE_ATTRIBUTE_SYSTEM_OR_HIDDEN = FILE_ATTRIBUTE_HIDDEN | FILE_ATTRIBUTE_SYSTEM;
        public const UInt32 FILE_ATTRIBUTE_REPARSE_POINT = 0x00000400;

        // The CharSet must match the CharSet of the corresponding PInvoke signature
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WIN32_FIND_DATA
        {
            public uint dwFileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr FindFirstFile(string lpFileName, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FindNextFile(IntPtr hFindFile, out WIN32_FIND_DATA lpFindFileData);

        [DllImport("kernel32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FindClose(IntPtr hFindFile);
    }
}
