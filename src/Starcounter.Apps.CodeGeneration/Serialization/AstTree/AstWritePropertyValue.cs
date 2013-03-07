// ***********************************************************************
// <copyright file="AstCase.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Text;
using Starcounter.Templates;

namespace Starcounter.Internal.Application.CodeGeneration.Serialization {
    internal class AstWritePropertyValue : AstNode {
        internal Template Template { get; set; }

        internal string VariableName { get; set; }

        internal override string DebugString {
            get {
                return "WritePropertyValue(" + Template.Name + ")";
            }
        }

        internal override void GenerateCsCodeForNode() {
            if (Template is TPuppet) {
                string appClassName = AstTreeHelper.GetAppClassName((TPuppet)Template);
                string varName = (VariableName == null) ? "app." + Template.PropertyName : VariableName;
                Prefix.Add("valSize = " + appClassName + "JsonSerializer.Serialize((IntPtr)pfrag, nextSize, " + varName + ");");
            } else {
                Prefix.Add("valSize = " + GetWriteFunction(Template));
            }
            Prefix.Add("if (valSize == -1)");
            Prefix.Add("    throw new Exception(\"Buffer too small.\");");
            Prefix.Add("nextSize -= valSize;");
            Prefix.Add("pfrag += valSize;");
        }

        private string GetWriteFunction(Template template) {
            string parseFunction = null;

            if (template is TString) {
                parseFunction = "JsonHelper.WriteString((IntPtr)pfrag, nextSize, app." + Template.PropertyName + ", tmpArr);";
            } else if (template is TLong) {
                parseFunction = "JsonHelper.WriteInt((IntPtr)pfrag, nextSize, app." + Template.PropertyName + ");";
            } else if (template is TDecimal) {
                parseFunction = "JsonHelper.WriteDecimal((IntPtr)pfrag, nextSize, app." + Template.PropertyName + ", tmpArr);";
            } else if (template is TDouble) {
                parseFunction = "JsonHelper.WriteDouble((IntPtr)pfrag, nextSize, app." + Template.PropertyName + ", tmpArr);";
            } else if (template is TBool) {
                parseFunction = "JsonHelper.WriteBool((IntPtr)pfrag, nextSize, app." + Template.PropertyName + ");";
            } else if (template is TTrigger) {
                parseFunction = "JsonHelper.WriteNull((IntPtr)pfrag, nextSize);";
            } else {
                throw new NotSupportedException("TODO! Add more types here");
            }
            
            return parseFunction;
        }
    }
}
