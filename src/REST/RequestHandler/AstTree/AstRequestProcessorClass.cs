

using System;
using System.Text;
namespace Starcounter.Internal.Uri {
    internal class AstRequestProcessorClass : AstNode {

        internal int ClassNo = 0;

        internal AstProcessFunction ProcessFunction { get; set; }
        internal ParseNode ParseNode { get; set; }

        internal void AllocateSubClassNo() {
            ClassNo = Root.GetNextClassNo();
        }

        internal override string DebugString {
            get {
                var sb = new StringBuilder();
                if (Parent == null)
                    sb.Append("public ");
                sb.Append( "class " );
                sb.Append(ClassName);
                return sb.ToString();
            }
        }

        internal bool IsTopLevel {
            get {
                return (Parent is AstNamespace);
            }
        }

        internal bool IsSingleHandler {
            get {
                return ParseNode.HandlerIndex != -1;
            }
        }

        /// <summary>
        /// Generates C# source code for this abstract syntax tree (AST) node
        /// </summary>
        internal override void GenerateCsCodeForNode() {
            var sb = new StringBuilder();
            Prefix.Add("");
            if (IsTopLevel) {
                Prefix.Add("public class GeneratedRequestProcessor : TopLevelRequestProcessor {");
            }
            else {
                sb.Append("public class ");
                sb.Append(ClassName);
                sb.Append(" : ");
                if (IsSingleHandler) {
                    sb.Append("SingleRequestProcessor");
                    bool hasGeneric = false;
                    foreach (var t in ParseNode.Parent.ParseTypes) {
                        if (hasGeneric)
                            sb.Append(",");
                        else
                            sb.Append("<");
                        sb.Append(t);
                        hasGeneric = true;
                    }
                    if (hasGeneric)
                        sb.Append(">");
                    sb.Append(" {");
                    Prefix.Add(sb.ToString());
                }
            }
            Suffix.Add("}");
        }


        internal string ClassName {
            get {
                if (Parent is AstNamespace) {
                    return "GeneratedRequestProcessor";
                }
                return "Sub" + ClassNo + "Processor";
            }
        }

        internal string PropertyName {
            get {
                return "Sub" + ClassNo;
            }
        }
    }
}
