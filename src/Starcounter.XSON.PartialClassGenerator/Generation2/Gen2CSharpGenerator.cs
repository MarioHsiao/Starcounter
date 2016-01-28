﻿// ***********************************************************************
// <copyright file="CSharpGenerator.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

// Remove this define to generate code without #line directives
#define ADDLINEDIRECTIVES

using Starcounter.Templates.Interfaces;
using System.Text;
using System;
using Starcounter.Templates;
using System.Collections.Generic;
using Starcounter.XSON.Metadata;
using System.Diagnostics;
using System.Globalization;

namespace Starcounter.Internal.MsBuild.Codegen {
    /// <summary>
    /// Class CSharpGenerator
    /// </summary>
    public class Gen2CSharpGenerator : ITemplateCodeGenerator {
        /// <summary>
        /// The generated code output.
        /// </summary>
        internal StringBuilder Output = new StringBuilder();

        /// <summary>
        /// The root of the generated code.
        /// </summary>
        public AstRoot Root;

        /// <summary>
        /// Gets or sets the indentation.
        /// </summary>
        /// <value>The indentation.</value>
        public int Indentation { get; set; }

        /// <summary>
        /// The code generator can be used to generate typed JSON. My storing the generarator
        /// we can obtain the wanted default obj template, i.e.
        /// which template to use as the default template for new objects.
        /// </summary>
        public Gen2DomGenerator Generator;
        private const string markAsCodegen = "[_GEN1_][_GEN2_(\"Starcounter\",\"2.0\")]";
        private const string markAsCodegen2 = "[_GEN2_(\"Starcounter\",\"2.0\")]";
        private static List<string> defaultUsings;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IReadOnlyTree GenerateAST() {
            return Root;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="generator"></param>
        /// <param name="root"></param>
        public Gen2CSharpGenerator(Gen2DomGenerator generator, AstRoot root) {
            Generator = generator;
            Root = root;
            Indentation = 4;
        }

        /// <summary>
        /// Returns a multiline string representation of the code dom tree
        /// </summary>
        /// <returns>A multiline string</returns>
        public string DumpAstTree() {
            var sb = new StringBuilder();
            DumpTree(sb, Root, 0);
            return sb.ToString();
        }

        /// <summary>
        /// Dumps the tree.
        /// </summary>
        /// <param name="sb">The sb.</param>
        /// <param name="node">The node.</param>
        /// <param name="indent">The indent.</param>
        private void DumpTree(StringBuilder sb, AstBase node, int indent) {
            sb.Append(' ', indent);
            sb.AppendLine(node.ToString());
            foreach (var kid in node.Children) {
                DumpTree(sb, kid, indent + 3);
            }
        }

        /// <summary>
        /// Generates source code for the simple code dom tree generated from the Starcounter
        /// application view document template.
        /// </summary>
        /// <returns>The .cs source code as a string</returns>
        public string GenerateCode() {
            ProcessAllNodes();

            bool writeRootNs = !string.IsNullOrEmpty(Root.AppClassClassNode.Namespace);
            if (writeRootNs)
                Output.AppendLine("namespace " + Root.AppClassClassNode.Namespace + " {");

            WriteHeader(Root, Root.AppClassClassNode.Template.CompilerOrigin.FileName, Output);            
            foreach (var napp in Root.Children) {
                WriteNode(napp);
            }
            WriteFooter(Output);

            if (writeRootNs)
                Output.AppendLine("}");
            
            return Output.ToString();
        }

        /// <summary>
        /// Processes all nodes.
        /// </summary>
        private void ProcessAllNodes() {
            AstJsonClass napp;
            AstBase previousKidWithNs;
            String previousNs;
            String currentNs = null;

            previousKidWithNs = null;
            previousNs = Root.AppClassClassNode.Namespace;
            for (Int32 i = 0; i < Root.Children.Count; i++) {
                var kid = Root.Children[i];

                if (kid is AstJsonClass) {
                    napp = kid as AstJsonClass;

                    currentNs = napp.Namespace;
                    if (currentNs != previousNs) {
                        if (previousKidWithNs != null && !String.IsNullOrEmpty(previousNs)) {
                            previousKidWithNs.Suffix.Add("}");
                        }
                        previousKidWithNs = kid;
                        previousNs = currentNs;

                        if (!String.IsNullOrEmpty(currentNs)) {
                            kid.Prefix.Add("");
                            kid.Prefix.Add("namespace " + currentNs + " {");
                            
                        }
                    }
                } else if (kid is AstClassAlias) {
                    var alias = kid as AstClassAlias;
                    kid.Prefix.Add("using " + alias.Alias + " = " + alias.Specifier + ";");
                }

                ProcessNode(kid);
            }

            if (previousKidWithNs != null && !String.IsNullOrEmpty(previousNs)) {
                previousKidWithNs.Suffix.Add("}");
            }
        }

        /// <summary>
        /// Create prefix and suffix strings for a node and its children
        /// </summary>
        /// <param name="node">
        /// The syntax tree node. Use root to generate the complete source code.
        /// </param>
        private void ProcessNode(AstBase node) {
            var sb = new StringBuilder();
            if (node is AstClass) {
                if (node is AstMetadataClass) {
                    node.Prefix.Add("");

                    AppendLineDirectiveIfDefined(node.Prefix, "#line hidden");

                    var n = node as AstMetadataClass;
                    sb.Append("public class ");
                    sb.Append(n.ClassStemIdentifier);
                    sb.Append("<__Tjsonobj__,__jsonobj__>");
                    if (n.InheritedClass != null) {
                        sb.Append(" : ");
                        sb.Append(n.InheritedClass.GlobalClassSpecifierWithoutGenerics);
                        sb.Append("<__Tjsonobj__,__jsonobj__>");
                    }
                    sb.Append(" {");
                    node.Prefix.Add(sb.ToString());
                    WriteObjMetadataClassPrefix(node as AstMetadataClass);
                    node.Suffix.Add("}");

                    AppendLineDirectiveIfDefined(node.Suffix, "#line default");

                } else if (node is AstClass) {
                    node.Prefix.Add("");

                    AppendLineDirectiveIfDefined(node.Prefix, "#line hidden");

                    var n = node as AstClass;
                    sb.Append("public ");

                    if (n.MarkAsCodegen) {
                        node.Prefix.Add(markAsCodegen);
                    }

                    if (n.IsStatic) {
                        sb.Append("static ");
                    }
                    if (n.IsPartial) {
                        sb.Append("partial ");
                    }
                    sb.Append("class ");
                    sb.Append(n.ClassStemIdentifier);
                    if (node is AstSchemaClass) {
                        var ast = node as AstSchemaClass;
                        var inherited = (AstTemplateClass)ast.InheritedClass;
                        if (inherited != null) {
                            sb.Append(" : ");
//                            sb.Append(inherited.GlobalClassSpecifierWithoutGenerics);
                            sb.Append(inherited.GlobalClassSpecifier);
                        }
                    } else {
                        if (n.Inherits != null) {
                            sb.Append(" : ");
                            sb.Append(n.Inherits);
                        }
                    }
                    sb.Append(" {");
                    node.Prefix.Add(sb.ToString());
                    if (node is AstJsonClass) {
                        WriteAppClassPrefix(node as AstJsonClass);
                    } else if (node is AstSchemaClass) {
                        WriteTAppConstructor((node as AstSchemaClass).Constructor);
                        WriteSchemaOverrides(node as AstSchemaClass);
                    }
                    node.Suffix.Add("}");

                    AppendLineDirectiveIfDefined(node.Suffix, "#line default");

                } else {
                    throw new Exception();
                }
            } else if (node is AstProperty) {
                if (node.Parent is AstJsonClass)
                    WriteAppMemberPrefix(node as AstProperty);
                else if (node.Parent is AstSchemaClass)
                    WriteTAppMemberPrefix(node as AstProperty);
                else if (node.Parent is AstMetadataClass)
                    WriteObjMetadataMemberPrefix(node as AstProperty);
            }

            foreach (var kid in node.Children) {
                ProcessNode(kid);
            }
        }

        /// <summary>
        /// Writes the node.
        /// </summary>
        /// <param name="node">The node.</param>
        private void WriteNode(AstBase node) {
            foreach (var x in node.Prefix) {
                Output.Append(' ', node.Indentation);
                Output.Append(x);
                Output.Append('\n');
            }
            foreach (var kid in node.Children) {
                kid.Indentation = node.Indentation + Indentation;
                WriteNode(kid);
            }
            foreach (var x in node.Suffix) {
                Output.Append(' ', node.Indentation);
                Output.Append(x);
                Output.Append('\n');
            }
        }

        /// <summary>
        /// Writes the app member prefix.
        /// </summary>
        /// <param name="m">The m.</param>
        private void WriteAppMemberPrefix(AstProperty m) {
            if (m.Template is TTrigger)
                return;

            bool appendMemberName = (m.Template != ((AstJsonClass)m.Parent).Template);

            m.Prefix.Add(markAsCodegen);
            var sb = new StringBuilder();

            sb.Append("public ");

            if (m.Template.IsPrimitive) {
                sb.Append(HelperFunctions.GetGlobalClassSpecifier(m.Template.InstanceType, true));
            } else {
                sb.Append(m.Type.GlobalClassSpecifier);
            }

            sb.Append(' ');
            sb.Append(m.MemberName);
            sb.AppendLine(" {");

            AppendLineDirectiveIfDefined(sb, 
                                     "#line "
                                     + m.Template.CompilerOrigin.LineNo
                                     + " \""
                                     + m.Template.CompilerOrigin.FileName
                                     + "\"");

            sb.AppendLine("    get {");

            AppendLineDirectiveIfDefined(sb, "#line hidden");

            sb.Append("        return ");
            if (!m.Template.IsPrimitive && m.Type is AstJsonClass) {
                sb.Append('(');
                sb.Append(m.Type.GlobalClassSpecifier);
                sb.Append(')');
            }

            sb.Append("Template");
            if (appendMemberName) {
                sb.Append('.');
                sb.Append(m.MemberName);
            } 
            sb.AppendLine(".Getter(this); }");

            AppendLineDirectiveIfDefined(sb,
                                     "#line "
                                     + m.Template.CompilerOrigin.LineNo
                                     + " \""
                                     + m.Template.CompilerOrigin.FileName
                                     + "\"");

            sb.AppendLine("    set {");
            AppendLineDirectiveIfDefined(sb, "#line hidden");
            sb.Append("        Template");
            if (appendMemberName) {
                sb.Append('.');
                sb.Append(m.MemberName);
            } 
            sb.AppendLine(".Setter(this, value); } }");

            AppendLineDirectiveIfDefined(sb, "#line default");

            m.Prefix.Add(sb.ToString());
        }

        /// <summary>
        /// Writes the app class prefix.
        /// </summary>
        /// <param name="a">A.</param>
        private void WriteAppClassPrefix(AstJsonClass a) {
            var sb = new StringBuilder();

            AppendLineDirectiveIfDefined(a.Prefix, "    #line hidden");

            a.Prefix.Add("    " + markAsCodegen2);
            a.Prefix.Add("    public static "
                         + a.NTemplateClass.GlobalClassSpecifier
                         + " DefaultTemplate = new "
                         + a.NTemplateClass.GlobalClassSpecifier
                         + "();");

            a.Prefix.Add("    " + markAsCodegen);
            a.Prefix.Add("    public "
                         + a.ClassStemIdentifier
                         + "() { }");
            a.Prefix.Add("    " + markAsCodegen);
            a.Prefix.Add("    public "
                         + a.ClassStemIdentifier
                         + "(" +
                         a.NTemplateClass.GlobalClassSpecifier +
                         " template) { Template = template; }");

            a.Prefix.Add("    " + markAsCodegen);
            a.Prefix.Add("    protected override _ScTemplate_ GetDefaultTemplate() { return DefaultTemplate; }");

            a.Prefix.Add("    " + markAsCodegen);
            a.Prefix.Add("    public new "
                         + a.NTemplateClass.GlobalClassSpecifier
                         + " Template { get { return ("
                         + a.NTemplateClass.GlobalClassSpecifier
                         + ")base.Template; } set { base.Template = value; } }");

            if (a.CodebehindClass != null && a.CodebehindClass.BoundDataClass != null) {
                a.Prefix.Add("    " + markAsCodegen);
                a.Prefix.Add("    public new "
                             + a.CodebehindClass.BoundDataClass
                             + " Data { get { return ("
                             + a.CodebehindClass.BoundDataClass
                             + ")base.Data; } set { base.Data = value; } }");
            }
            a.Prefix.Add("    public override bool IsCodegenerated { get { return true; } }");

            foreach (AstBase kid in a.NTemplateClass.Children) {
                var prop = kid as AstProperty;
                if (prop != null && prop.BackingFieldName != null) {
                    string bfTypeName = null;
                    if (prop.Template is TObjArr) {
                        var templateClass = prop.Type as AstTemplateClass;
                        if (templateClass != null)
                            bfTypeName = templateClass.NValueClass.GlobalClassSpecifier;
                    } else if (prop.Template is TObject) {
                        var parent = prop.Type.Parent;
                        while (parent != null) {
                            if (parent is AstJsonClass) {
                                bfTypeName = ((AstJsonClass)parent).GlobalClassSpecifier;
                                break;
                            }
                            parent = parent.Parent;
                        }
                    }

                    if (bfTypeName == null)
                        bfTypeName = HelperFunctions.GetGlobalClassSpecifier(prop.Template.InstanceType, true);

                    a.Prefix.Add("    private "
                                + bfTypeName
                                + " "
                                + prop.BackingFieldName
                                + ";");
                }
            }
            AppendLineDirectiveIfDefined(a.Prefix, "    #line default");
        }

        private AstClass GetParentPropertyType(Template a) {
            var x = Generator.ObtainValueClass((Template)a.Parent);
            return x;
        }

        /// <summary>
        /// Writes the app template member prefix.
        /// </summary>
        /// <param name="m">The m.</param>
        private void WriteTAppMemberPrefix(AstProperty m) {
            var sb = new StringBuilder();
            sb.Append("public ");
            sb.Append(m.Type.GlobalClassSpecifier);
            sb.Append(' ');
            sb.Append(m.MemberName);
            sb.Append(";");
            m.Prefix.Add(sb.ToString());
        }

        /// <summary>
        /// Writes the app metadata member prefix.
        /// </summary>
        /// <param name="m">The m.</param>
        private void WriteObjMetadataMemberPrefix(AstProperty m) {
            var sb = new StringBuilder();
            sb.Append("public ");
            sb.Append(m.Type.GlobalClassSpecifier);
            sb.Append(' ');
            sb.Append(m.MemberName);
            sb.Append(" { get { return __p_");
            sb.Append(m.MemberName);
            sb.Append(" ?? (__p_");
            sb.Append(m.MemberName);
            sb.Append(" = new ");
            sb.Append(m.Type.GlobalClassSpecifier);
            sb.Append('(');
            sb.Append("App"); // Property name .App TODO
            sb.Append(", ");
            sb.Append("App"); // Property name .App TODO
            sb.Append(".Template.");
            sb.Append(m.MemberName);
            sb.Append(")); } }");
            m.Prefix.Add(sb.ToString());

            sb.Clear();
            sb.Append("private ");
            sb.Append(m.Type.GlobalClassSpecifier);
            sb.Append(" __p_");
            sb.Append(m.MemberName);
            sb.Append(';');
            m.Prefix.Add(sb.ToString());
        }

        /// <summary>
        /// Writes the class declaration and constructor for an TApp class
        /// </summary>
        /// <param name="cst">The CST.</param>
        private void WriteTAppConstructor(AstConstructor cst) {
            AstSchemaClass a = (AstSchemaClass)cst.Parent;
            TObjArr tArr;
            TObject tObjElement;
            var sb = new StringBuilder();

            sb.Append("    public ");
            sb.Append(a.ClassStemIdentifier);
            sb.Append("()");
            a.Prefix.Add(sb.ToString());
            a.Prefix.Add("        : base() {");

            if (a.BindChildren != BindingStrategy.Auto) {
                a.Prefix.Add("        BindChildren = st::BindingStrategy." + a.BindChildren + ";");
            }

            sb = new StringBuilder();
            sb.Append("        InstanceType = typeof(");
            sb.Append(a.NValueClass.GlobalClassSpecifier);
            sb.Append(");");
            a.Prefix.Add(sb.ToString());

            sb = new StringBuilder();
            sb.Append("        ClassName = \"");
            sb.Append(a.NValueClass.ClassStemIdentifier);
            sb.Append("\";");
            a.Prefix.Add(sb.ToString());

            if (a.Template is TObject) {
                a.Prefix.Add("        Properties.ClearExposed();");
            }

            tArr = a.Template as TObjArr;
            if (tArr != null) {
                tObjElement = tArr.ElementType as TObject;
                bool isCustomClass = ((tArr.ElementType != null) && (tObjElement != null) && (tObjElement.Properties.Count > 0));
                if (isCustomClass || !"Json".Equals(a.InheritedClass.Generic[0].ClassStemIdentifier)) {
                    sb.Clear();
                    sb.Append("        ");
                    sb.Append("SetCustomGetElementType((arr) => { return ");
                    sb.Append(a.InheritedClass.Generic[0].GlobalClassSpecifier);
                    sb.Append(".DefaultTemplate;});");
                    a.Prefix.Add(sb.ToString());
                }
            }

            foreach (AstBase kid in cst.Children) {
                if (kid is AstProperty) {
                    var mn = kid as AstProperty;
                    TValue tv = mn.Template as TValue;

                    string memberName = mn.MemberName;
                    if (mn.Template == a.Template)
                        memberName = "this";

                    sb = new StringBuilder();

                    if (!"this".Equals(memberName)) {
                        sb.Append("        ");
                        sb.Append(memberName);
                        sb.Append(" = Add<");
                        sb.Append(mn.Type.GlobalClassSpecifier);

                        sb.Append(">(\"");
                        sb.Append(mn.Template.TemplateName);
                        sb.Append('"');
    
                        if (tv != null && tv.BindingStrategy != BindingStrategy.UseParent && tv.BindingStrategy != BindingStrategy.Auto) {
                            if (tv.Bind == null) {
                                sb.Append(", bind:null");
                            } else {
                                sb.Append(", bind:\"");
                                sb.Append(tv.Bind);
                                sb.Append('"');
                            }
                        }
                        sb.Append(");");
                        a.Prefix.Add(sb.ToString());
                    }

                    if (tv != null) {
                        WriteDefaultValue(a, mn, tv, memberName);
                    }

                    if (mn.Template.Editable) {
                        a.Prefix.Add("        " + memberName + ".Editable = true;");
                    }

                    tArr = mn.Template as TObjArr;
                    if (tArr != null) {
                        tObjElement = tArr.ElementType as TObject;
                        bool isCustomClass = ((tArr.ElementType != null) && (tObjElement != null) && (tObjElement.Properties.Count > 0));
                        if (isCustomClass || !"Json".Equals(mn.Type.Generic[0].ClassStemIdentifier)) {
                            sb.Clear();
                            sb.Append("        ");
                            sb.Append(memberName);
                            sb.Append(".SetCustomGetElementType((arr) => { return ");
                            sb.Append(mn.Type.Generic[0].GlobalClassSpecifier);
                            sb.Append(".DefaultTemplate;});");
                            a.Prefix.Add(sb.ToString());
                        }
                    }

                    if (mn.BackingFieldName != null /*&& !(mn.Template is TObjArr)*/) {
                        sb.Clear();
                        sb.Append("        ");
                        sb.Append(memberName);
                        sb.Append(".SetCustomAccessors((_p_) => { return ((");
                        sb.Append(a.NValueClass.GlobalClassSpecifier);
                        sb.Append(")_p_).");
                        sb.Append(mn.BackingFieldName);
                        sb.Append("; }, (_p_, _v_) => { ((");
                        sb.Append(a.NValueClass.GlobalClassSpecifier);
                        sb.Append(")_p_).");
                        sb.Append(mn.BackingFieldName);
                        sb.Append(" = (");

                        AstProperty valueProp = (AstProperty)a.NValueClass.Children.Find((AstBase item) => {
                            var prop = item as AstProperty;
                            if (prop != null && (memberName.Equals("this") || prop.MemberName.Equals(memberName)))
                                return true;
                            return false;
                        });

                        if (valueProp.Template.IsPrimitive) {
                            sb.Append(HelperFunctions.GetGlobalClassSpecifier(valueProp.Template.InstanceType, true));
                        } else {
                            sb.Append(valueProp.Type.GlobalClassSpecifier);
                        }

                        sb.Append(")_v_; }, false);");
                        a.Prefix.Add(sb.ToString());
                    }
                } else if (kid is AstInputBinding) {
                    a.Prefix.Add(GetAddInputHandlerCode((AstInputBinding)kid));
                }
            }
            a.Prefix.Add(
                "    }");
        }

        private void WriteDefaultValue(AstSchemaClass a, AstProperty mn, TValue tv, string memberName) {
            string value = null;

            if (tv is TBool) {
                value = (((TBool)tv).DefaultValue == true) ? "true" : "false";
            } else if (tv is TDouble) {
                value = ((TDouble)tv).DefaultValue.ToString(CultureInfo.InvariantCulture) + "d";
            } else if (tv is TDecimal) {
                value = ((TDecimal)tv).DefaultValue.ToString(CultureInfo.InvariantCulture) + "m";
            } else if (tv is TLong) {
                value = ((TLong)tv).DefaultValue + "L";
            } else if (tv is TString) {
                value = ((TString)tv).DefaultValue;
                if (value == null) value = "null";
                else value = '"' + EscapeStringValue(value) + '"';
            } 
            //else if (tv is TOid) {
            //    value = ((TOid)tv).DefaultValue + "UL";
            //}

            if (value != null) {
                a.Prefix.Add("        " + memberName + ".DefaultValue = " + value + ";");
            }
        }

        private string EscapeStringValue(string input) {
            string result = input;

            // Really slow way of escaping the value so we can write it in generated code,
            // but the code where this is called should not be performance critical
            result = result.Replace("\\", @"\\");    // This needs to be done first!
            result = result.Replace("\"", @"\""");
            result = result.Replace("\a", @"\a");
            result = result.Replace("\b", @"\b");
            result = result.Replace("\f", @"\f");
            result = result.Replace("\n", @"\n");
            result = result.Replace("\r", @"\r");
            result = result.Replace("\t", @"\t");
            result = result.Replace("\v", @"\v");
            result = result.Replace("\0", @"\0");

            return result;
        }

        private void WriteSchemaOverrides(AstSchemaClass node) {
            node.Prefix.Add(
                "    public override object CreateInstance(s.Json parent) { return new "
                + node.NValueClass.GlobalClassSpecifier
                + "(this) { Parent = parent }; }"
            );
        }

        /// <summary>
        /// Gets the add input handler code.
        /// </summary>
        /// <param name="ib">The ib.</param>
        /// <returns>String.</returns>
        private String GetAddInputHandlerCode(AstInputBinding ib) {
            // TODO:
            // Needs to be rewritten for better handling of changes in xson code.

            bool hasValue = ib.HasValue;
            StringBuilder sb = new StringBuilder();
            sb.Append("        ");

            if (ib.BindsToProperty.Type == ib.DeclaringAppClass.NTemplateClass) {
                // Handler for a single value
                sb.Append("this");
            } else {
                sb.Append(ib.BindsToProperty.Template.PropertyName);       // {0}
            }
            sb.Append(".AddHandler((Json pup, ");

            if (hasValue) {
                sb.Append("Property");
                sb.Append('<');
                sb.Append(ib.BindsToProperty.Template.JsonType);   // {1}
                sb.Append('>');
            } else {
                sb.Append("TValue");
            }
            sb.Append(" prop");

            if (hasValue) {
                sb.Append(", ");
                sb.Append(ib.BindsToProperty.Template.JsonType);   // {1}
                sb.Append(" value");
            }
            sb.Append(") => { return (new ");
            sb.Append(ib.InputTypeName);                       // {2}
            sb.Append("() { App = (");
            sb.Append(ib.PropertyAppClass.ClassStemIdentifier);          // {3}
            sb.Append(")pup, Template = (");
            sb.Append(ib.BindsToProperty.Type.ClassStemIdentifier);      // {4}
            sb.Append(")prop");

            if (hasValue) {
                sb.Append(", Value = value");
            }

            sb.Append(" }); }, (Json pup, Starcounter.Input");

            if (hasValue) {
                sb.Append('<');
                sb.Append(ib.BindsToProperty.Template.JsonType);   // {1}
                sb.Append('>');
            }

            sb.Append(" input) => { ((");
            sb.Append(ib.DeclaringAppClass.ClassStemIdentifier);         // {5}
            sb.Append(")pup");

            for (Int32 i = 0; i < ib.AppParentCount; i++) {
                sb.Append(".Parent");
            }

            sb.Append(").Handle((");
            sb.Append(ib.InputTypeName);                       // {2}
            sb.Append(")input); });");

            return sb.ToString();
        }

        /// <summary>
        /// Writes the class declaration and constructor for an TApp class
        /// </summary>
        /// <param name="a">The class declaration syntax node</param>
        private void WriteObjMetadataClassPrefix(AstMetadataClass a) {
            var sb = new StringBuilder();
            sb.Append("    public ");
            sb.Append(a.ClassStemIdentifier);

            sb.Append('(');
            sb.Append(((AstJsonClass)a.NValueClass).GlobalClassSpecifier);
            sb.Append(" obj, ");
            sb.Append(((AstClass)a).GlobalClassSpecifier);
            sb.Append(" template) : base(obj, template) { }");
            a.Prefix.Add(sb.ToString());
        }

        /// <summary>
        /// Writes the header of the CSharp file, including using directives.
        /// </summary>
        /// <param name="fileName">The name of the original json file</param>
        /// <param name="h">The h.</param>
        static internal void WriteHeader(AstRoot root, string fileName, StringBuilder h) {
            if (defaultUsings == null) {
                defaultUsings = new List<string>();
                defaultUsings.Add("System");
                defaultUsings.Add("System.Collections");
                defaultUsings.Add("System.Collections.Generic");
                defaultUsings.Add("Starcounter.Advanced");
                defaultUsings.Add("Starcounter");
                defaultUsings.Add("Starcounter.Internal");
                defaultUsings.Add("Starcounter.Templates");
                defaultUsings.Add("st=Starcounter.Templates");
                defaultUsings.Add("s=Starcounter");
                defaultUsings.Add("_GEN1_=System.Diagnostics.DebuggerNonUserCodeAttribute");
                defaultUsings.Add("_GEN2_=System.CodeDom.Compiler.GeneratedCodeAttribute");
                defaultUsings.Add("_ScTemplate_=Starcounter.Templates.Template");
            }

            h.Append("// This is a system generated file (G2). It reflects the Starcounter App Template defined in the file \"");
            h.Append(fileName);
            h.Append('"');
            h.AppendLine();
            h.AppendLine("// DO NOT MODIFY DIRECTLY - CHANGES WILL BE OVERWRITTEN");
            h.AppendLine();

            foreach (var usingDirective in defaultUsings) {
                h.AppendLine("using " + usingDirective + ";");
            }

            if (root.Generator.CodeBehindMetadata != null) {
                var usingList = root.Generator.CodeBehindMetadata.UsingDirectives;
                foreach (var usingDirective in usingList) {
                    if (!defaultUsings.Contains(usingDirective))
                        h.AppendLine("using " + usingDirective + ";");
                }
            }

            h.AppendLine("#pragma warning disable 0108");
            h.AppendLine("#pragma warning disable 1591");
            h.AppendLine();
        }

        static internal void WriteFooter(StringBuilder f) {
#if !DEBUG
            string s = "apa";
            Console.Write(s);
            s = null;

#endif

            f.AppendLine("#pragma warning restore 1591");
            f.AppendLine("#pragma warning restore 0108");
        }

        [Conditional("ADDLINEDIRECTIVES")]
        private static void AppendLineDirectiveIfDefined(List<string> list, string str) {
            list.Add(str);
        }

        [Conditional("ADDLINEDIRECTIVES")]
        private static void AppendLineDirectiveIfDefined(StringBuilder sb, string str) {
            sb.AppendLine(str);
        }
    }
}
