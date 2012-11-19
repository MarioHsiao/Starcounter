using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Starcounter.CommandLine;

namespace Starcounter.Internal.Tests {
    
    /// <summary>
    /// Expose a set of methods used when testing the functionality of
    /// our common command-line parsing / syntax API's.
    /// </summary>
    [TestFixture]
    public class CommandLineTest {
        /// <summary>
        /// Tests the low-level parser, supporting strings to be parsed
        /// into string arrays in a manner similar to how the CRT/CLR
        /// does it (i.e. the same type of parsing that preceeds calling
        /// the Main entrypoint).
        /// </summary>
        [Test]
        public void TestParsingStringToStringArray() {
            Assert.Throws<ArgumentNullException>(() => { CommandLineStringParser.SplitCommandLine(null); });
            AssertParseResult("one two three", new string[] { "one", "two", "three" });
            AssertParseResult("\"one two\" three", new string[] { "one two", "three" });
            AssertParseResult(@"C:\Users\Per", new string[] { @"C:\Users\Per" });
            AssertParseResult("\"C:\\Users\\Per\\Visual Studio Projects\"", new string[] { @"C:\Users\Per\Visual Studio Projects" });
            AssertParseResult("\"C:\\Users\\Per\\Visual Studio Projects\\\"", new string[] { @"C:\Users\Per\Visual Studio Projects\" });
            AssertParseResult("Path=\"C:\\Users\\Per\\Visual Studio Projects\"", new string[] { "Path=\"C:\\Users\\Per\\Visual Studio Projects\"" });
            AssertParseResult("\"Assembly=C:\\Program Files\\MyApp.exe\"", new string[] {@"Assembly=C:\Program Files\MyApp.exe"});
        }

        /// <summary>
        /// Utility method supporting unit testing of the low-level parsing of a
        /// command-line string to it's command-line string array eqivivalent.
        /// </summary>
        /// <param name="input">The command line as a string.</param>
        /// <param name="expectedResult">The expected result - assertions will be
        /// made that the parsed result is equal in number of items and that each
        /// item returns true when compared for equality.</param>
        public static void AssertParseResult(string input, string[] expectedResult) {
            var result = CommandLineStringParser.SplitCommandLine(input).ToArray();
            Assert.NotNull(result);
            Assert.True(result.Length == expectedResult.Length);
            for (int i = 0; i < result.Length; i++) {
                Assert.AreEqual(result[i], expectedResult[i]);
            }
        }
    }
}
