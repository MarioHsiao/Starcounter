using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.Templates;

namespace Starcounter.Internal.XSON.PartialClassGeneration.Tests {
    internal static class Helper {
        private static Random Random = new Random(2134235);

        internal static TObject Create(string json, string className) {
            var tobj = (TObject)Template.CreateFromMarkup("json", json, "NodeJson");

            // This is needed since we don't have any generated code to add this.
            TObjArr toa = (tobj.Properties.Count > 3) ? tobj.Properties[3] as TObjArr : null;
            if (toa != null) {
                toa.SetCustomGetElementType((arr) => {
                    //return Helper.Create(json, "NodeJson"); 
                    return (TObject)arr.Parent;
                });
            }
            tobj.ClassName = "NodeJson";
            return tobj;
        }

        internal static Node GetTreeData() {
            var node = new Node();
            node.Header = "root";
            node.Depth = 0;
            CreateChildNodes(node, Random.Next(1, 5), 1);
            return node;
        }

        private static void CreateChildNodes(Node node, int number, int depth) {
            Node child;
            if (depth > 5)
                return;

            for (int i = 1; i <= number; i++) {
                child = new Node() {
                    Header = "Node" + depth + "_" + i,
                    Depth = depth
                };
                node.Nodes.Add(child);
                CreateChildNodes(child, Random.Next(1, 5), depth + 1);
            }
        }

        [Conditional("CONSOLE")]
        internal static void ConsoleWriteLine(string msg) {
            Console.WriteLine(msg);
        }

        [Conditional("CONSOLE")]
        internal static void ConsoleWrite(string msg) {
            Console.Write(msg);
        }
    }

    internal class Node {
        public string Header;
        public long Depth;
        public List<Node> Nodes = new List<Node>();
        public long Count { get { return Nodes.Count; } }
    }
}
