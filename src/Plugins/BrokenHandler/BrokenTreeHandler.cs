using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace VSlice
{
    /// -----------------------------------------------------------------------------------
    /// <summary>
    /// Simple handler for the a broken tree
    /// </summary>
    /// -----------------------------------------------------------------------------------
    class BrokenTreeHandler : BaseTreeHandler
    {
        public override ColumnInfo Columns => new ColumnInfo(null, null);

        public BrokenTreeHandler(string name) : base(null, StandardColumns.COUNT)
        {
            Root = new BrokenItem(name, name);
            CurrentItemLabel = "N/A";
        }

        public override IEnumerable<Seed> GetSeeds()
        {
            List<Seed> seeds = new List<Seed>();
            seeds.Add(new Seed { Id = null, Name = "[" + Root.Name + "]", TreeHandler = this });
            return seeds;
        }

        public override void ValidateSeed(Seed seed) { }

        public override Func<ITreeItem> GetCancelableScanFunction(Seed seed, CancellationToken token)
        {
            return () => null;
        }
    }
}
