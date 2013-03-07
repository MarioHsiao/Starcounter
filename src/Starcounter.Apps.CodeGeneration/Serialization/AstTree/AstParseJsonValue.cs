// ***********************************************************************
// <copyright file="AstParseCode.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Text;
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

            if (template is TObjArr) {
                template = ((TObjArr)template).App;
            }
            
            if (template is TPuppet) {
                appClassName = AstTreeHelper.GetAppClassName((TPuppet)template);
                Prefix.Add("var" + valueName + " = " + appClassName + "JsonSerializer.Deserialize((IntPtr)pfrag, nextSize, out valueSize);");
            } else if (template is TTrigger) {
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
            
            if (template is TString){
                parseFunction = "ParseString";
            } else if (template is TLong){
                parseFunction = "ParseInt";
            } else if (template is TDecimal) {
                parseFunction = "ParseDecimal";
            } else if (template is TDouble) {
                parseFunction = "ParseDouble";
            } else if (template is TBool) {
                parseFunction = "ParseBoolean";
            } else if (template is TPuppet || template is TObjArr) {
                parseFunction = "DeserializeApp(" + ((TPuppet)template).ClassName + ")";
            } else {
                throw new NotSupportedException("TODO! Add more types here");
            }
            return parseFunction;
        }
    }
}
