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

        private Template Template { get { return (Template)ParseNode.Handler.Code; } }

        /// <summary>
        /// Generates C# source code for this abstract syntax tree (AST) node
        /// </summary>
        /// <value>The debug string.</value>
        internal override string DebugString {
            get {
                return GetParseFunctionName(Template);
            }
        }

        /// <summary>
        /// Generates C# source code for this abstract syntax tree (AST) node
        /// </summary>
        internal override void GenerateCsCodeForNode() {
            string appClassName;
            string valueName = " val" + ParseNode.HandlerIndex;;
            Template template = Template;

            if (template is ArrProperty) {
                template = ((ArrProperty)template).App;
            }
            
            if (template is AppTemplate) {
                appClassName = AstTreeHelper.GetAppClassName((AppTemplate)template);
                Prefix.Add("var" + valueName + " = " + appClassName + "JsonSerializer.Deserialize((IntPtr)pfrag, nextSize, out valueSize);");
            } else if (template is ActionProperty) {
                Prefix.Add("if (JsonHelper.IsNullValue((IntPtr)pfrag, nextSize, out valueSize)) {");
                Suffix.Add("} else {");
                Suffix.Add("    throw new Exception(\"Unable to deserialize App. Content not compatible.\");"); // TODO: pinpoint error in deserializer.
                Suffix.Add("}");
            } else {
                Prefix.Add(template.InstanceType.Name + valueName + ";");
                Prefix.Add("if (JsonHelper." + GetParseFunctionName(template) + "((IntPtr)pfrag, nextSize, out" + valueName + ", out valueSize)) {");
                Suffix.Add("} else {");
                Suffix.Add("    throw new Exception(\"Unable to deserialize App. Content not compatible.\");"); // TODO: pinpoint error in deserializer.
                Suffix.Add("}");
            }
        }

        private string GetParseFunctionName(Template template) {
            string parseFunction = null;
            
            if (template is StringProperty){
                parseFunction = "ParseString";
            } else if (template is IntProperty){
                parseFunction = "ParseInt";
            } else if (template is DecimalProperty) {
                parseFunction = "ParseDecimal";
            } else if (template is DoubleProperty) {
                parseFunction = "ParseDouble";
            } else if (template is BoolProperty) {
                parseFunction = "ParseBoolean";
            } else if (template is AppTemplate || template is ArrProperty) {
                parseFunction = "DeserializeApp(" + ((AppTemplate)template).ClassName + ")";
            } else {
                throw new NotSupportedException("TODO! Add more types here");
            }
            return parseFunction;
        }
    }
}
