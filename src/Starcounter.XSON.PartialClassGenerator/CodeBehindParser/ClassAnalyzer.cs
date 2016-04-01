using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Starcounter.XSON.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.XSON.PartialClassGenerator {

    /// <summary>
    /// Analayzer of JSON code-behind classes.
    /// </summary>
    internal sealed class ClassAnalyzer : CSharpSyntaxWalker {
        CodeBehindClassInfo codeBehindMetadata;
        public readonly CodeBehindFileAnalyzer CodeBehindAnalyzer;
        public readonly ClassDeclarationSyntax Node;
        public readonly ClassAnalyzer NestingClass;

        CodeBehindMetadata Result {
            get { return CodeBehindAnalyzer.Result; }
        }

        string ClassDiagnosticName {
            get {
                return string.Format("{0} (in {1})", nestedName, CodeBehindAnalyzer.Parser.FileNameNotNull);
            }
        }

        string nestedName;

        public ClassAnalyzer(CodeBehindFileAnalyzer parent, ClassDeclarationSyntax node, ClassAnalyzer nestingClass = null) {
            CodeBehindAnalyzer = parent;
            Node = node;
            NestingClass = nestingClass;
            var simpleName = node.Identifier.ValueText;
            nestedName = nestingClass != null ? nestingClass.nestedName + "+" + simpleName : simpleName;
        }

        public override void VisitClassDeclaration(ClassDeclarationSyntax node) {
            if (node != Node) {
                var nested = new ClassAnalyzer(CodeBehindAnalyzer, node, this);
                nested.Visit(node);
                return;
            }

            // Should we check if there is already a class info registered? I'm
            // thinking we might find partial class declarations in the same file
            // TODO;

            // Materialize ourself as a code behind class
            var ci = codeBehindMetadata = new CodeBehindClassInfo(null);
            ci.IsDeclaredInCodeBehind = true;

            // Check 1: we are named as the root object, or 2, we contain an attribute
            // that specify where we relate. Otherwise, raise an error. Also raise an
            // error if there is more mapping attributes than one.

            var lists = node.AttributeLists;
            AttributeSyntax map = null;
            foreach (var list in lists) {
                foreach (var attrib in list.Attributes) {
                    var mapped = CodeBehindClassInfo.EvaluateAttributeString(attrib.ToString().Trim('[', ']'), ci);
                    if (mapped != null) {
                        if (map != null) {
                            throw IllegalCodeBehindException("Class {0} contain more than one mapping attribute", ClassDiagnosticName);
                        }
                        else {
                            map = attrib;
                        }
                    }
                }
            }

            var rootMapAttributeText = this.CodeBehindAnalyzer.Root.Name + "_json";
            if (map == null) {
                if (!IsNamedRootObject()) {
                    throw IllegalCodeBehindException("Class {0} is neither a named root nor contains any mapping attribute", ClassDiagnosticName);
                }

                // This is the named root, but without any mapping information
                // explicitly given: let's decorate it by faking it
                ci = CodeBehindClassInfo.EvaluateAttributeString(rootMapAttributeText, ci);
            }
            else {
                // It's mapped. If it's also a named root, it can only be
                // mapped as such.
                if (IsNamedRootObject()) {
                    if (ci.RawDebugJsonMapAttribute != rootMapAttributeText) {
                        throw IllegalCodeBehindException(
                            "Class {0} is a named root but maps to [{1}]", ClassDiagnosticName, ci.RawDebugJsonMapAttribute);
                    }
                }
            }

            var isPartial = node.Modifiers.Any((t) => t.Kind() == SyntaxKind.PartialKeyword);
            if (!isPartial) {
                throw IllegalCodeBehindException("Class {0} is not marked partial.", ClassDiagnosticName);
            }

            if (node.TypeParameterList != null) {
                throw IllegalCodeBehindException("Class {0} is a generic class.", ClassDiagnosticName);
            }

            // Run through the declaration
            base.VisitClassDeclaration(node);

            // Validate, and wrap up (add attribute if not exist), and finally
            // add to the result.
            // Validate 1: we have detected a base class
            // Validate 2: we are adding ourselves as a root, and there is already
            // a result in the result
            // TODO:

            var root = Result.RootClassInfo;
            if (root != null && ci.IsRootClass) {
                throw IllegalCodeBehindException("Class {0} is considered a root class; {1}  is too.", ClassDiagnosticName, root.ClassName);
            }

            Result.CodeBehindClasses.Add(ci);
        }

        public override void VisitBaseList(BaseListSyntax node) {
            Console.WriteLine("Baselist of {0}: {1}", this.Node.Identifier.ValueText, node.ToString());

            // Grab and discover the presumed base class
            DiscoverBaseClass(node.Types[0]);

            // Process secondary types
            for (int i = 1; i < node.Types.Count; i++) {
                
                var baseType = node.Types[i];
                var type = baseType.Type;

                Console.WriteLine("Basetype: {0} = {1}, {2}", baseType.ToString(), baseType.Type.Kind(), baseType.Type.GetType());

                switch (type.Kind()) {
                    case SyntaxKind.IdentifierName:
                        // e.g. "Json"
                        DiscoverBaseType(baseType, (IdentifierNameSyntax)type);
                        break;
                    case SyntaxKind.QualifiedName:
                        // e.g "Starcounter.Json"
                        DiscoverBaseType(baseType, (QualifiedNameSyntax)type);
                        break;
                    case SyntaxKind.GenericName:
                        // e.g IBound<Foo>
                        DiscoverBaseType(baseType, (GenericNameSyntax)type);
                        break;
                    default:
                        // NOTE: Lets add a test for `Starcounter.IBound<Foo>`
                        // TODO:

                        // Internal error
                        // TODO;
                        break;
                }
            }
        }

        public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node) {
            var isStatic = node.Modifiers.Any((t) => t.Kind() == SyntaxKind.StaticKeyword);
            if (!isStatic) {
                throw IllegalCodeBehindException("Class {0} defines at least one constructor.", ClassDiagnosticName);
            }
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node) {
            Console.WriteLine("Found this method: {0}", node.Identifier.Text);
            base.VisitMethodDeclaration(node);
        }

        bool IsNamedRootObject() {
            if (this.NestingClass != null) {
                return false;
            }

            return this.Node.Identifier.ValueText == this.CodeBehindAnalyzer.Root.Name;
        }

        void DiscoverBaseClass(BaseTypeSyntax baseType) {
            Console.WriteLine("{0} base class: {1}", this.Node.Identifier.Text, baseType.Type.ToString());
            this.codeBehindMetadata.BaseClassName = baseType.Type.ToString();
        }

        void DiscoverBaseType(BaseTypeSyntax baseType, IdentifierNameSyntax name) {

        }

        void DiscoverBaseType(BaseTypeSyntax baseType, GenericNameSyntax name) {
            if (name.TypeArgumentList == null || name.TypeArgumentList.Arguments.Count != 1) {
                return;
            }

            var generic = name.Identifier.Text;
            if (generic.Equals("IBound") || generic.Equals("Starcounter.IBound")) {
                codeBehindMetadata.BoundDataClass = name.TypeArgumentList.Arguments[0].ToString();
            }
        }

        void DiscoverBaseType(BaseTypeSyntax baseType, QualifiedNameSyntax name) {

        }

        Exception IllegalCodeBehindException(string message, params object[] args) {
            return new Exception(string.Format(message, args));
        }
    }
}
