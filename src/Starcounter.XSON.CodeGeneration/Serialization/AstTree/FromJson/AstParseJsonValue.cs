// ***********************************************************************
// <copyright file="AstParseCode.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Text;
using Starcounter.Templates;

namespace Starcounter.Internal.Application.CodeGeneration {
    /// <summary>
    /// 
    /// </summary>
    internal class AstParseJsonValue : AstNode {
        internal ParseNode ParseNode { get; set; }

        private Template Template { get { return ParseNode.Template.Template; } }

        /// <summary>
        /// Generates C# source code for this abstract syntax tree (AST) node
        /// </summary>
        /// <value>The debug string.</value>
        internal override string DebugString {
            get {
                return "ParseJsonValue(" + Template.PropertyName + ")";
            }
        }

        /// <summary>
        /// Generates C# source code for this abstract syntax tree (AST) node
        /// </summary>
        internal override void GenerateCsCodeForNode() {
            string valueName = " val" + ParseNode.TemplateIndex;
            Template template = Template;

            if (template is TObjArr) {
                Prefix.Add("var" + valueName + " = obj." + template.PropertyName + ".Add();");
                Prefix.Add("valueSize = " + valueName + ".Populate((IntPtr)pBuffer, leftBufferSize);");
                Prefix.Add("if (valueSize != -1) {");
                GenerateBufferJumpCode();
                GenerateElseExceptionCode();
            } else if (template is TObj) {  
                Prefix.Add("var" + valueName + " = obj." + template.PropertyName + ";");
                Prefix.Add("valueSize = " + valueName + ".Populate((IntPtr)pBuffer, leftBufferSize);");
                Prefix.Add("if (valueSize != -1) {");
                GenerateBufferJumpCode();
                GenerateElseExceptionCode();
            } else if (template is TTrigger) {
                Prefix.Add("if (JsonHelper.IsNullValue((IntPtr)pBuffer, leftBufferSize, out valueSize)) {");
                GenerateBufferJumpCode();
                GenerateElseExceptionCode();
            } else {
                Prefix.Add(template.InstanceType.Name + valueName + ";");
                Prefix.Add("if (JsonHelper." + GetParseFunctionName(template) + "((IntPtr)pBuffer, leftBufferSize, out" + valueName + ", out valueSize)) {");
                Prefix.Add("    obj." + template.PropertyName + " =" + valueName + ";");
                GenerateBufferJumpCode();
                GenerateElseExceptionCode();
            }
        }

        private void GenerateBufferJumpCode() {
            Prefix.Add("    leftBufferSize -= valueSize;");
            Prefix.Add("    if (leftBufferSize < 0) {");
            Prefix.Add("        throw new Exception(\"Unable to deserialize App. Unexpected end of content\");");
            Prefix.Add("    }");
            Prefix.Add("    pBuffer += valueSize;");
        }

        private void GenerateElseExceptionCode() {
             Prefix.Add("} else {");
             Prefix.Add("    throw new Exception(\"Unable to deserialize App. Content not compatible.\");"); // TODO: pinpoint error in deserializer.
             Prefix.Add("}");
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
            }  else {
                throw new NotSupportedException();
            }
            return parseFunction;
        }
    }
}
