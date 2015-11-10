
using System.CodeDom;
using System.Collections.Generic;

namespace GenerateMetadataClasses.CodeGenerator {
    /// <summary>
    /// Class governing the generation of the runtime MetaBinder.
    /// </summary>
    public class BinderGenerator {
        const string MetaBinderClassName = "MetaBinder";
        List<string> names = new List<string>();

        /// <summary>
        /// Initialize a new <see cref="BinderGenerator"/>.
        /// </summary>
        public BinderGenerator() {
            names.Add("Starcounter.Metadata.MetadataEntity");
        }

        /// <summary>
        /// Includes a metadata class.
        /// </summary>
        /// <param name="metadataClass">The class to include</param>
        /// <param name="ns">The namespace of the class.</param>
        public void Include(CodeTypeDeclaration metadataClass, CodeNamespace ns) {
            names.Add(ns.Name + "." + metadataClass.Name);
        }

        /// <summary>
        /// Generate binding related code into the given namespace.
        /// </summary>
        /// <param name="ns">Namespace to generate code into.</param>
        public void Generate(CodeNamespace ns) {
            var impl = GenerateBinderImpl();
            var binder = GenerateMetaBinderPartial(impl);
            ns.Types.AddRange(new[] { binder, impl });
        }

        CodeTypeDeclaration GenerateBinderImpl() {
            var c = new CodeTypeDeclaration();
            c.Name = "RuntimeMetaBinder";
            c.IsClass = true;
            c.BaseTypes.Add(BinderGenerator.MetaBinderClassName);

            // MetaBinder.GetDefinitions override
            var typeDef = "TypeDef";
            var getDefs = new CodeMemberMethod();
            getDefs.Name = "GetDefinitions";
            getDefs.Attributes = MemberAttributes.Public | MemberAttributes.Override;
            getDefs.ReturnType = new CodeTypeReference(typeDef, 1);

            var expressions = new List<CodeExpression>();
            foreach (var name in names) {
                var typeOfExpr = new CodeTypeOfExpression(name);
                var expr = new CodeMethodInvokeExpression(new CodeTypeReferenceExpression(typeDef), "CreateTypeTableDef", typeOfExpr);
                expressions.Add(expr);
            }
            
            var x = new CodeArrayCreateExpression(typeDef, expressions.ToArray());
            var returnExpr = new CodeMethodReturnStatement(x);
            getDefs.Statements.Add(returnExpr);

            // MetaBinder.GetSpecifications
            var typeName = "System.Type";
            var getSpecs = new CodeMemberMethod();
            getSpecs.Name = "GetSpecifications";
            getSpecs.Attributes = MemberAttributes.Public | MemberAttributes.Override;
            getSpecs.ReturnType = new CodeTypeReference(typeName, 1);

            expressions.Clear();
            foreach (var name in names) {
                var typeOfExpr = new CodeTypeOfExpression(name + "." + TypeSpecificationGenerator.ClassName);
                expressions.Add(typeOfExpr);
            }

            x = new CodeArrayCreateExpression(typeName, expressions.ToArray());
            returnExpr = new CodeMethodReturnStatement(x);
            getSpecs.Statements.Add(returnExpr);

            c.Members.AddRange(new[] { getDefs, getSpecs });

            return c;
        }

        CodeTypeDeclaration GenerateMetaBinderPartial(CodeTypeDeclaration generatedBinder) {
            var c = new CodeTypeDeclaration();
            c.Name = BinderGenerator.MetaBinderClassName;
            c.IsClass = true;
            c.IsPartial = true;
            c.Attributes = MemberAttributes.Abstract | MemberAttributes.Public;

            var cctor = new CodeTypeConstructor();
            cctor.Name = c.Name;

            var instanceRef = new CodeFieldReferenceExpression(
                new CodeTypeReferenceExpression(BinderGenerator.MetaBinderClassName), "Instance");
            var nb = new CodeObjectCreateExpression(generatedBinder.Name);

            cctor.Statements.Add(new CodeAssignStatement(instanceRef, nb));

            c.Members.Add(cctor);
            return c;
        }
    }
}
