using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Starcounter.XSON.Metadata;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.XSON.PartialClassGenerator {

    internal sealed class ClassAnalyzer : CSharpSyntaxWalker {
        CodeBehindClassInfo codeBehindMetadata;
        public readonly CodeBehindFileAnalyzer CodeBehindAnalyzer;
        public readonly ClassDeclarationSyntax Node;
        public readonly ClassAnalyzer NestingClass;

        CodeBehindMetadata Result {
            get { return CodeBehindAnalyzer.Result; }
        }

        public ClassAnalyzer(CodeBehindFileAnalyzer parent, ClassDeclarationSyntax node, ClassAnalyzer nestingClass = null) {
            CodeBehindAnalyzer = parent;
            Node = node;
            NestingClass = nestingClass;
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

            // Pre-validation we can gather here: if the class is partial?
            // Is that a requirement for all classes in the code-behind?
            // TODO:
            //foreach (var modifier in node.Modifiers) {
            //    modifier.Kind() == SyntaxKind.PartialKeyword
            //}

            // Discover if this class is to be considered a root class.
            // ci.IsRootClass = false; // We are a root class if we got that attribute and/or are named according to the code-behind file

            // Run through the declaration
            base.VisitClassDeclaration(node);

            // Validate, and wrap up (add attribute if not exist), and finally
            // add to the result.
            // Validate 1: we have detected a base class

            // Result.JsonPropertyMapList.Add(ci);
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

        public override void VisitAttribute(AttributeSyntax node) {
            Console.WriteLine("Attribute of {0}: {1} - {2}", this.Node.Identifier.ValueText, node.ToString(), node.Name.ToString());
            // Console.WriteLine("Argument list: {0}", node.ArgumentList.ToString());
            base.VisitAttribute(node);
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node) {
            Console.WriteLine("Found this method: {0}", node.Identifier.Text);
            // base.VisitMethodDeclaration(node);
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
    }
}
