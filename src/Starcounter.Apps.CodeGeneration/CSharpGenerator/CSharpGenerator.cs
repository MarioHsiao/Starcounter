// ***********************************************************************
// <copyright file="CSharpGenerator.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Templates.Interfaces;
using System.Text;
using System;
using Starcounter.Templates;

namespace Starcounter.Internal.Application.CodeGeneration  {
    /// <summary>
    /// Class CSharpGenerator
    /// </summary>
    public class CSharpGenerator : ITemplateCodeGenerator {
        /// <summary>
        /// The output
        /// </summary>
        internal StringBuilder Output = new StringBuilder();

        /// <summary>
        /// The root
        /// </summary>
        public NRoot Root;
        /// <summary>
        /// Gets or sets the indentation.
        /// </summary>
        /// <value>The indentation.</value>
        public int Indentation { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CSharpGenerator" /> class.
        /// </summary>
        /// <param name="root">The root.</param>
        public CSharpGenerator(NRoot root ) {
            Root = root;
            Indentation = 4;
        }

        /// <summary>
        /// Returns a multiline string representation of the code dom tree
        /// </summary>
        /// <returns>A multiline string</returns>
        public string DumpTree() {
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
        private void DumpTree( StringBuilder sb, NBase node, int indent ) {
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
            //return Old.GenerateCodeOld();
            ProcessAllNodes();

            WriteHeader(Root.AppClassClassNode.Template.CompilerOrigin.FileName, Output);
            foreach (NAppClass napp in Root.Children)
            {
                WriteNode(napp);
            }

            //return DumpTree();
            return Output.ToString();
        }

        /// <summary>
        /// Processes all nodes.
        /// </summary>
        /// <exception cref="System.Exception">Unable to generate code. Invalid node found. Expected App but found: </exception>
        private void ProcessAllNodes() {
            NAppClass napp;
            NAppClass previousAppClass;
            String previousNs;
            String currentNs;

            previousAppClass = null;
            previousNs = "";
            for (Int32 i = 0; i < Root.Children.Count; i++)
            {
                napp = Root.Children[i] as NAppClass;
                if (napp == null)
                {
                    throw new Exception("Unable to generate code. Invalid node found. Expected App but found: " + Root.Children[i]);
                }

                currentNs = napp.Template.Namespace;
                if (currentNs != previousNs)
                {
                    if (previousAppClass != null && !String.IsNullOrEmpty(previousNs))
                    {
                        previousAppClass.Suffix.Add("}");
                    }

                    if (!String.IsNullOrEmpty(currentNs))
                    {
                        napp.Prefix.Add("namespace " + currentNs + " {");
                    }
                }

                ProcessNode(napp);

                previousNs = currentNs;
                previousAppClass = napp;
            }

            if (previousAppClass != null && !String.IsNullOrEmpty(previousNs))
            {
                previousAppClass.Suffix.Add("}");
            }
        }

        /// <summary>
        /// Create prefix and suffix strings for a node and its children
        /// </summary>
        /// <param name="node">The syntax tree node. Use root to generate the complete source code.</param>
        private void ProcessNode(NBase node) {
            var sb = new StringBuilder();
            if (node is NClass) {
                if (node is NAppMetadata) {
                    var n = node as NAppMetadata;
                    sb.Append("public class ");
                    sb.Append(n.ClassName);
                    if (n.Inherits != null) {
                        sb.Append(" : ");
                        sb.Append(n.Inherits);
                    }
                    sb.Append(" {");
                    node.Prefix.Add(sb.ToString());
                    if (node is NAppMetadata) {
                        WriteAppMetadataClassPrefix(node as NAppMetadata);
                    }
                }
                else {
                    var n = node as NClass;
                    sb.Append("public ");
                    if (n.IsStatic) {
                        sb.Append("static ");
                    }
                    if (n.IsPartial) {
                        sb.Append("partial ");
                    }
                    sb.Append("class ");
                    sb.Append(n.ClassName);
                    if (n.Inherits != null) {
                        sb.Append(" : ");
                        sb.Append(n.Inherits);
                    }
                    sb.Append(" {");
                    node.Prefix.Add(sb.ToString());
                    if (node is NAppClass) {
                        WriteAppClassPrefix(node as NAppClass);
                    }
                    else if (node is NAppTemplateClass) {
                        WriteAppTemplateConstructor((node as NAppTemplateClass).Constructor);
                        WriteAppTemplateCreateInstance(node as NAppTemplateClass);
                    }
                }
                node.Suffix.Add("}");
            }
            else if (node is NProperty) {
                if (node.Parent is NAppClass)
                    WriteAppMemberPrefix(node as NProperty);
                else if (node.Parent is NAppTemplateClass)
                    WriteAppTemplateMemberPrefix(node as NProperty);
                else if (node.Parent is NAppMetadata)
                    WriteAppMetadataMemberPrefix(node as NProperty);
            }
            foreach (var kid in node.Children) {
                ProcessNode(kid);
            }
        }

        /// <summary>
        /// Writes the node.
        /// </summary>
        /// <param name="node">The node.</param>
        private void WriteNode( NBase node ) {
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
        private void WriteAppMemberPrefix(NProperty m) {
            var sb = new StringBuilder();
            sb.Append("public ");
            sb.Append(m.Type.FullClassName);
            sb.Append(' ');
            sb.Append(m.MemberName);
            sb.Append(" { get { return GetValue");
//            if (m.Type is NListing) {
//                sb.Append('<');
//                sb.Append(((NListing)m.Type).NApp.FullClassName);
//                sb.Append('>');
//            }
            if (m.FunctionGeneric != null) {
                sb.Append('<');
                sb.Append( m.FunctionGeneric.FullClassName );
                sb.Append('>');
            }
            sb.Append("(Template.");
            sb.Append(m.MemberName);
            sb.Append("); } set { SetValue");
            if (m.Type is NListingXXXClass) {
                sb.Append('<');
                sb.Append(((NListingXXXClass)m.Type).NApp.FullClassName);
                sb.Append('>');
            }
            sb.Append("(Template.");
            sb.Append(m.MemberName);
            sb.Append(", value); } }");
            m.Prefix.Add(sb.ToString());

            if (m.Bound) {
                WriteBoundGetterAndSetter(m);
            }
        }

        /// <summary>
        /// Writes a getter and setter bound to the underlying entity using
        /// either the name from Bind or the same name as the property.
        /// </summary>
        /// <param name="m"></param>
        private void WriteBoundGetterAndSetter(NProperty m) {
            String bindTo;
            StringBuilder sb;

            // TODO: 
            // Add support for binding directly to en Enumerable of entities.
            sb = new StringBuilder();
           
            bindTo = (String.IsNullOrEmpty(m.Template.Bind)) ? m.MemberName : m.Template.Bind;
           
            sb.Append("private ");
            sb.Append(m.Type.FullClassName);
            sb.Append(" __data_");
            sb.Append(m.MemberName);
            sb.Append(" { get { return (");
            sb.Append(m.Type.FullClassName);
            sb.Append(")Data.");
            sb.Append(bindTo);
            sb.Append("; } ");

            // TODO:
            // We need another parameter saying if it's bound to a readonly property or not.
            if (m.Template.Editable) {
                sb.Append(" set { Data.");
                sb.Append(bindTo);
                sb.Append(" = value; } }");
            } else {
                sb.Append('}');
            }

            m.Prefix.Add(sb.ToString());
        }

        /// <summary>
        /// Writes the app class prefix.
        /// </summary>
        /// <param name="a">A.</param>
        private void WriteAppClassPrefix(NAppClass a) {
            a.Prefix.Add(
                "    public static " +
                a.NTemplateClass.ClassName +
                " DefaultTemplate = new " +
                a.NTemplateClass.ClassName +
                "();");
/*            var sb = new StringBuilder();

            sb.Append("    public ");
            sb.Append(a.ClassName);
            sb.Append("( Entity data ) {");
            a.Prefix.Add(sb.ToString());
            sb = new StringBuilder();
            sb.Append("        Data = data;");
            a.Prefix.Add(sb.ToString());
            sb = new StringBuilder();
            sb.Append("    }");
            a.Prefix.Add(sb.ToString());
            */

            a.Prefix.Add(
                "    public " +
                a.ClassName +
                "() {");
            a.Prefix.Add(
                "        Template = DefaultTemplate;");
            a.Prefix.Add(
                "    }");
            a.Prefix.Add(
                "    public new " +
                a.NTemplateClass.ClassName +
                " Template { get { return (" +
                a.NTemplateClass.ClassName +
                ")base.Template; } set { base.Template = value; } }");
            a.Prefix.Add(
                "    public new " +
                a.NTemplateClass.NMetadataClass.ClassName +
                " Metadata { get { return (" +
                a.NTemplateClass.NMetadataClass.ClassName +
                ")base.Metadata; } }");
        }


        /// <summary>
        /// Writes the app template member prefix.
        /// </summary>
        /// <param name="m">The m.</param>
        private void WriteAppTemplateMemberPrefix(NProperty m) {
            var sb = new StringBuilder();
            sb.Append("public ");
            sb.Append(m.Type.FullClassName);
            sb.Append(' ');
            sb.Append(m.MemberName);
            sb.Append(";");
            m.Prefix.Add(sb.ToString());

            if (m.Bound) {
                sb.Clear();
                sb.Append("private ");
                sb.Append(m.Template.InstanceType.Name);
                sb.Append(" __data_get_");
                sb.Append(m.MemberName);
                sb.Append("(App app) { return ((");
                sb.Append(((NAppTemplateClass)m.Parent).NValueClass.FullClassName);
                sb.Append(")app).__data_");
                sb.Append(m.MemberName);
                sb.Append("; }");
                m.Prefix.Add(sb.ToString());

                if (m.Template.Editable) {
                    sb.Clear();
                    sb.Append("private void ");
                    sb.Append("__data_set_");
                    sb.Append(m.MemberName);
                    sb.Append("(App app, ");
                    sb.Append(m.Template.InstanceType.Name);
                    sb.Append(" value) { ((");
                    sb.Append(((NAppTemplateClass)m.Parent).NValueClass.FullClassName);
                    sb.Append(")app).__data_");
                    sb.Append(m.MemberName);
                    sb.Append(" = value; }");
                    m.Prefix.Add(sb.ToString());
                }
            }
        }

        /// <summary>
        /// Writes the app metadata member prefix.
        /// </summary>
        /// <param name="m">The m.</param>
        private void WriteAppMetadataMemberPrefix(NProperty m) {
            var sb = new StringBuilder();
            sb.Append("public ");
            sb.Append(m.Type.FullClassName);
            sb.Append(' ');
            sb.Append(m.MemberName);
            sb.Append(" { get { return __p_");
            sb.Append(m.MemberName);
            sb.Append(" ?? (__p_");
            sb.Append(m.MemberName);
            sb.Append(" = new ");
            sb.Append(m.Type.FullClassName);
            sb.Append("(App, App.Template.");
            sb.Append(m.MemberName);
            sb.Append(")); } } private ");
            sb.Append(m.Type.FullClassName);
            sb.Append(" __p_");
            sb.Append(m.MemberName);
            sb.Append(';');
            m.Prefix.Add(sb.ToString());
        }

        /// <summary>
        /// Writes override method for creating default appinstance from template.
        /// </summary>
        /// <param name="node"></param>
        private void WriteAppTemplateCreateInstance(NAppTemplateClass node) {
            StringBuilder sb = new StringBuilder();
            sb.Append("    public override object CreateInstance(AppNode parent) { return new ");
            sb.Append(node.NValueClass.ClassName);
            sb.Append("() { Parent = parent }; }");
            node.Prefix.Add(sb.ToString());
        }

        /// <summary>
        /// Writes the class declaration and constructor for an AppTemplate class
        /// </summary>
        /// <param name="cst">The CST.</param>
        private void WriteAppTemplateConstructor(NConstructor cst) {
            NAppTemplateClass a = (NAppTemplateClass)cst.Parent;

            var sb = new StringBuilder();
            sb.Append("    public ");
            sb.Append(a.ClassName);
            sb.Append("()");
            a.Prefix.Add(sb.ToString());
            a.Prefix.Add("        : base() {");
            sb = new StringBuilder();
            sb.Append("        InstanceType = typeof(");
            sb.Append(a.NValueClass.FullClassName);
            sb.Append(");");
            a.Prefix.Add(sb.ToString());
            
            sb = new StringBuilder();
            sb.Append("        ClassName = \"");
            sb.Append(a.NValueClass.ClassName);
            sb.Append("\";");
            a.Prefix.Add(sb.ToString());

            foreach (NBase kid in cst.Children)
            {
                if (kid is NProperty)
                {
                    var mn = kid as NProperty;
                    sb = new StringBuilder();
                    sb.Append("        ");
                    sb.Append(mn.MemberName);
                    sb.Append(" = Register<");
                    sb.Append(mn.Type.FullClassName);

                    if (mn.Bound) {
                        sb.Append(", ");
                        sb.Append(mn.Template.InstanceType.Name);
                    }

                    sb.Append(">(\"");
                    sb.Append(mn.MemberName);
                    sb.Append('"');

                    if (mn.Bound) {
                        sb.Append(", __data_get_");
                        sb.Append(mn.MemberName);

                        if (mn.Template.Editable) {
                            sb.Append(", __data_set_");
                            sb.Append(mn.MemberName);
                        }
                    }

                    if (mn.Template.Editable)
                    {
                        sb.Append(", Editable = true);");
                    }
                    else
                    {
                        sb.Append(");");
                    }
                    a.Prefix.Add(sb.ToString());
                }
                else if (kid is NInputBinding)
                {
                    a.Prefix.Add(GetAddInputHandlerCode((NInputBinding)kid));
                }
            }
            a.Prefix.Add(
                "    }");
        }

        /// <summary>
        /// Gets the add input handler code.
        /// </summary>
        /// <param name="ib">The ib.</param>
        /// <returns>String.</returns>
        private String GetAddInputHandlerCode(NInputBinding ib)
        {
            bool hasValue = ib.HasValue;
            StringBuilder sb = new StringBuilder();
            sb.Append("        ");
            sb.Append(ib.BindsToProperty.Template.Name);       // {0}
            sb.Append(".AddHandler((App app, Property");

            if (hasValue) {
                sb.Append('<');
                sb.Append(ib.BindsToProperty.Template.JsonType);   // {1}
                sb.Append('>');
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
            sb.Append(ib.PropertyAppClass.ClassName);          // {3}
            sb.Append(")app, Template = (");
            sb.Append(ib.BindsToProperty.Type.ClassName);      // {4}
            sb.Append(")prop");
            
            if (hasValue)
            {
                sb.Append(", Value = value");
            }

            sb.Append(" }); }, (App app, Starcounter.Input");

            if (hasValue) {
                sb.Append('<');
                sb.Append(ib.BindsToProperty.Template.JsonType);   // {1}
                sb.Append('>');
            }

            sb.Append(" input) => { ((");
            sb.Append(ib.DeclaringAppClass.ClassName);         // {5}
            sb.Append(")app");

            for (Int32 i = 0; i < ib.AppParentCount; i++)
            {
                sb.Append(".Parent");
            }

            sb.Append(").Handle((");
            sb.Append(ib.InputTypeName);                       // {2}
            sb.Append(")input); });");

            return sb.ToString();
        }

        /// <summary>
        /// Writes the class declaration and constructor for an AppTemplate class
        /// </summary>
        /// <param name="a">The class declaration syntax node</param>
        private void WriteAppMetadataClassPrefix(NMetadataClass a) {
            var sb = new StringBuilder();
            sb.Append("    public ");
            sb.Append(a.ClassName);
            sb.Append("(App app, AppTemplate template) : base(app, template) { }");
            a.Prefix.Add(sb.ToString());
            sb = new StringBuilder();
            sb.Append("    public new ");
            sb.Append(a.NTemplateClass.NValueClass.FullClassName);
            sb.Append(" App { get { return (");
            sb.Append(a.NTemplateClass.NValueClass.FullClassName);
            sb.Append(")base.App; } }");
            a.Prefix.Add(sb.ToString());
            sb = new StringBuilder();
            sb.Append("    public new ");
            sb.Append(a.NTemplateClass.FullClassName);
            sb.Append(" Template { get { return (");
            sb.Append(a.NTemplateClass.FullClassName);
            sb.Append(")base.Template; } }");
            a.Prefix.Add(sb.ToString());
            /*            sb = new StringBuilder();
                        sb.Append("        InstanceType = typeof(");
                        sb.Append(a.AppNode.FullClassName);
                        sb.Append(");");
                        a.Prefix.Add(sb.ToString());
                        sb = new StringBuilder();
                        sb.Append("        ClassName = \"");
                        sb.Append(a.AppNode.ClassName);
                        sb.Append("\";");
                        a.Prefix.Add(sb.ToString());
                        foreach (var kid in a.Children) {
                            if (kid is MemberNode) {
                                var mn = kid as MemberNode;
                                sb = new StringBuilder();
                                sb.Append("        ");
                                sb.Append(mn.MemberName);
                                sb.Append(" = Register<");
                                sb.Append(mn.Type.FullClassName);
                                sb.Append(">( Name = \"");
                                sb.Append(mn.MemberName);
                                sb.Append('"');
                                if (mn.Template.Editable) {
                                    sb.Append(", Editable = true );");
                                }
                                else {
                                    sb.Append(" );");
                                }
                                a.Prefix.Add(sb.ToString());
                            }
                        }
                        a.Prefix.Add(
                            "    }");
             */
        }


        /// <summary>
        /// Writes the header of the CSharp file, including using directives.
        /// </summary>
        /// <param name="fileName">The name of the original json file</param>
        /// <param name="h">The h.</param>
        static internal void WriteHeader( string fileName, StringBuilder h ) {
            h.Append("// This is a system generated file. It reflects the Starcounter App Template defined in the file \"");
            h.Append(fileName);
            h.Append('"');
            h.Append('\n');
            h.Append("// DO NOT MODIFY DIRECTLY - CHANGES WILL BE OVERWRITTEN\n");
            h.Append('\n');
            h.Append("using System;\n");
            h.Append("using System.Collections.Generic;\n");
            h.Append("using Starcounter;\n");
            h.Append("using Starcounter.Internal;\n");
            h.Append("using Starcounter.Templates;\n");
            h.Append('\n');
        }
    }
}
