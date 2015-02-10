using System.Collections.Generic;

namespace Starcounter.Internal.XSON.Tests.CompiledJson {
    /// <summary>
    /// </summary>
    [simplewithcodebehind_json]
    partial class simplewithcodebehind : Json, IBound<TestData> {
        /// <summary></summary>
        public string SomeName {
            get {
                return Data.Name;
            }
        }

        /// <summary></summary>
        public IEnumerable<TestData> AllItems {
            get {
                var allItems = new List<TestData>();
                allItems.Add(new TestData() { Name = "papa" });
                return allItems;
            }
        }

        /// <summary></summary>
        [simplewithcodebehind_json.Items]
        partial class ItemJson : Json, IBound<TestData> {
            /// <summary></summary>
            public string SomeOtherName {
                get {
                    return Data.Name;
                }
            }
        }
    }

    /// <summary></summary>
    public class TestData {
        /// <summary></summary>
        public string Name { get; set; }
    }
}
