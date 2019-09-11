using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using VSlice;

namespace vSliceUnitTests
{


    [TestClass]
    public class TreeFilterTests
    {
        [TestMethod]
        public void Allow_Works_WithEqualTo()
        {
            var target = new TreeFilter()
            {
                ColumnName = "Text",
                Operator = "==",
                FilterText = "wow!"
            };

            var columnLookup = new Dictionary<string, int>() { { "Text", 0 }, { "Numeric" , 1 }, };
            var data = new string[] { "Wow!", "222.4" };
            var treeItem = new ColumnarItemData(null, columnLookup, data);
            
            // Text matches
            Assert.AreEqual(true, target.ShouldAllow(treeItem));

            target.IsCaseSensitive = true;
            Assert.AreEqual(false, target.ShouldAllow(treeItem));

            target.FilterText = "Wo";
            Assert.AreEqual(false, target.ShouldAllow(treeItem));

            target.ColumnName = "NonExistent";
            target.FilterText = "Wow!";
            Assert.AreEqual(true, target.ShouldAllow(treeItem));

            // Numeric matches
            target.ColumnName = "Numeric";
            target.FilterText = "222.4";
            Assert.AreEqual(true, target.ShouldAllow(treeItem));

            target.FilterText = "222.40";
            Assert.AreEqual(false, target.ShouldAllow(treeItem));

            target.IsValueColumn = true;
            Assert.AreEqual(true, target.ShouldAllow(treeItem));

            target.FilterText = "222.401";
            Assert.AreEqual(false, target.ShouldAllow(treeItem));

            data[1] = "1000";
            target.FilterText = "1000";
            Assert.AreEqual(true, target.ShouldAllow(treeItem));

            // Bad filter value
            target.FilterText = "a";
            Assert.AreEqual(false, target.ShouldAllow(treeItem));
        }

        [TestMethod]
        public void Allow_Works_WithNotEqualTo()
        {
            var target = new TreeFilter()
            {
                ColumnName = "Text",
                Operator = "!=",
                FilterText = "wow!"
            };

            var columnLookup = new Dictionary<string, int>() { { "Text", 0 }, { "Numeric", 1 }, };
            var data = new string[] { "Wow!", "222.4" };
            var treeItem = new ColumnarItemData(null, columnLookup, data);

            // Text matches
            Assert.AreEqual(false, target.ShouldAllow(treeItem));

            target.IsCaseSensitive = true;
            Assert.AreEqual(true, target.ShouldAllow(treeItem));

            target.FilterText = "Wo";
            Assert.AreEqual(true, target.ShouldAllow(treeItem));

            target.ColumnName = "NonExistent";
            target.FilterText = "Wow!";
            Assert.AreEqual(true, target.ShouldAllow(treeItem));

            // Numeric matches
            target.ColumnName = "Numeric";
            target.FilterText = "222.4";
            Assert.AreEqual(false, target.ShouldAllow(treeItem));

            target.FilterText = "222.40";
            Assert.AreEqual(true, target.ShouldAllow(treeItem));

            target.IsValueColumn = true;
            Assert.AreEqual(false, target.ShouldAllow(treeItem));

            target.FilterText = "222.401";
            Assert.AreEqual(true, target.ShouldAllow(treeItem));

            data[1] = "1000";
            target.FilterText = "1000";
            Assert.AreEqual(false, target.ShouldAllow(treeItem));

            // Bad filter value
            target.FilterText = "a";
            Assert.AreEqual(true, target.ShouldAllow(treeItem));

        }

        [TestMethod]
        public void Allow_Works_WithInequalities()
        {
            var target = new TreeFilter()
            {
                ColumnName = "Text",
                Operator = ">",
                FilterText = "wzzz"
            };

            var columnLookup = new Dictionary<string, int>() { { "Text", 0 }, { "Numeric", 1 }, };
            var data = new string[] { "Wow!", "222.4" };
            var treeItem = new ColumnarItemData(null, columnLookup, data);

            // Text matches
            target.Operator = ">"; Assert.AreEqual(false, target.ShouldAllow(treeItem));
            target.Operator = "<"; Assert.AreEqual(true, target.ShouldAllow(treeItem));

            target.IsCaseSensitive = true;
            target.Operator = ">"; Assert.AreEqual(false, target.ShouldAllow(treeItem));
            target.Operator = "<"; Assert.AreEqual(true, target.ShouldAllow(treeItem));

            target.FilterText = "Wow!";
            target.Operator = ">"; Assert.AreEqual(false, target.ShouldAllow(treeItem));
            target.Operator = "<"; Assert.AreEqual(false, target.ShouldAllow(treeItem));

            target.ColumnName = "NonExistent";
            target.Operator = ">"; Assert.AreEqual(true, target.ShouldAllow(treeItem));
            target.Operator = "<"; Assert.AreEqual(true, target.ShouldAllow(treeItem));

            // Numeric matches
            target.ColumnName = "Numeric";
            target.FilterText = "11111";
            target.Operator = ">"; Assert.AreEqual(true, target.ShouldAllow(treeItem));
            target.Operator = "<"; Assert.AreEqual(false, target.ShouldAllow(treeItem));

            target.IsValueColumn = true;
            target.Operator = ">"; Assert.AreEqual(false, target.ShouldAllow(treeItem));
            target.Operator = "<"; Assert.AreEqual(true, target.ShouldAllow(treeItem));

            target.FilterText = "222.401";
            target.Operator = ">"; Assert.AreEqual(false, target.ShouldAllow(treeItem));
            target.Operator = "<"; Assert.AreEqual(true, target.ShouldAllow(treeItem));

            target.FilterText = "222.40";
            target.Operator = ">"; Assert.AreEqual(false, target.ShouldAllow(treeItem));
            target.Operator = "<"; Assert.AreEqual(false, target.ShouldAllow(treeItem));

            // Bad filter value
            target.FilterText = "a";
            Assert.AreEqual(true, target.ShouldAllow(treeItem));
        }


        [TestMethod]
        public void Allow_Works_WithContains()
        {
            var target = new TreeFilter()
            {
                ColumnName = "Text",
                Operator = "Contains",
                FilterText = "OW"
            };

            var columnLookup = new Dictionary<string, int>() { { "Text", 0 }, { "Numeric", 1 }, };
            var data = new string[] { "Wow!", "222.4" };
            var treeItem = new ColumnarItemData(null, columnLookup, data);

            // Text matches
            target.Operator = "Contains";       Assert.AreEqual(true, target.ShouldAllow(treeItem));
            target.Operator = "DoesNotContain"; Assert.AreEqual(false, target.ShouldAllow(treeItem));

            target.IsCaseSensitive = true;
            target.Operator = "Contains"; Assert.AreEqual(false, target.ShouldAllow(treeItem));
            target.Operator = "DoesNotContain"; Assert.AreEqual(true, target.ShouldAllow(treeItem));

            target.FilterText = "ZZ";
            target.Operator = "Contains"; Assert.AreEqual(false, target.ShouldAllow(treeItem));
            target.Operator = "DoesNotContain"; Assert.AreEqual(true, target.ShouldAllow(treeItem));

            target.IsCaseSensitive = false;
            target.Operator = "Contains"; Assert.AreEqual(false, target.ShouldAllow(treeItem));
            target.Operator = "DoesNotContain"; Assert.AreEqual(true, target.ShouldAllow(treeItem));

            target.FilterText = "!";
            target.Operator = "Contains"; Assert.AreEqual(true, target.ShouldAllow(treeItem));
            target.Operator = "DoesNotContain"; Assert.AreEqual(false, target.ShouldAllow(treeItem));
            target.FilterText = "W";
            target.Operator = "Contains"; Assert.AreEqual(true, target.ShouldAllow(treeItem));
            target.Operator = "DoesNotContain"; Assert.AreEqual(false, target.ShouldAllow(treeItem));
            target.FilterText = "Wow!";
            target.Operator = "Contains"; Assert.AreEqual(true, target.ShouldAllow(treeItem));
            target.Operator = "DoesNotContain"; Assert.AreEqual(false, target.ShouldAllow(treeItem));

            target.ColumnName = "NonExistent";
            target.Operator = "Contains"; Assert.AreEqual(true, target.ShouldAllow(treeItem));
            target.Operator = "DoesNotContain"; Assert.AreEqual(true, target.ShouldAllow(treeItem));

            // Numeric matches
            target.ColumnName = "Numeric";
            target.FilterText = "2.";
            target.IsValueColumn = true;
            target.Operator = "Contains"; Assert.AreEqual(true, target.ShouldAllow(treeItem));
            target.Operator = "DoesNotContain"; Assert.AreEqual(false, target.ShouldAllow(treeItem));

            // Non numeric filter values should still work (because we sometimes mistcategorize columns)
            target.FilterText = "a";
            target.Operator = "Contains"; Assert.AreEqual(false, target.ShouldAllow(treeItem));
            target.Operator = "DoesNotContain"; Assert.AreEqual(true, target.ShouldAllow(treeItem));
        }

        [TestMethod]
        public void Allow_Works_WithRegex()
        {
            var target = new TreeFilter()
            {
                ColumnName = "Text",
                Operator = "Regex",
                FilterText = "[bu]+LES"
            };

            var columnLookup = new Dictionary<string, int>() { { "Text", 0 }, { "Numeric", 1 }, };
            var data = new string[] { "BUBBLES!", "222.4" };
            var treeItem = new ColumnarItemData(null, columnLookup, data);

            // Text matches
            Assert.AreEqual(true, target.ShouldAllow(treeItem));

            target.IsCaseSensitive = true;
            Assert.AreEqual(false, target.ShouldAllow(treeItem));

            target.FilterText = "blah";
            Assert.AreEqual(false, target.ShouldAllow(treeItem));

            target.ColumnName = "NonExistent";
            target.FilterText = "Wow!";
            Assert.AreEqual(true, target.ShouldAllow(treeItem));

            // Numeric matches
            target.ColumnName = "Numeric";
            target.FilterText = "2+\\.4";
            target.IsValueColumn = true;
            Assert.AreEqual(true, target.ShouldAllow(treeItem));
            
            // Bad filter value
            target.FilterText = "2(";
            Assert.AreEqual(true, target.ShouldAllow(treeItem));
        }

        [TestMethod]
        public void Allow_Works_WithBeforeAndAfter()
        {
            var target = new TreeFilter()
            {
                ColumnName = "Date",
                FilterText = "2018/10/11"
            };

            var columnLookup = new Dictionary<string, int>() { { "Date", 0 }, { "NonDate", 1 }, };
            var data = new string[] { "2018/10/10", "2ssf3322.4" };
            var treeItem = new ColumnarItemData(null, columnLookup, data);

            target.Operator = "BeforeDate"; Assert.AreEqual(true, target.ShouldAllow(treeItem));
            target.Operator = "AfterDate"; Assert.AreEqual(false, target.ShouldAllow(treeItem));

            // Non-date
            target.ColumnName = "NonDate";
            target.Operator = "BeforeDate"; Assert.AreEqual(true, target.ShouldAllow(treeItem));
            target.Operator = "AfterDate"; Assert.AreEqual(true, target.ShouldAllow(treeItem));

            // NonExisting value
            target.ColumnName = "bbbbbbbbbb";
            target.Operator = "BeforeDate"; Assert.AreEqual(true, target.ShouldAllow(treeItem));
            target.Operator = "AfterDate"; Assert.AreEqual(true, target.ShouldAllow(treeItem));

            // Bad filter value
            target.FilterText = "dfdfdf";
            target.Operator = "BeforeDate"; Assert.AreEqual(true, target.ShouldAllow(treeItem));
            target.Operator = "AfterDate"; Assert.AreEqual(true, target.ShouldAllow(treeItem));
        }


    }
}
