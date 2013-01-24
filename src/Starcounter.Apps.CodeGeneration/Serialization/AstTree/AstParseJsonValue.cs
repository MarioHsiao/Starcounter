// ***********************************************************************
// <copyright file="AstParseCode.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Text;
using Starcounter.Internal.Uri;
using Starcounter.Templates;

namespace Starcounter.Internal.Application.CodeGeneration.Serialization {
    /// <summary>
    /// 
    /// </summary>
    internal class AstParseJsonValue : AstNode {
        internal ParseNode ParseNode { get; set; }

        /// <summary>
        /// Generates C# source code for this abstract syntax tree (AST) node
        /// </summary>
        /// <value>The debug string.</value>
        internal override string DebugString {
            get {
                Template template = (Template)ParseNode.Handler.Code;
                Type valueType = template.InstanceType;
                return GetParseFunctionName(valueType) + "(...)";
            }
        }

        /// <summary>
        /// Generates C# source code for this abstract syntax tree (AST) node
        /// </summary>
        internal override void GenerateCsCodeForNode() {
            string appClassName;
            string fullAppClassName;
            Template template = (Template)ParseNode.Handler.Code;
            string valueName = " val" + ParseNode.HandlerIndex;;
            
            if (template is AppTemplate) {
                appClassName = AstTreeHelper.GetAppClassName((AppTemplate)template);
                fullAppClassName = AstTreeHelper.GetFullAppClassName((AppTemplate)template);
                Prefix.Add(fullAppClassName + valueName + ";");
                Prefix.Add(valueName + " = " + appClassName + "JsonSerializer.Deserialize((IntPtr)pfrag, nextSize, out valueSize);");
            } else if (template is ListingProperty) {
                ListingProperty lp = (ListingProperty)template;
                appClassName = AstTreeHelper.GetAppClassName((AppTemplate)lp.App);
                fullAppClassName = AstTreeHelper.GetFullAppClassName((AppTemplate)lp.App);
                Prefix.Add(fullAppClassName + valueName + ";");
                Prefix.Add(valueName + " = " + appClassName + "JsonSerializer.Deserialize((IntPtr)pfrag, nextSize, out valueSize);");
            } else {
                Prefix.Add(template.InstanceType.Name + valueName + ";");
                Prefix.Add("if (JsonHelper." + GetParseFunctionName(template.InstanceType) + "((IntPtr)pfrag, nextSize, out" + valueName + ", out valueSize)) {");
                Suffix.Add("} else {");
                Suffix.Add("    throw new Exception(\"Unable to deserialize App. Content not compatible.\");"); // TODO: pinpoint error in deserializer.
                Suffix.Add("}");
            }
        }

        private string GetParseFunctionName(Type valueType) {
            string parseFunction = null;
            TypeCode code = Type.GetTypeCode(valueType);

            switch (code) {
                case TypeCode.String:
                    parseFunction = "ParseString";
                    break;
                case TypeCode.Int32:
                    parseFunction = "ParseInt";
                    break;
                case TypeCode.Decimal:
                    parseFunction = "ParseDecimal";
                    break;
                case TypeCode.Double:
                    parseFunction = "ParseDouble";
                    break;
                case TypeCode.Boolean:
                    parseFunction = "ParseBoolean";
                    break;
                case TypeCode.DateTime:
                    parseFunction = "ParseDateTime";
                    break;
                case TypeCode.Object:
                    parseFunction = "ParseApp<" + valueType.Name + ">(...)";
                    break;
                default:
                    throw new NotSupportedException("TODO! Add more types here");
            }
            return parseFunction;
        }
    }
}
