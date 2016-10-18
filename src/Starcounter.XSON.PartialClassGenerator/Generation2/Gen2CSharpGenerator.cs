// Remove this define to generate code without #line directives
#define ADDLINEDIRECTIVES

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using Starcounter.Internal;
using Starcounter.Templates;
using Starcounter.XSON.Interfaces;

namespace Starcounter.XSON.PartialClassGenerator {
    /// <summary>
    /// Generates C# code from an AST tree.
    /// </summary>
    public class Gen2CSharpGenerator : ITemplateCodeGenerator {
        private const string MARKASCODEGEN = "[_GEN1_][_GEN2_(\"Starcounter\",\"2.0\")]";
        private const string MARKASCODEGEN2 = "[_GEN2_(\"Starcounter\",\"2.0\")]";

        private const string LINE_HIDDEN = "#line hidden";
        private const string LINE_DEFAULT = "#line default";
        private const string LINE_POSANDFILE = "#line {0} \"{1}\"";

        /// <summary>
        /// A list of predefined using-directives that should always be added. 
        /// </summary>
        private static List<string> defaultUsings;

        /// <summary>
        /// The generated code output.
        /// </summary>
        private StringBuilder output;

        /// <summary>
        /// The root of the generated code.
        /// </summary>
        private AstRoot root;

        /// <summary>
        /// Gets or sets the indentation.
        /// </summary>
        private int indentation;

        /// <summary>
        /// The code generator can be used to generate typed JSON.
        /// </summary>
        private Gen2DomGenerator generator;
        
        static Gen2CSharpGenerator() {
            defaultUsings = new List<string>();
            defaultUsings.Add("System");
            defaultUsings.Add("System.Collections");
            defaultUsings.Add("System.Collections.Generic");
            defaultUsings.Add("Starcounter.Advanced");
            defaultUsings.Add("Starcounter");
            defaultUsings.Add("Starcounter.Internal");
            defaultUsings.Add("Starcounter.Templates");
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="generator"></param>
        /// <param name="root"></param>
        public Gen2CSharpGenerator(Gen2DomGenerator generator, AstRoot root) {
            this.generator = generator;
            this.root = root;
            this.output = new StringBuilder();
            this.indentation = 4;
        }

        public IEnumerable<ITemplateCodeGeneratorWarning> Warnings {
            get {
                return this.generator.Warnings;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public IReadOnlyTree GenerateAST() {
            return root;
        }

        /// <summary>
        /// Returns a multiline string representation of the code dom tree
        /// </summary>
        /// <returns>A multiline string</returns>
        public string DumpAstTree() {
            var sb = new StringBuilder();
            DumpTree(sb, root, 0);
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

            bool writeRootNs = !string.IsNullOrEmpty(root.AppClassClassNode.Namespace);

            WriteHeader();

            if (writeRootNs)
                output.AppendLine("namespace " + root.AppClassClassNode.Namespace + " {");
            foreach (var napp in root.Children) {
                WriteNode(napp);
            }
            if (writeRootNs)
                output.AppendLine("}");

            WriteFooter();
            return output.ToString();
        }

        /// <summary>
        /// Writes all nodes recursively.
        /// </summary>
        /// <param name="node">The node.</param>
        private void WriteNode(AstBase node) {
            foreach (var x in node.Prefix) {
                output.Append(' ', node.Indentation);
                output.Append(x);
                output.Append('\n');
            }
            foreach (var kid in node.Children) {
                kid.Indentation = node.Indentation + indentation;
                WriteNode(kid);
            }
            foreach (var x in node.Suffix) {
                output.Append(' ', node.Indentation);
                output.Append(x);
                output.Append('\n');
            }
        }

        /// <summary>
        /// Writes the header of the CSharp file, including using directives.
        /// </summary>
        private void WriteHeader() {
            output.Append("// This is a system generated file (G2). It reflects the Starcounter App Template defined in the file \"");
            output.Append(root.AppClassClassNode.Template.CodegenInfo.SourceInfo.Filename);
            output.Append('"');
            output.AppendLine();
            output.Append("// Version: ");
            output.Append(PartialClassGenerator.CSHARP_CODEGEN_VERSION);
            output.AppendLine();
            output.AppendLine("// DO NOT MODIFY DIRECTLY - CHANGES WILL BE OVERWRITTEN");
            output.AppendLine();

            foreach (var usingDirective in defaultUsings) {
                output.AppendLine("using " + usingDirective + ";");
            }

            if (root.Generator.CodeBehindMetadata != null) {
                var usingList = root.Generator.CodeBehindMetadata.UsingDirectives;
                foreach (var usingDirective in usingList) {
                    if (!defaultUsings.Contains(usingDirective))
                        output.AppendLine("using " + usingDirective + ";");
                }
            }

            output.AppendLine("#pragma warning disable 0108");
            output.AppendLine("#pragma warning disable 1591");
            output.AppendLine();
        }

        /// <summary>
        /// Writes the footer of the CSharp file.
        /// </summary>
        private void WriteFooter() {
            output.AppendLine("#pragma warning restore 1591");
            output.AppendLine("#pragma warning restore 0108");
        }

        /// <summary>
        /// Processes all nodes and generates C# code.
        /// </summary>
        private void ProcessAllNodes() {
            AstJsonClass napp;
            AstBase previousKidWithNs;
            String previousNs;
            String currentNs = null;

            previousKidWithNs = null;
            previousNs = root.AppClassClassNode.Namespace;
            for (Int32 i = 0; i < root.Children.Count; i++) {
                var kid = root.Children[i];

                if (kid is AstJsonClass) {
                    // Special handling for namespaces, since we need to keep track of the 
                    // previous namespace (if any exists).
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
                } 
                ProcessNode(kid);
            }

            if (previousKidWithNs != null && !String.IsNullOrEmpty(previousNs)) {
                previousKidWithNs.Suffix.Add("}");
            }
        }

        /// <summary>
        /// Create prefix and suffix strings for a node and its children containing C# code
        /// </summary>
        /// <param name="node">
        /// The syntax tree node. Use root to generate the complete source code.
        /// </param>
        private void ProcessNode(AstBase node) {
            var sb = new StringBuilder();

            if (node is AstJsonClass) {
                ProcessJsonClass(node as AstJsonClass);
            } else if (node is AstSchemaClass) {
                ProcessSchemaClass((AstSchemaClass)node);
            } else if (node is AstMetadataClass) {
                ProcessMetadataClass(node as AstMetadataClass);
            } else if (node is AstClass) {
                ProcessClassDeclaration((AstClass)node);
            } else if (node is AstProperty) {
                if (node.Parent is AstJsonClass)
                    ProcessJsonProperty(node as AstProperty);
                else if (node.Parent is AstSchemaClass)
                    ProcessSchemaProperty(node as AstProperty);
                else if (node.Parent is AstMetadataClass)
                    ProcessMetadataProperty(node as AstProperty);
            } else if (node is AstClassAlias) {
                ProcessClassAlias((AstClassAlias)node);
            }

            foreach (var kid in node.Children) {
                ProcessNode(kid);
            }
        }
        
        /// <summary>
        /// Generates code for the member that always should be added (not properties) 
        /// and constructors of the jsonclass.
        /// </summary>
        /// <param name="jsonClass">The class </param>
        private void ProcessJsonClass(AstJsonClass jsonClass) {
            var sb = new StringBuilder();

            // Declaration of the class itself, including inheritance.
            ProcessClassDeclaration(jsonClass);

            AppendLineDirectiveIfDefined(jsonClass.Prefix, LINE_HIDDEN, 4);

            // Static instance of the default template.
            jsonClass.Prefix.Add("    " + MARKASCODEGEN2);
            jsonClass.Prefix.Add("    public static "
                         + jsonClass.NTemplateClass.GlobalClassSpecifier
                         + " DefaultTemplate = new "
                         + jsonClass.NTemplateClass.GlobalClassSpecifier
                         + "();");

            // Default (parameterless) constructor.
            jsonClass.Prefix.Add("    " + MARKASCODEGEN);
            jsonClass.Prefix.Add("    public "
                         + jsonClass.ClassStemIdentifier
                         + "() { }");

            // Constructor with template as parameter.
            jsonClass.Prefix.Add("    " + MARKASCODEGEN);
            jsonClass.Prefix.Add("    public "
                         + jsonClass.ClassStemIdentifier
                         + "(" +
                         jsonClass.NTemplateClass.GlobalClassSpecifier +
                         " template) { Template = template; }");

            //
            jsonClass.Prefix.Add("    " + MARKASCODEGEN);
            jsonClass.Prefix.Add("    protected override _ScTemplate_ GetDefaultTemplate() { return DefaultTemplate; }");

            // Overwriting the property 'Template' in class Json to return template 
            // with the correct type to avoid need for casting.
            jsonClass.Prefix.Add("    " + MARKASCODEGEN);
            jsonClass.Prefix.Add("    public new "
                         + jsonClass.NTemplateClass.GlobalClassSpecifier
                         + " Template { get { return ("
                         + jsonClass.NTemplateClass.GlobalClassSpecifier
                         + ")base.Template; } set { base.Template = value; } }");

            if (jsonClass.CodebehindClass != null && jsonClass.CodebehindClass.BoundDataClass != null) {
                // Overwriting the property 'Data' in class Json to return object
                // with the correct type to avoid need for casting.
                jsonClass.Prefix.Add("    " + MARKASCODEGEN);
                jsonClass.Prefix.Add("    public new "
                             + jsonClass.CodebehindClass.BoundDataClass
                             + " Data { get { return ("
                             + jsonClass.CodebehindClass.BoundDataClass
                             + ")base.Data; } set { base.Data = value; } }");
            }
            jsonClass.Prefix.Add("    public override bool IsCodegenerated { get { return true; } }");
            
            AppendLineDirectiveIfDefined(jsonClass.Prefix, LINE_DEFAULT, 4);
        }

        /// <summary>
        /// Process the schemaclass and adds constructor code and overrides.
        /// </summary>
        /// <param name="schemaClass"></param>
        private void ProcessSchemaClass(AstSchemaClass schemaClass) {
            ProcessClassDeclaration(schemaClass);

            // TODO:
            // See TODO in GeneratorPhase1.cs
            ProcessSchemaConstructor(schemaClass.Constructor);

            schemaClass.Prefix.Add(
                "    public override object CreateInstance(s.Json parent) { return new "
                + schemaClass.NValueClass.GlobalClassSpecifier
                + "(this) { Parent = parent }; }"
            );
        }
        
        /// <summary>
        /// Generates code for a class declaration including inheritance.
        /// </summary>
        /// <param name="astClass"></param>
        private void ProcessClassDeclaration(AstClass astClass) {
            StringBuilder sb = new StringBuilder();

            astClass.Prefix.Add("");
            AppendLineDirectiveIfDefined(astClass.Prefix, LINE_HIDDEN, 0);

            if (astClass.MarkAsCodegen) {
                astClass.Prefix.Add(MARKASCODEGEN);
            }

            sb.Append("public ");
            
            if (astClass.IsStatic) {
                sb.Append("static ");
            }
            if (astClass.IsPartial) {
                sb.Append("partial ");
            }
            sb.Append("class ");
            sb.Append(astClass.ClassStemIdentifier);

            if (astClass.Inherits != null) {
                sb.Append(" : ");
                sb.Append(astClass.Inherits);
            }
            sb.Append(" {");

            astClass.Prefix.Add(sb.ToString());

            astClass.Suffix.Add("}");
            AppendLineDirectiveIfDefined(astClass.Suffix, LINE_DEFAULT, 0);
        }
        
        /// <summary>
        /// Generates code for a property of a jsonclass.
        /// </summary>
        /// <param name="m">The m.</param>
        private void ProcessJsonProperty(AstProperty m) {
            var sb = new StringBuilder();

            bool appendMemberName = (m.Template != ((AstJsonClass)m.Parent).Template);
            string memberTypeName;
            
            if (m.Template.IsPrimitive) {
                memberTypeName = HelperFunctions.GetGlobalClassSpecifier(m.Template.InstanceType, true);
            } else {
                memberTypeName = m.Type.GlobalClassSpecifier;
            }
            
            // Adding a backingfield for locally caching the value.
            if (m.BackingFieldName != null) {
                AppendLineDirectiveIfDefined(m.Prefix, LINE_HIDDEN, 0);
                m.Prefix.Add("private " + memberTypeName + " " + m.BackingFieldName + ";");
                AppendLineDirectiveIfDefined(m.Prefix, LINE_DEFAULT, 0);
            }

            if (!m.GenerateAccessorProperty)
                return;
        
            // Adding public accessors for the property.
            m.Prefix.Add(MARKASCODEGEN);
            sb.Append("public ");
            sb.Append(memberTypeName);
            sb.Append(' ');
            sb.Append(m.MemberName);
            sb.Append(" {");
            m.Prefix.Add(sb.ToString());
            sb.Clear();

            AppendLineDirectiveIfDefined(m.Prefix, GetLinePosAndFile(m.Template), 0);
            
            m.Prefix.Add("    get {");
            AppendLineDirectiveIfDefined(m.Prefix, LINE_HIDDEN, 4);
            
            sb.Append("        return ");
            
            // AstJsonClass can be both object and array.
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
            sb.Append(".Getter(this); }");

            m.Prefix.Add(sb.ToString());
            sb.Clear();
            
            AppendLineDirectiveIfDefined(m.Prefix, GetLinePosAndFile(m.Template), 4);
            m.Prefix.Add("    set {");
            AppendLineDirectiveIfDefined(m.Prefix, LINE_HIDDEN, 4);
            sb.Append("        Template");
            if (appendMemberName) {
                sb.Append('.');
                sb.Append(m.MemberName);
            } 
            sb.Append(".Setter(this, value); } }");
            m.Prefix.Add(sb.ToString());
            sb.Clear();

            AppendLineDirectiveIfDefined(m.Prefix, LINE_DEFAULT, 4);
        }
    
        /// <summary>
        /// Generates code for a member of a schema class.
        /// </summary>
        /// <param name="m">The m.</param>
        private void ProcessSchemaProperty(AstProperty m) {
            var sb = new StringBuilder();
            sb.Append("public ");
            sb.Append(m.Type.GlobalClassSpecifier);
            sb.Append(' ');
            sb.Append(m.MemberName);
            sb.Append(";");
            m.Prefix.Add(sb.ToString());
        }
        
        /// <summary>
        /// Generates code for the constructor of the schema.
        /// </summary>
        /// <param name="cst">The AST node for the constructor</param>
        private void ProcessSchemaConstructor(AstConstructor cst) {
            TObjArr tArr;
            TObject tObjElement;
            AstSchemaClass schemaClass = (AstSchemaClass)cst.Parent;
            var sb = new StringBuilder();

            // Declaration of the constructor.
            sb.Append("    public ");
            sb.Append(schemaClass.ClassStemIdentifier);
            sb.Append("()");
            schemaClass.Prefix.Add(sb.ToString());
            schemaClass.Prefix.Add("        : base() {");

            // Initiation of settings for the schema itself.
            if (schemaClass.BindChildren != BindingStrategy.Auto) {
                schemaClass.Prefix.Add("        BindChildren = st::BindingStrategy." + schemaClass.BindChildren + ";");
            }

            sb.Clear();
            sb.Append("        InstanceType = typeof(");
            sb.Append(schemaClass.NValueClass.GlobalClassSpecifier);
            sb.Append(");");
            schemaClass.Prefix.Add(sb.ToString());
            
            if (schemaClass.Template is TObject) {
                schemaClass.Prefix.Add("        Properties.ClearExposed();");
            }

            tArr = schemaClass.Template as TObjArr;
            if (tArr != null) {
                tObjElement = tArr.ElementType as TObject;
                bool isCustomClass = ((tArr.ElementType != null) && (tObjElement != null) && (tObjElement.Properties.Count > 0));
                if (isCustomClass || !"Json".Equals(schemaClass.InheritedClass.Generic[0].ClassStemIdentifier)) {
                    sb.Clear();
                    sb.Append("        ");
                    sb.Append("SetCustomGetElementType((arr) => { return ");
                    sb.Append(schemaClass.InheritedClass.Generic[0].GlobalClassSpecifier);
                    sb.Append(".DefaultTemplate;});");
                    schemaClass.Prefix.Add(sb.ToString());
                }
            }

            // Adding initialization code for each member of the constructor.
            foreach (AstBase kid in cst.Children) {
                if (kid is AstProperty)
                    ProcessSchemaConstructorProperty((AstProperty)kid, schemaClass);
                else if (kid is AstInputBinding)
                    ProcessInputbinding((AstInputBinding)kid, schemaClass);
            }

            schemaClass.Prefix.Add("    }");
        }

        private void ProcessSchemaConstructorProperty(AstProperty property, AstSchemaClass schemaClass) {
            TObjArr tArr;
            TObject tObjElement;
            TValue tv = property.Template as TValue;

            string memberName = property.MemberName;
            if (property.Template == schemaClass.Template)
                memberName = "this";

            var sb = new StringBuilder();

            // Adding the property to the schema.
            if (!"this".Equals(memberName)) {
                sb.Append("        ");
                sb.Append(memberName);
                sb.Append(" = Add<");
                sb.Append(property.Type.GlobalClassSpecifier);

                sb.Append(">(\"");
                sb.Append(property.Template.TemplateName);
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
                schemaClass.Prefix.Add(sb.ToString());
            }

            if (tv != null) {
                AddDefaultValue(schemaClass, property, tv, memberName);
            }

            if (property.Template.Editable) {
                schemaClass.Prefix.Add("        " + memberName + ".Editable = true;");
            }

            // Special handling of arrays. Specify the ElementType of the array if it is specified.
            tArr = property.Template as TObjArr;
            if (tArr != null) {
                tObjElement = tArr.ElementType as TObject;
                bool isCustomClass = ((tArr.ElementType != null) && (tObjElement != null) && (tObjElement.Properties.Count > 0));
                if (isCustomClass || !"Json".Equals(property.Type.Generic[0].ClassStemIdentifier)) {
                    sb.Clear();
                    sb.Append("        ");
                    sb.Append(memberName);
                    sb.Append(".SetCustomGetElementType((arr) => { return ");
                    sb.Append(property.Type.Generic[0].GlobalClassSpecifier);
                    sb.Append(".DefaultTemplate;});");
                    schemaClass.Prefix.Add(sb.ToString());
                }
            }

            // Override unbound getter and setter delegate to point to the backingfield if it exists.
            if (property.BackingFieldName != null) {
                sb.Clear();
                sb.Append("        ");
                sb.Append(memberName);
                sb.Append(".SetCustomAccessors((_p_) => { return ((");
                sb.Append(schemaClass.NValueClass.GlobalClassSpecifier);
                sb.Append(")_p_).");
                sb.Append(property.BackingFieldName);
                sb.Append("; }, (_p_, _v_) => { ((");
                sb.Append(schemaClass.NValueClass.GlobalClassSpecifier);
                sb.Append(")_p_).");
                sb.Append(property.BackingFieldName);
                sb.Append(" = (");

                AstProperty valueProp = (AstProperty)schemaClass.NValueClass.Children.Find((AstBase item) => {
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
                schemaClass.Prefix.Add(sb.ToString());
            }
        }
        
        /// <summary>
        /// Generates code for assigning delegates for inputhandling.
        /// </summary>
        /// <param name="inputBinding"></param>
        /// <param name="schemaClass"></param>
        private void ProcessInputbinding(AstInputBinding inputBinding, AstSchemaClass schemaClass) {
            StringBuilder sb = new StringBuilder();
            sb.Append("        ");

            if (inputBinding.BindsToProperty.Type == inputBinding.DeclaringAppClass.NTemplateClass) {
                sb.Append("this"); // Handler for a single value
            } else {
                sb.Append(inputBinding.BindsToProperty.Template.PropertyName);       // {0}
            }
            sb.Append(".AddHandler((Json pup, ");
            sb.Append("Property");
            sb.Append('<');
            sb.Append(inputBinding.BindsToProperty.Template.JsonType);   // {1}
            sb.Append('>');
            sb.Append(" prop");
            sb.Append(", ");
            sb.Append(inputBinding.BindsToProperty.Template.JsonType);   // {1}
            sb.Append(" value");
            sb.Append(") => { return (new ");
            sb.Append(inputBinding.InputTypeName);                       // {2}
            sb.Append("() { App = (");
            sb.Append(inputBinding.PropertyAppClass.ClassStemIdentifier);          // {3}
            sb.Append(")pup, Template = (");
            sb.Append(inputBinding.BindsToProperty.Type.ClassStemIdentifier);      // {4}
            sb.Append(")prop");
            sb.Append(", Value = value");
            sb.Append(" }); }, (Json pup, Starcounter.Input");
            sb.Append('<');
            sb.Append(inputBinding.BindsToProperty.Template.JsonType);   // {1}
            sb.Append('>');
            sb.Append(" input) => { ((");
            sb.Append(inputBinding.DeclaringAppClass.ClassStemIdentifier);         // {5}
            sb.Append(")pup");

            for (Int32 i = 0; i < inputBinding.AppParentCount; i++) {
                sb.Append(".Parent");
            }

            sb.Append(").Handle((");
            sb.Append(inputBinding.InputTypeName);                       // {2}
            sb.Append(")input); });");
            
            schemaClass.Prefix.Add(sb.ToString());
        }

        /// <summary>
        /// Processes the class declaration and constructor for a metadata class.
        /// </summary>
        /// <param name="metadataClass">The class declaration syntax node</param>
        private void ProcessMetadataClass(AstMetadataClass metadataClass) {
            var sb = new StringBuilder();

            metadataClass.Prefix.Add("");
            AppendLineDirectiveIfDefined(metadataClass.Prefix, LINE_HIDDEN, 0);
            sb.Append("public class ");
            sb.Append(metadataClass.ClassStemIdentifier);
            sb.Append("<__Tjsonobj__,__jsonobj__>");
            if (metadataClass.InheritedClass != null) {
                sb.Append(" : ");
                sb.Append(metadataClass.InheritedClass.GlobalClassSpecifierWithoutGenerics);
                sb.Append("<__Tjsonobj__,__jsonobj__>");
            }
            sb.Append(" {");

            metadataClass.Prefix.Add(sb.ToString());

            sb.Clear();
            sb.Append("    public ");
            sb.Append(metadataClass.ClassStemIdentifier);

            sb.Append('(');
            sb.Append(((AstJsonClass)metadataClass.NValueClass).GlobalClassSpecifier);
            sb.Append(" obj, ");
            sb.Append(((AstClass)metadataClass).GlobalClassSpecifier);
            sb.Append(" template) : base(obj, template) { }");

            metadataClass.Prefix.Add(sb.ToString());

            metadataClass.Suffix.Add("}");
            AppendLineDirectiveIfDefined(metadataClass.Suffix, LINE_DEFAULT, 0);
        }

        /// <summary>
        /// Generates code for a metadata property.
        /// </summary>
        /// <param name="m">The m.</param>
        private void ProcessMetadataProperty(AstProperty m) {
            var sb = new StringBuilder();

            // Backing field for caching the metadata member.
            sb.Clear();
            sb.Append("private ");
            sb.Append(m.Type.GlobalClassSpecifier);
            sb.Append(" __p_");
            sb.Append(m.MemberName);
            sb.Append(';');
            m.Prefix.Add(sb.ToString());

            // Get accessor for the metadata-member.
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
        }

        /// <summary>
        /// Generates code for aliasing a class.
        /// </summary>
        /// <param name="alias"></param>
        private void ProcessClassAlias(AstClassAlias alias) {
            alias.Prefix.Add("using " + alias.Alias + " = " + alias.Specifier + ";");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="mn"></param>
        /// <param name="tv"></param>
        /// <param name="memberName"></param>
        private void AddDefaultValue(AstSchemaClass a, AstProperty mn, TValue tv, string memberName) {
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
        
        [Conditional("ADDLINEDIRECTIVES")]
        private static void AppendLineDirectiveIfDefined(List<string> list, string str, int indentation) {
            string tmp = "";
            for (int i = 0; i < indentation; i++)
                tmp += ' ';
            tmp += str;
            list.Add(tmp);
        }

        [Conditional("ADDLINEDIRECTIVES")]
        private static void AppendLineDirectiveIfDefined(StringBuilder sb, string str, int indentation) {
            sb.Append(' ', indentation);
            sb.AppendLine(str);
        }
        
        private static string GetLinePosAndFile(Template template) {
            return string.Format(LINE_POSANDFILE, template.CodegenInfo.SourceInfo.Line, template.CodegenInfo.SourceInfo.Filename);
        }
    }
}
