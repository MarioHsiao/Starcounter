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

        internal override string DebugString {
            get {
                return "PropertyValue(" + Template.TemplateName + ")";
            }
        }

        internal override void GenerateCsCodeForNode() {
            Prefix.Add("valueSize = " + GetWriteFunction(Template));
            Prefix.Add("if (valueSize == -1)");
            Prefix.Add("    goto restart;");
            Prefix.Add("if (bufferSize < (offset + 1))");
            Prefix.Add("    goto restart;");
            Prefix.Add("offset += valueSize;");
            Prefix.Add("buf += valueSize;");
        }

        private string GetWriteFunction(Template template) {
            string parseFunction = null;

            if (template is TString) {
                parseFunction = "JsonHelper.WriteString((IntPtr)buf, bufferSize - offset, obj." + Template.PropertyName + ");";
            } else if (template is TLong) {
                parseFunction = "JsonHelper.WriteInt((IntPtr)buf, bufferSize - offset, obj." + Template.PropertyName + ");";
            } else if (template is TDecimal) {
                parseFunction = "JsonHelper.WriteDecimal((IntPtr)buf, bufferSize - offset, obj." + Template.PropertyName + ");";
            } else if (template is TDouble) {
                parseFunction = "JsonHelper.WriteDouble((IntPtr)buf, bufferSize - offset, obj." + Template.PropertyName + ");";
            } else if (template is TBool) {
                parseFunction = "JsonHelper.WriteBool((IntPtr)buf, bufferSize - offset, obj." + Template.PropertyName + ");";
            } else if (template is TTrigger) {
                parseFunction = "JsonHelper.WriteNull((IntPtr)buf, bufferSize - offset);";
            } else {
                throw new NotSupportedException();
            }
            
            return parseFunction;
        }
    }
}
