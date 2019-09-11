using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace VSlice
{
    // ******************************************************************************
    /// <summary>
    /// Interface for abstracting an api that can walk a directory structure
    /// </summary>
    // ******************************************************************************
    public interface IDirectoryInfo
    {
        string Name { get; }
        string FullName { get; }
        IItemData[] GetFiles();
        IDirectoryInfo[] GetDirectories();
    }

}
