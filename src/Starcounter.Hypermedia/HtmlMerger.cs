

using HtmlAgilityPack;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Starcounter.Advanced.Hypermedia {
    /// <summary>
    /// Merges two HTML representations into a single HTML representation.
    /// </summary>
    /// <remarks>
    /// Elements in document B will replace elements in document A if they
    /// have the same id and that their direct or indirect parents share the
    /// same id. 
    /// 
    /// Elements that have no ids but that are bound to a single data property
    /// using the {{ }} syntax, will be treated as having the id
    /// "[[ParentId.SomeDataProperty]]". 
    /// This allows the author to merge html documents without having to set
    /// explicit ids.
    /// </remarks>
    public class HtmlMerger {

        HtmlDoc DocA;
        HtmlDoc DocB;

//        Dictionary<string, HtmlNode> Nodes;

        public HtmlMerger(string a, string b) {
            DocA = new HtmlDoc();
            DocA.LoadHtml(a);
            DocB = new HtmlDoc();
            DocB.LoadHtml(b);
//            UseLayoutFromA = true;
        }

//        public bool UseLayoutFromA { get; set; }

        public void Merge() {
        }

        public string GetString() {
            return GetString(DocA) + "\r\n" + GetString(DocB);
        }

        private static string GetString(HtmlDocument doc) {
            var ms = new MemoryStream();
            doc.Save(ms,Encoding.Default); // sb.ToString();
            var a = Encoding.Default.GetString(ms.GetBuffer(),0,(int)ms.Length);
            return a;
        }
    }

    internal class HtmlDoc : HtmlDocument {
    }
}
