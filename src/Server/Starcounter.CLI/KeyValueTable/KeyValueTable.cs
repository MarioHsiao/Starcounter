using System;
using System.Collections.Generic;
using System.Linq;

namespace Starcounter.CLI {
    /// <summary>
    /// Expose functionality to write key-value sets in a table-like
    /// output to a given output source (normally the console).
    /// </summary>
    public class KeyValueTable {
        /// <summary>
        /// Gets or sets the left margin of the table.
        /// </summary>
        public int LeftMargin { get; set; }

        /// <summary>
        /// Gets or sets the space between the key- and the
        /// value column.
        /// </summary>
        public int ColumnSpace { get; set; }

        /// <summary>
        /// Gets or sets the title of the table.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Allows a client to customize the way any value is split up
        /// before written to the value column.
        /// </summary>
        public Func<string, int, IEnumerable<string>> SplitValue;

        /// <summary>
        /// Allows a client to customize entries of a split up value
        /// just before it is written to the value column. Every item
        /// of the returned result will be written on a new line.
        /// </summary>
        public Func<string, int, IEnumerable<string>> TrimValueItem;

        class BuiltInValueDelegates {
            public static IEnumerable<string> NoSplit(string content, int ignoredValueWidth) {
                return new SingleStringEnumerable(content);
            }

            public static IEnumerable<string> SplitOnLines(string content, int ignoredValueWidth) {
                return content.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            }

            public static IEnumerable<string> TrimToColumnWidth(string content, int columnWidth) {
                if (content.Length > columnWidth) {
                    content = content.Substring(0, columnWidth - 1);
                }
                return new SingleStringEnumerable(content);
            }

            public static IEnumerable<string> TrimBySplittingUpWords(string content, int columnWidth) {
                if (content.Length <= columnWidth) {
                    return new SingleStringEnumerable(content);
                }

                var words = content.Split(new[] { ' ' }, StringSplitOptions.None);
                var result = new List<string>();
                var current = string.Empty;

                foreach (var word in words) {
                    if ((current.Length + word.Length + 1) > columnWidth) {
                        result.Add(current.TrimEnd(' '));
                        current = string.Empty;
                    }
                    current += word + " ";
                }

                if (current != string.Empty) {
                    result.Add(current.TrimEnd(' '));
                }
                return result;
            }
        }

        class SingleStringEnumerable : IEnumerable<string>, IEnumerator<string> {
            readonly string content;
            bool more;

            internal SingleStringEnumerable(string c) {
                content = c;
                more = true;
            }

            IEnumerator<string> IEnumerable<string>.GetEnumerator() {
                return this;
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
                return this;
            }

            string IEnumerator<string>.Current {
                get { return content; }
            }

            void IDisposable.Dispose() {
            }

            object System.Collections.IEnumerator.Current {
                get { return content; }
            }

            bool System.Collections.IEnumerator.MoveNext() {
                var moved = more;
                more = false;
                return moved;
            }

            void System.Collections.IEnumerator.Reset() {
                more = true;
            }
        }

        /// <summary>
        /// Instantiate a new <see cref="KeyValueTable"/>.
        /// </summary>
        public KeyValueTable() {
            LeftMargin = 2;
            ColumnSpace = 1;
            SplitValue = BuiltInValueDelegates.NoSplit;
            TrimValueItem = BuiltInValueDelegates.TrimBySplittingUpWords;
        }

        /// <summary>
        /// Writes the given content to the current <see cref="KeyValueTable"/>.
        /// </summary>
        /// <param name="content">The content, including keys and
        /// values.</param>
        /// <param name="splitValueOption">Optional way of specifying a built-in
        /// split-up of values.</param>
        public void Write(Dictionary<string, string> content, ValueSplitOptions splitValueOption = ValueSplitOptions.None) {
            int keyWidth = FindLongestKey(content.Keys).Length;
            keyWidth += ColumnSpace;
            int valueWidth = Console.BufferWidth - (LeftMargin + keyWidth);

            var format = "".PadLeft(LeftMargin) + "{0,-" + keyWidth.ToString() + "}{1}";
            if (splitValueOption == ValueSplitOptions.SplitLines) {
                SplitValue = BuiltInValueDelegates.SplitOnLines;
            }

            WriteTitle();
            foreach (var item in content) {
                WriteOne(format, item.Key, item.Value, valueWidth);
            }
        }

        void WriteTitle() {
            if (!string.IsNullOrWhiteSpace(Title)) {
                Console.WriteLine(Title);
            }
        }

        void WriteOne(string format, string key, string value, int valueColumnWidth) {
            var values = SplitValue(value, valueColumnWidth);

            foreach (var item in values) {
                var trimmedItems = TrimValueItem(item, valueColumnWidth);
                foreach (var valueItem in trimmedItems) {
                    Console.WriteLine(format, key, valueItem);
                    key = string.Empty;
                }
            }
        }

        string FindLongestKey(IEnumerable<string> keys) {
            var result = keys.FirstOrDefault();
            foreach (var item in keys) {
                if (item.Length > result.Length) {
                    result = item;
                }
            }
            return result;
        }
    }
}
