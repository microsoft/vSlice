using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading.Tasks;

namespace VSlice
{
    public abstract class BaseTreeItem: ITreeItem
    {
        public virtual string FullName { get; set; } = "";

        public virtual string Name { get; private set; } = "";

        public virtual double ContentValue { get; protected set; }
        public virtual double TotalValue { get; protected set; }
        public virtual double HeatmapContentValue { get; protected set; }
        public virtual double HeatmapTotalValue { get; protected set; }
        public virtual int TotalItemCount { get; protected set; }
        public virtual int LocalItemCount { get; protected set; }

        public virtual List<ITreeItem> Children { get; private set; } = new List<ITreeItem>();

        public virtual Dictionary<string, IItemData> Content { get; private set; } = new Dictionary<string, IItemData>();

        public virtual ITreeItem Parent { get; set; }

        public int? HeatmapBucket { get; private set; }

        // ******************************************************************************
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="file"></param>
        // ******************************************************************************
        public BaseTreeItem(string name, string fullName) 
        {
            Name = name;
            FullName = fullName;
        }

        /// ---------------------------------------------------------------------------
        /// <summary>
        /// Add some regular content
        /// </summary>
        /// ---------------------------------------------------------------------------
        public virtual void AddContent(string name, IItemData data)
        {
            if(Content.ContainsKey(name))
            {
                Debug.WriteLine("WARNING: duplicate content: " + name);
            }
            Content[name] = data;
        }

        /// ---------------------------------------------------------------------------
        /// <summary>
        /// 
        /// </summary>
        /// ---------------------------------------------------------------------------
        public virtual void AddChild(ITreeItem child)
        {
            child.Parent = this;
            TotalValue += child.TotalValue;
            Children.Add(child);
        }

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// recalculate the size of this node based on the value name
        /// </summary>
        /// -----------------------------------------------------------------------------------
        public virtual void Recalculate(string valueColumnName, string heatmapColumnName, TreeFilter[] filters)
        {
            var output = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var queue = new Queue<BaseTreeItem>();
            queue.Enqueue(this);

            CollectDescendents(queue);

            var dirs = new List<BaseTreeItem>();
            Parallel.ForEach(queue, (item) =>
            {
                item.CountStats(valueColumnName, heatmapColumnName, filters);
                item.HeatmapBucket = null;
                lock (dirs)
                {
                    dirs.Add(item);
                }
            });


            Coallate();

            if (heatmapColumnName != null)
            {
                // Figure out histogram buckets for heatmap
                int leftPointer = 0;
                int rightPointer = dirs.Count - 1;
                double bucketSize = dirs.Count / 100.0;

                dirs.Sort((x, y) => x.HeatmapContentValue.CompareTo(y.HeatmapContentValue));
                while (leftPointer < rightPointer)
                {
                    // Start from the left so that if the repo is dominated by '0' values, the 
                    // chart is dark.
                    var leftBucket = (int)(leftPointer / bucketSize);
                    var maxLeftIndex = Math.Min((int)((leftBucket + 1) * bucketSize), rightPointer);
                    while (leftPointer < maxLeftIndex)
                    {
                        var currentValue = dirs[leftPointer].HeatmapContentValue;
                        // Force all items of the same value to be in the same bucket
                        while (dirs[leftPointer].HeatmapContentValue == currentValue)
                        {
                            dirs[leftPointer].HeatmapBucket = leftBucket;
                            leftPointer++;
                            if (leftPointer > rightPointer) break;
                        }
                    }

                    // Now check from the right so that the highest value items are 
                    // maximum hotness
                    var rightBucket = (int)(rightPointer / bucketSize);
                    var minRightIndex = Math.Max((int)((rightBucket - 1) * bucketSize), leftPointer);
                    while (rightPointer > minRightIndex)
                    {
                        var currentValue = dirs[rightPointer].HeatmapContentValue;
                        // Force all items of the same value to be in the same bucket
                        while (dirs[rightPointer].HeatmapContentValue == currentValue)
                        {
                            dirs[rightPointer].HeatmapBucket = rightBucket;
                            rightPointer--;
                            if (leftPointer > rightPointer) break;

                        }
                    }
                }
            }
        }

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// Count up the numbers for this item
        /// </summary>
        /// -----------------------------------------------------------------------------------
        private void Coallate()
        {
            foreach (BaseTreeItem child in Children)
            {
                child.Coallate();
                TotalValue += child.TotalValue;
                HeatmapTotalValue += child.HeatmapTotalValue;
                TotalItemCount += child.TotalItemCount;
            }
        }

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// Count up the numbers for this item
        /// </summary>
        /// -----------------------------------------------------------------------------------
        private void CountStats(string valueColumnName, string heatmapColumnName, TreeFilter[] filters)
        {
            ContentValue = TotalValue = HeatmapContentValue = HeatmapTotalValue = 0;
            TotalItemCount = 0;
            foreach(var item in Content.Values)
            {
                if(filters != null)
                {
                    bool shouldAllow = true;
                    foreach(var filter in filters)
                    {
                        if(!filter.ShouldAllow(item))
                        {
                            shouldAllow = false;
                            break ;
                        }
                    }
                    if (!shouldAllow) continue;
                }
                var itemValue = Math.Abs(item.GetValue(valueColumnName));
                ContentValue += itemValue;
                TotalValue += itemValue;

                if(!string.IsNullOrWhiteSpace(heatmapColumnName))
                {
                    var heatmapValue = item.GetValue(heatmapColumnName);
                    HeatmapContentValue += heatmapValue;
                    HeatmapTotalValue += heatmapValue;

                }
                TotalItemCount++;
                LocalItemCount++;
            }
        }

        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// Add all children recursively to the queue
        /// </summary>
        /// -----------------------------------------------------------------------------------
        private void CollectDescendents(Queue<BaseTreeItem> queue)
        {
            foreach (BaseTreeItem child in Children)
            {
                queue.Enqueue(child);
                child.CollectDescendents(queue);
            }
        }


        /// -----------------------------------------------------------------------------------
        /// <summary>
        /// recalculate the size of this node based on the value name
        /// </summary>
        /// -----------------------------------------------------------------------------------
        public string[] GetCommonValues(string columnName)
        {
            var output = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var queue = new Queue<ITreeItem>();
            queue.Enqueue(this);

            while(queue.Count > 0)
            {
                var treeSpot = queue.Dequeue();
                foreach (var child in treeSpot.Children)
                {
                    queue.Enqueue(child);
                }

                foreach(var item in treeSpot.Content.Values)
                {
                    var data = item.GetText(columnName);
                    if (string.IsNullOrWhiteSpace(data)
                        || output.Contains(data))
                    {
                        continue;
                    }
                    output.Add(data);
                    if (output.Count > 50) return new string[0];
                }

            }

            return output.OrderBy(i => i).ToArray();
        }


        /// ---------------------------------------------------------------------------
        /// <summary>
        /// These need to be implemented by all base classes
        /// </summary>
        /// ---------------------------------------------------------------------------
        public virtual void DoShiftClick() { }
        public virtual void DoCtrlClick() { }
        public virtual void DoAltClick() { }

        public virtual string ClickInstructions { get; protected set; }

        #region IComparable Members

        /// ---------------------------------------------------------------------------
        /// <summary>
        /// Compare this tree item to another
        /// </summary>
        /// ---------------------------------------------------------------------------
        public int CompareTo(ITreeItem compareMe)
        {
            if (this.TotalValue == compareMe.TotalValue) return 0;
            return this.TotalValue > compareMe.TotalValue ? -1 : 1;
        }
        #endregion
    }
}
