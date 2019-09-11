using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VSlice.Models
{

    /// -----------------------------------------------------------------------------------
    /// <summary>
    /// This class allows for a more compact way to keep an array of strings.  
    /// Generally should save about 75% memory
    /// 
    /// TODO:
    ///     [ ] The set indexer should generate a new string and new index values
    ///     [ ] Add unit tests
    ///     [ ] Prototype against a string array to see what the cost is
    /// </summary>
    /// -----------------------------------------------------------------------------------
    class DelimitedString
    {
        public int Length => _indexes.Length;
        ushort[] _indexes;
        string _rootString;

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// ctor
        /// </summary>
        /// -----------------------------------------------------------------------------------
        public DelimitedString(string rootString, char delimiter)
        {
            var newString = new StringBuilder();
            var list = new List<ushort>();
            list.Add(0);
            newString.Append(rootString[0]);
            for (ushort i = 1; i < rootString.Length; i++)
            {
                var c = rootString[i];
                if (c == delimiter)
                {
                    i++;
                    list.Add((ushort)newString.Length);
                }
                else
                {
                    newString.Append(c);
                }
            }
            _rootString = newString.ToString();
            _indexes = list.ToArray();
        }

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// Indexer
        /// </summary>
        /// -----------------------------------------------------------------------------------
        public string this[uint key]
        {
            get
            {
                if (key >= _indexes.Length) throw new ArgumentException("Delimited string index out of bounds.");
                var start = _indexes[key];
                var end = _rootString.Length;
                if (key < _indexes.Length - 1)
                {
                    end = _indexes[key + 1];
                }

                if (end == start && key > 0 && start == _indexes[key - 1]) return string.Empty;

                return _rootString.Substring(start, end - start + 1);
            }
        }
    }

}
