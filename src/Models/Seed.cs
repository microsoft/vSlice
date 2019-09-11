using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VSlice
{
    public class Seed
    {
        /// <summary>
        /// Id value for accessing the seed
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Human readable version 
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The treehandler for this seed
        /// </summary>
        public ITreeHandler TreeHandler {get; set;}
    }
}
