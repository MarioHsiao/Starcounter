using Microsoft.CodeAnalysis;
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
            
            // Materialize ourself as a code behind class
            var ci = codeBehindMetadata = new CodeBehindClassInfo(null);
            var outer = NestingClass;

            ci.ClassName = node.Identifier.ValueText;
            ci.BaseClassName = string.Empty;
            ci.IsDeclaredInCodeBehind = true;
            ci.ParentClasses = new List<string>();

            if (outer != null) {
                ci.Namespace = outer.codeBehindMetadata.Namespace;

                // Foo.Bar.[ThisName] -> list([Bar], [Foo])
                // We keep the somewhat weird reversed format to be
                // compatible with how the Mono parser does it, and
                // what outer code expect.
                var simpleName = node.Identifier.ValueText;
                var tokens = nestedName.Split('+');
                var list = tokens.Reverse().Where((s) => { return s != simpleName; });
                ci.ParentClasses.AddRange(list);
            }
            else {
                var ns = RoslynSyntaxHelpers.GetFullNamespace(node);
                ci.Namespace = ns == string.Empty ? null : ns;
            }

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
            
            var root = Result.RootClassInfo;
            if (root != null && ci.IsRootClass) {
                throw IllegalCodeBehindException("Class {0} is considered a root class; {1}  is too.", ClassDiagnosticName, root.ClassName);
            }

            Result.CodeBehindClasses.Add(ci);
        }

        public override void VisitBaseList(BaseListSyntax node) {
            // It the base list contains only a single element, we don't know if it's
            // a base class or an interface ... We have to assume it's the base class.
            // To implement interfaces on code-behind classes, you'll have to specify
            // first the base class (e.g. "Json"), then the interfaces.

            // Grab and discover the presumed base class
            DiscoverPrimaryBaseType(node);

            // Process secondary types
            for (int i = 1; i < node.Types.Count; i++) {
                
                var baseType = node.Types[i];
                var type = baseType.Type;

                switch (type.Kind()) {
                    case SyntaxKind.IdentifierName:
                        // e.g. "Json"
                        DiscoverSecondaryBaseType(baseType, (IdentifierNameSyntax)type);
                        break;
                    case SyntaxKind.QualifiedName:
                        // e.g "Starcounter.Json"
                        DiscoverSecondaryBaseType(baseType, (QualifiedNameSyntax)type);
                        break;
                    case SyntaxKind.GenericName:
                        // e.g IBound<Foo>
                        DiscoverSecondaryBaseType(baseType, (GenericNameSyntax)type);
                        break;
                }
            }
        }

        public override void VisitConstructorDeclaration(ConstructorDeclarationSyntax node) {
            // By design: Let's not invoke base visitor, since we don't need to analyze 
            // anything else about it, and we provide a faster execution if we don't.

            var isStatic = node.Modifiers.Any((t) => t.Kind() == SyntaxKind.StaticKeyword);
            if (!isStatic) {
                throw IllegalCodeBehindException("Class {0} defines at least one constructor.", ClassDiagnosticName);
            }
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node) {
            // By design: Let's not invoke base visitor, since we don't need to analyze 
            // anything else about it, and we provide a faster execution if we don't.

            if (node.Identifier.Text == "Handle") {
                DiscoverInputHandler(node);
            }
        }

        void DiscoverInputHandler(MethodDeclarationSyntax node) {
            var isStatic = node.Modifiers.Any((t) => t.Kind() == SyntaxKind.StaticKeyword);
            if (isStatic) {
                throw IllegalCodeBehindException(InvalidCodeBehindError.InputHandlerStatic, node);
            }

            if (node.ParameterList == null || node.ParameterList.Parameters.Count != 1) {
                throw IllegalCodeBehindException(InvalidCodeBehindError.InputHandlerBadParameterCount, node);
            }

            if (node.TypeParameterList != null) {
                throw IllegalCodeBehindException(InvalidCodeBehindError.InputHandlerHasTypeParameters, node);
            }

            var parameter = node.ParameterList.Parameters[0];
            if (parameter.Modifiers.Any((t) => t.Kind() == SyntaxKind.RefKeyword)) {
                throw IllegalCodeBehindException(InvalidCodeBehindError.InputHandlerWithRefParameter, node);
            }

            var returns = node.ReturnType as PredefinedTypeSyntax;
            if (returns == null || returns.Keyword != SyntaxFactory.Token(SyntaxKind.VoidKeyword)) {
                throw IllegalCodeBehindException(InvalidCodeBehindError.InputHandlerNotVoidReturnType, node);
            }

            var info = new InputBindingInfo();
            info.DeclaringClassName = codeBehindMetadata.ClassName;
            info.DeclaringClassNamespace = codeBehindMetadata.Namespace;
            info.FullInputTypeName = parameter.Type.ToString();

            codeBehindMetadata.InputBindingList.Add(info);
        }

        void DiscoverPrimaryBaseType(BaseListSyntax node) {
            var baseType = node.Types[0];
            var name = baseType.Type.ToString();

            // We employ a specific and forgiving strategy for classes
            // that define IBound<T> as the first argument of their base
            // type list, allowing implicit inheriting of Json if the
            // first (and possibly only) type is IBound<T>, such as
            // partial class Foo : IBound<Bar> { ... }.
            if (baseType.Type.Kind() == SyntaxKind.GenericName) {
                var genericName = (GenericNameSyntax)baseType.Type;
                if (IsBindingName(genericName)) {
                    codeBehindMetadata.BoundDataClass = genericName.TypeArgumentList.Arguments[0].ToString();
                    name = string.Empty;
                }
            }

            this.codeBehindMetadata.BaseClassName = name;
        }

        void DiscoverSecondaryBaseType(BaseTypeSyntax baseType, IdentifierNameSyntax name) {
        }

        void DiscoverSecondaryBaseType(BaseTypeSyntax baseType, GenericNameSyntax name) {
            if (IsBindingName(name)) {
                codeBehindMetadata.BoundDataClass = name.TypeArgumentList.Arguments[0].ToString();
            }
        }

        void DiscoverSecondaryBaseType(BaseTypeSyntax baseType, QualifiedNameSyntax name) {
        }

        bool IsNamedRootObject() {
            if (this.NestingClass != null) {
                return false;
            }

            return this.Node.Identifier.ValueText == this.CodeBehindAnalyzer.Root.Name;
        }

        bool IsBindingName(GenericNameSyntax name) {
            if (name.TypeArgumentList == null || name.TypeArgumentList.Arguments.Count != 1) {
                return false;
            }

            var generic = name.Identifier.Text;
            return generic.Equals("IBound") || generic.Equals("Starcounter.IBound");
        }

        Exception IllegalCodeBehindException(string message, params object[] args) {
            return new Exception(string.Format(message, args));
        }

        InvalidCodeBehindException IllegalCodeBehindException(InvalidCodeBehindError error, CSharpSyntaxNode node = null) {
            return new InvalidCodeBehindException(error, node);
        }
    }
}
