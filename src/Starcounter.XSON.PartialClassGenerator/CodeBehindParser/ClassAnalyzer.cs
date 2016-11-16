using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Starcounter.XSON.Metadata;
using System.Collections.Generic;
using System.Linq;
using System;

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
                            throw IllegalCodeBehindException(InvalidCodeBehindError.MultipleMappingAttributes, node);
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
                    // Code-behind classes not indicating any mapping are not of
                    // our interest.
                    return;
                }

                // This is the named root, but without any mapping information
                // explicitly given: let's decorate it by faking it
                ci = CodeBehindClassInfo.EvaluateAttributeString(rootMapAttributeText, ci);
            }
            else {
                // It's mapped. If it's also a named root, it can only be
                // mapped as such.
                if (IsNamedRootObject()) {
                    if (ci.JsonMapAttribute != rootMapAttributeText) {
                        throw IllegalCodeBehindException(InvalidCodeBehindError.RootClassWithCustomMapping, node);
                    }
                }
            }

            var isPartial = node.Modifiers.Any((t) => t.Kind() == SyntaxKind.PartialKeyword);
            if (!isPartial) {
                throw IllegalCodeBehindException(InvalidCodeBehindError.ClassNotPartial, node);
            }

            if (node.TypeParameterList != null) {
                throw IllegalCodeBehindException(InvalidCodeBehindError.ClassGeneric, node);
            }

            // Run through the declaration
            base.VisitClassDeclaration(node);

            // Validate, and wrap up (add attribute if not exist), and finally
            // add to the result.
            
            var root = Result.RootClassInfo;
            if (root != null && ci.IsRootClass) {
                throw IllegalCodeBehindException(InvalidCodeBehindError.MultipleRootClasses, node);
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
                throw IllegalCodeBehindException(InvalidCodeBehindError.DefineInstanceConstructor, node);
            }

            base.VisitConstructorDeclaration(node);
        }

        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node) {
            DiscoverTemplatePropertyAssignments(node);
        }

        public override void VisitMethodDeclaration(MethodDeclarationSyntax node) {
            // By design: Let's not invoke base visitor, since we don't need to analyze 
            // anything else about it, and we provide a faster execution if we don't.

            if (node.Identifier.Text == "Handle") {
                DiscoverInputHandler(node);
            }
        }

        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node) {
            // By design: Let's not invoke base visitor, since we don't need to analyze 
            // anything else about it, and we provide a faster execution if we don't.
            
            DiscoverProperty(node);
        }

        public override void VisitFieldDeclaration(FieldDeclarationSyntax node) {
            // By design: Let's not invoke base visitor, since we don't need to analyze 
            // anything else about it, and we provide a faster execution if we don't.

            DiscoverField(node);
        }

        private void DiscoverProperty(PropertyDeclarationSyntax node) {
            var name = node.Identifier.Text;
            var typeName = node.Type.ToString();

            codeBehindMetadata.FieldOrPropertyList.Add(
                new CodeBehindFieldOrPropertyInfo() { Name = name, TypeName = typeName, IsProperty = true }
            );
        }

        private void DiscoverField(FieldDeclarationSyntax node) {
            var declaration = node.Declaration;
            var typeName = declaration.Type.ToString();

            // There can be several identifiers in one declaration (i.e. "private int ett, tva;")
            foreach (VariableDeclaratorSyntax variable in declaration.Variables) {
                var name = variable.Identifier.Text;
                codeBehindMetadata.FieldOrPropertyList.Add(
                    new CodeBehindFieldOrPropertyInfo() { Name = name, TypeName = typeName, IsProperty = false }
                );
            }
        }

        void DiscoverInputHandler(MethodDeclarationSyntax node) {
            var isStatic = node.Modifiers.Any((t) => t.Kind() == SyntaxKind.StaticKeyword);
            if (isStatic) {
                throw IllegalCodeBehindException(InvalidCodeBehindError.InputHandlerStatic, node);
            }

            var isAbstract = node.Modifiers.Any((t) => t.Kind() == SyntaxKind.AbstractKeyword);
            if (isAbstract) {
                throw IllegalCodeBehindException(InvalidCodeBehindError.InputHandlerAbstract, node);
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
            if (returns == null || !returns.Keyword.IsKind(SyntaxKind.VoidKeyword)) {
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
                bool explicitlyBound;
                var genericName = (GenericNameSyntax)baseType.Type;
                if (IsBindingName(genericName, out explicitlyBound)) {
                    codeBehindMetadata.BoundDataClass = genericName.TypeArgumentList.Arguments[0].ToString();
                    codeBehindMetadata.ExplicitlyBound = explicitlyBound;
                    name = string.Empty;
                }
            }

            this.codeBehindMetadata.BaseClassName = name;
        }

        void DiscoverSecondaryBaseType(BaseTypeSyntax baseType, IdentifierNameSyntax name) {
        }

        void DiscoverSecondaryBaseType(BaseTypeSyntax baseType, GenericNameSyntax name) {
            bool explicitlyBound;
            if (IsBindingName(name, out explicitlyBound)) {
                codeBehindMetadata.BoundDataClass = name.TypeArgumentList.Arguments[0].ToString();
                codeBehindMetadata.ExplicitlyBound = explicitlyBound;
            }
        }

        void DiscoverSecondaryBaseType(BaseTypeSyntax baseType, QualifiedNameSyntax name) {
        }

        /// <summary>
        /// Finds and evaluates assignments to templates. Currenty assignments of 'Bind' is supported.
        /// </summary>
        /// <param name="node"></param>
        private void DiscoverTemplatePropertyAssignments(AssignmentExpressionSyntax node) {
            if(node.Kind() != SyntaxKind.SimpleAssignmentExpression)
                return;

            var propertySyntax = node.Left as MemberAccessExpressionSyntax;
            if(propertySyntax == null)
                return;

            var templateSyntax = propertySyntax.Expression as MemberAccessExpressionSyntax;
            if(templateSyntax == null)
                return;

            var propertyName = propertySyntax.Name.Identifier.ValueText;
            if(propertyName == null)
                return;

            if(propertyName.Equals("Bind")) {
                HandleTemplateBindAssignment(node, templateSyntax.ToString());
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="node"></param>
        /// <param name="templatePath"></param>
        private void HandleTemplateBindAssignment(AssignmentExpressionSyntax node, string templatePath) {
            // TODO:
            // Due to lack of time for testing, and to want to keep old stuff as is for the moment we
            // will ignore all assignments of Bind if the ExplicitBound<T> interface is not used.
            // These assignments are currently only needed to get correct compilation-errors for
            // explicitly bound properties.
            if(!codeBehindMetadata.ExplicitlyBound)
                return;

            if(!templatePath.StartsWith("DefaultTemplate."))
                throw IllegalCodeBehindException(InvalidCodeBehindError.TemplateBindInvalidAssignment, node);
            
            string bind;
            SyntaxKind kind = node.Right.Kind();
            if(kind == SyntaxKind.NullLiteralExpression) {
                bind = null;
            } else if(kind == SyntaxKind.StringLiteralExpression) {
                bind = node.Right.ToString();
                if(!string.IsNullOrEmpty(bind) && bind[0] == '"')
                    bind = bind.Substring(1, bind.Length - 2);
            } else {
                // We treat all other assignments as null as well for now to not generate any code for 
                // compile-time checking of bindings. What will happen is that the template will be considered
                // unbound during codegeneration and later when it instantiated the correct binding will be set.

                // What we probably should do (for a more correct approach) is to somehow mark these as custom 
                // bindings already here.
                bind = null;    
            }

            var assignmentInfo = new CodeBehindAssignmentInfo() {
                TemplatePath = templatePath,
                Value = bind
            };
            codeBehindMetadata.BindAssignments.Add(assignmentInfo);
        }
        
        bool IsNamedRootObject() {
            if (this.NestingClass != null) {
                return false;
            }

            return this.Node.Identifier.ValueText == this.CodeBehindAnalyzer.Root.Name;
        }

        bool IsBindingName(GenericNameSyntax name, out bool explicitlyBound) {
            explicitlyBound = false;

            // If ExplicitlyBound is already set to true, it means that we have both IBound<T> 
            // and IExplicitBound<T> declared. IExplicitBound<T> will have  priority.
            if(name.TypeArgumentList == null 
                || name.TypeArgumentList.Arguments.Count != 1
                || this.codeBehindMetadata.ExplicitlyBound == true) {
                return false;
            }

            var generic = name.Identifier.Text;
            if(generic.Equals("IExplicitBound") || generic.Equals("Starcounter.IExplicitBound")) {
                explicitlyBound = true;
                return true;
            }
            
            return generic.Equals("IBound") || generic.Equals("Starcounter.IBound");
        }

        InvalidCodeBehindException IllegalCodeBehindException(InvalidCodeBehindError error, CSharpSyntaxNode node = null) {
            return new InvalidCodeBehindException(error, node);
        }
    }
}
