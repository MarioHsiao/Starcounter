﻿
using GenerateMetadataClasses.Model;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;

namespace GenerateMetadataClasses.CodeGenerator {

    public class ClassGenerator {
        Dictionary<string, CodeTypeDeclaration> done = new Dictionary<string, CodeTypeDeclaration>();

        public readonly CodeNamespace PublicNamespace;
        public readonly CodeNamespace InternalNamespace;

        public ClassGenerator(CodeNamespace publicNamespace, CodeNamespace internalNamespace) {
            PublicNamespace = publicNamespace;
            InternalNamespace = internalNamespace;
        }

        public CodeTypeDeclaration Generate(Table table) {
            if (done.Keys.Contains(table.TableName)) {
                return done[table.TableName];
            }

            var baseClassName = string.IsNullOrEmpty(table.BaseTableName) 
                ? "MetadataEntity"
                : Generate(table.Schema.Tables[table.BaseTableName]).Name;
            
            var c = new CodeTypeDeclaration(table.TableName);
            c.IsClass = true;
            c.IsPartial = true;
            c.BaseTypes.Add(baseClassName);
            
            var tsg = new TypeSpecificationGenerator(table, c);
            
            var propGenerator = new PropertyGenerator();
            foreach (var column in table.Columns) {
                var prop = propGenerator.Generate(column);
                tsg.AddColumn(column);

                c.Members.Add(prop);
            }

            var ts = tsg.Generate();
            c.Members.Add(ts);

            var ctors = GenerateConstructors(c);
            c.Members.AddRange(ctors);
            
            var ns = GetNamespace(table);
            ns.Types.Add(c);

            done.Add(table.TableName, c);
            return c;
        }

        /// <summary>
        /// Return the namespace the generated class will end up in.
        /// </summary>
        /// <param name="table">The table</param>
        /// <returns>The namespace</returns>
        public CodeNamespace GetNamespace(Table table) {
            return table.PublicNamespace? PublicNamespace : InternalNamespace;
        }
        
        CodeConstructor[] GenerateConstructors(CodeTypeDeclaration classDecl) {
            // public [Name](Uninitialized u): base(u) {}
            // internal [Name](): base(__starcounterTypeSpecification.tableHandle) {}

            var uctor = new CodeConstructor();
            uctor.Name = classDecl.Name;
            uctor.Attributes = MemberAttributes.Public;
            uctor.Parameters.Add(new CodeParameterDeclarationExpression("Uninitialized", "u"));
            uctor.BaseConstructorArgs.Add(new CodeVariableReferenceExpression("u"));
            
            var tableRef = new CodeFieldReferenceExpression(
                new CodeTypeReferenceExpression(TypeSpecificationGenerator.ClassName),
                TypeSpecificationGenerator.TableHandle);
            var ictor = new CodeConstructor();
            ictor.Name = classDecl.Name;
            ictor.Attributes = MemberAttributes.Assembly;
            ictor.BaseConstructorArgs.Add(tableRef);

            var xctor = new CodeConstructor();
            xctor.Name = classDecl.Name;
            xctor.Attributes = MemberAttributes.FamilyAndAssembly;
            xctor.Parameters.Add(new CodeParameterDeclarationExpression(typeof(ushort), "table"));
            xctor.BaseConstructorArgs.Add(new CodeVariableReferenceExpression("table"));

            return new[] { uctor, ictor, xctor };
        }
    }
}
