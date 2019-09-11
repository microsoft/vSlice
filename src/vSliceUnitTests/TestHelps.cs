using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestHelps
{
    //--------------------------------------------------------------------------------
    /// <summary>
    /// Useful extentions to Assert
    /// </summary>
    //--------------------------------------------------------------------------------
    public class AssertEx
    {
        //--------------------------------------------------------------------------------
        /// <summary>
        /// Check all the public properties on expected object and compare to 
        /// same name properties on the actual object.
        /// </summary>
        //--------------------------------------------------------------------------------
        internal static void PropertiesMatch(object expected, object actual)
        {
            foreach (var expectedProperty in expected.GetType().GetProperties())
            {
                var actualProperty = actual.GetType().GetProperty(expectedProperty.Name);
                Assert.IsNotNull(actualProperty, $"Object did not have property {expectedProperty.Name}");
                Assert.AreEqual(
                    expectedProperty.GetValue(expected),
                    actualProperty.GetValue(actual),
                    $"Property '{expectedProperty.Name}' did not match.");
            }
        }

        //--------------------------------------------------------------------------------
        /// <summary>
        /// An override to the common AreEqual that redirects to better versions for
        /// certain types.
        /// </summary>
        //--------------------------------------------------------------------------------
        public static void AreEqual(object expected, object actual, string message = "")
        {
            if (expected == null || actual == null)
            {
                Assert.AreEqual(expected, actual, message);
                return;
            }

            var expectedType = expected.GetType();
            var actualType = actual.GetType();

            switch (expectedType.Name)
            {
                case "String":
                    AreStringsEqual((string)expected, (string)actual, message);
                    break;
            }

            var expectedAsEnumerable = expected as IEnumerable;
            var actualAsEnumberable = actual as IEnumerable;

            if (expectedAsEnumerable != null && actualAsEnumberable != null)
            {
                AreEnumerablesEqual(expectedAsEnumerable, actualAsEnumberable, message);
                return;
            }

            if (expectedType.Name != actualType.Name)
            {
                Assert.Fail($"Expected type '{expectedType.Name}' does not match actual type '{actualType.Name}'.  {message}");
            }

            Assert.AreEqual(expected, actual, message);
        }

        //--------------------------------------------------------------------------------
        /// <summary>
        /// Compare two enumerable collections
        /// </summary>
        //--------------------------------------------------------------------------------
        static void AreEnumerablesEqual(IEnumerable expected, IEnumerable actual, string message = "")
        {
            var expectedEnumerator = expected.GetEnumerator();
            var actualEnumerator = actual.GetEnumerator();

            int count = 0;
            while (true)
            {
                var expectedHasValue = expectedEnumerator.MoveNext();
                var actualHasValue = actualEnumerator.MoveNext();

                if (!expectedHasValue && !actualHasValue) return;
                count++;
                if (!expectedHasValue)
                {
                    var expectedCount = count - 1;
                    var actualCount = count;
                    var value = actualEnumerator.Current;
                    while (actualEnumerator.MoveNext()) actualCount++;
                    var failMessage = new StringBuilder();
                    failMessage.AppendLine($"Expected {expectedCount} items but got {actualCount}.");
                    failMessage.AppendLine($"Value at index {count - 1} was: {value}");
                    failMessage.AppendLine(message);
                    Assert.Fail(failMessage.ToString());
                }

                if (!actualHasValue)
                {
                    var actualCount = count - 1;
                    var expectedCount = count;
                    var value = expectedEnumerator.Current;
                    while (expectedEnumerator.MoveNext()) expectedCount++;
                    var failMessage = new StringBuilder();
                    failMessage.AppendLine($"Expected {expectedCount} items but got {actualCount}.");
                    failMessage.AppendLine($"Value at index {count - 1} was: {value}");
                    failMessage.AppendLine(message);
                    Assert.Fail(failMessage.ToString());
                }

                AreEqual(expectedEnumerator.Current, actualEnumerator.Current, $"\r\nArrays differed at element {count - 1}. \r\n {message}");

            }

        }

        //--------------------------------------------------------------------------------
        /// <summary>
        /// Checks if the string starts with the expected value.  If not, it will show
        /// a detailed veiw of where they are different.
        /// </summary>
        //--------------------------------------------------------------------------------
        public static void StartsWith(string expectedStart, string actualString, string message = "")
        {
            if (expectedStart == null) expectedStart = "";
            var length = expectedStart.Length;
            var truncatedActual = actualString;
            if (actualString != null && actualString.Length > length)
            {
                truncatedActual = actualString.Substring(0, length);
            }
            AreStringsEqual(expectedStart, truncatedActual, message);
        }


        //--------------------------------------------------------------------------------
        /// <summary>
        /// Checks if the string ends with the expected value.  If not, it will show
        /// a detailed veiw of where they are different.
        /// </summary>
        //--------------------------------------------------------------------------------
        public static void EndsWith(string expectedEnd, string actualString, string message = "")
        {
            if (expectedEnd == null) expectedEnd = "";
            var length = expectedEnd.Length;
            var truncatedActual = actualString;
            if (actualString != null && actualString.Length > length)
            {
                truncatedActual = actualString.Substring(actualString.Length - length);
            }
            AreStringsEqual(expectedEnd, truncatedActual, message);
        }


        // Helper to convert movement characters to something we can see
        static char SafeChar(char input)
        {
            switch (input)
            {
                case '\t':
                case '\r':
                case '\n': return '°';
                default: return input;
            }
        }

        //--------------------------------------------------------------------------------
        /// <summary>
        /// Does a better job of showing exactly where the strings diverge.  THis is 
        /// called automatically through AssertEx.AreEqual.
        /// 
        /// Note: for this to work best, you need a fixed-width font for test output.  The
        /// only way do do that in visual studio is through this setting:
        /// Tools > Options > Fonts and Colors > "Show Settings For" dropdown > "Environment Font"
        /// (This will also change your menu font)
        /// </summary>
        //--------------------------------------------------------------------------------
        public static void AreStringsEqual(string expected, string actual, string message = "")
        {
            // Trivial checks
            if (expected == null && actual == null) return;
            if (actual == null) Assert.Fail($"Got a null value for the actual string. {message}");
            if (expected == null) Assert.Fail($"Expected a null value for the actual string. {message}");

            // Find the spot where the strings differ
            var spot = 0;
            for (; spot < expected.Length && spot < actual.Length; spot++)
            {
                if (expected[spot] != actual[spot]) break;
            }

            if (spot == expected.Length && spot == actual.Length) return;


            // Construct partial strings to highlight where the difference is
            var expectedPart = new StringBuilder();
            var actualPart = new StringBuilder();
            var radius = 25;
            var dashLength = (spot < radius) ? spot : radius;

            if (spot > radius)
            {
                expectedPart.Append("...");
                actualPart.Append("...");
                dashLength += 3;
            }

            for (int i = spot - radius; i < spot + radius; i++)
            {
                if (i < 0) continue;
                if (i < expected.Length) expectedPart.Append(SafeChar(expected[i]));
                if (i < actual.Length) actualPart.Append(SafeChar(actual[i]));
            }

            if (spot + 30 < expected.Length)
            {
                expectedPart.Append("...");
            }
            if (spot + 30 < actual.Length)
            {
                actualPart.Append("...");
            }

            // Build an output message
            var output = new StringBuilder();
            output.AppendLine();
            output.AppendLine($"Strings did not match at position {spot}. ");
            if (!string.IsNullOrEmpty(message)) output.AppendLine($"   {message}");
            output.AppendLine();
            output.AppendLine($"Expected: {expectedPart}");
            output.AppendLine($"Actual:   {actualPart}");
            output.AppendLine($"          {new string('-', dashLength)}^");
            output.AppendLine();
            output.AppendLine("Expected: ");
            output.AppendLine($"{expected.Substring(0, Math.Min(expected.Length, 1400))}");
            output.AppendLine();
            output.AppendLine("Actual: ");
            output.AppendLine($"{actual.Substring(0, Math.Min(actual.Length, 1400))}");

            Assert.Fail(output.ToString());
        }

    }
}
