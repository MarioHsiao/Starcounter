// ***********************************************************************
// <copyright file="AstCase.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Text;
using Starcounter.Templates;

namespace Starcounter.Internal.Application.CodeGeneration {
    internal class AstJsonPropertyValue : AstNode {
        internal Template Template { get; set; }

        internal string VariableName { get; set; }

        internal override string DebugString {
            get {
                if (Template is TObj) {
                    return Template.PropertyName + ".ToJson(... )";
                } else if (Template is TObjArr) {
                    return "loop(item in " + Template.PropertyName + ") item.ToJson(...)";
                } else {
                    return "PropertyValue(" + Template.TemplateName + ")";
                }
            }
        }

        internal override void GenerateCsCodeForNode() {
            if (Template is TObj) {
                Prefix.Add("childObj = obj." + Template.PropertyName + ";");
                GenerateCodeForObj();
            } else if (Template is TObjArr) {
                GenerateCodeForObjArray();
            } else {
                Prefix.Add("valueSize = " + GetWriteFunction(Template));
            }
            Prefix.Add("if (valueSize == -1)");
            Prefix.Add("    throw ErrorCode.ToException(Starcounter.Error.SCERRUNSPECIFIED);");
            Prefix.Add("leftBufferSize -= valueSize;");
            Prefix.Add("pBuffer += valueSize;");
        }

        private void GenerateCodeForObj(){
            Prefix.Add("if (childObj == null)");
            Prefix.Add("    valueSize = JsonHelper.WriteNull((IntPtr)pBuffer, leftBufferSize);");
            Prefix.Add("else");
            Prefix.Add("    valueSize = childObj.ToJson((IntPtr)pBuffer, leftBufferSize);");
        }

        private void GenerateCodeForObjArray() {
            // Writing the start of the array and checking size with end character included.
            Prefix.Add("if ((leftBufferSize - 2) < 0)");
            Prefix.Add("    throw ErrorCode.ToException(Starcounter.Error.SCERRUNSPECIFIED);");
            Prefix.Add("*pBuffer++ = (byte)'[';");
            Prefix.Add("leftBufferSize -= 2;");

            // looping all objects in the array.
            Prefix.Add("for(int i = 0; i < obj." + Template.PropertyName + ".Count; i++) {");
            Prefix.Add("    childObj = obj." + Template.PropertyName + "[i];");
            
            GenerateCodeForObj();

            Suffix.Add("    if ((i+1) < obj." + Template.PropertyName + ".Count) {");
            Suffix.Add("        leftBufferSize--;");
            Suffix.Add("        if (leftBufferSize < 0)");
            Suffix.Add("            throw ErrorCode.ToException(Starcounter.Error.SCERRUNSPECIFIED);");
            Suffix.Add("        *pBuffer++ = (byte)',';");
            Suffix.Add("    }");
            Suffix.Add("}"); // end of loop
            Suffix.Add("*pBuffer++ = (byte)']';"); // end of array.
        }

        private string GetWriteFunction(Template template) {
            string parseFunction = null;

            if (template is TString) {
                parseFunction = "JsonHelper.WriteString((IntPtr)pBuffer, leftBufferSize, obj." + Template.PropertyName + ");";
            } else if (template is TLong) {
                parseFunction = "JsonHelper.WriteInt((IntPtr)pBuffer, leftBufferSize, obj." + Template.PropertyName + ");";
            } else if (template is TDecimal) {
                parseFunction = "JsonHelper.WriteDecimal((IntPtr)pBuffer, leftBufferSize, obj." + Template.PropertyName + ");";
            } else if (template is TDouble) {
                parseFunction = "JsonHelper.WriteDouble((IntPtr)pBuffer, leftBufferSize, obj." + Template.PropertyName + ");";
            } else if (template is TBool) {
                parseFunction = "JsonHelper.WriteBool((IntPtr)pBuffer, leftBufferSize, obj." + Template.PropertyName + ");";
            } else if (template is TTrigger) {
                parseFunction = "JsonHelper.WriteNull((IntPtr)pBuffer, leftBufferSize);";
            } else {
                throw new NotSupportedException();
            }
            
            return parseFunction;
        }
    }
}
