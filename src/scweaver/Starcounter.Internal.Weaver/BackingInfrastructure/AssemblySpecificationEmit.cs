
using PostSharp.Sdk.CodeModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.Hosting;
using System.Reflection;

namespace Starcounter.Internal.Weaver.BackingInfrastructure {
    /// <summary>
    /// Encapsulates the emission of the assembly specification according
    /// to <see cref="http://www.starcounter.com/internal/wiki/W3#Assembly_specification"/>.
    /// </summary>
    /// <remarks>
    /// Code hosts that want to safely consume the emitted infrastructure should
    /// use the corresponding <see cref="AssemblySpecification"/> type.
    /// </remarks>
    internal sealed class AssemblySpecificationEmit {
        ModuleDeclaration module;
        TypeDefDeclaration assemblyTypeDefinition;
        TypeDefDeclaration databaseClassIndexTypeDefinition;
        static TypeAttributes specificationTypeAttributes;
        static TypeAttributes databaseClassIndexTypeAttributes;
        static AssemblySpecificationEmit() {
            specificationTypeAttributes =
                TypeAttributes.Class |
                TypeAttributes.NotPublic |
                TypeAttributes.Abstract |
                TypeAttributes.BeforeFieldInit |
                TypeAttributes.Sealed;
            databaseClassIndexTypeAttributes =
                TypeAttributes.Class |
                TypeAttributes.NestedAssembly |
                TypeAttributes.Sealed |
                TypeAttributes.Abstract |
                TypeAttributes.BeforeFieldInit;
        }

        public AssemblySpecificationEmit(ModuleDeclaration module) {
            this.module = module;
            EmitSpecification();
        }

        void EmitSpecification() {
            assemblyTypeDefinition = new TypeDefDeclaration {
                Name = AssemblySpecification.Name,
                Attributes = specificationTypeAttributes
            };
            module.Types.Add(assemblyTypeDefinition);

            databaseClassIndexTypeDefinition = new TypeDefDeclaration {
                Name = AssemblySpecification.DatabaseClassIndexName,
                Attributes = databaseClassIndexTypeAttributes
            };
            assemblyTypeDefinition.Types.Add(databaseClassIndexTypeDefinition);
        }

        internal void IncludeDatabaseClass(TypeDefDeclaration databaseClassTypeDef) {
            var name = AssemblySpecification.TypeNameToClassIndexName(databaseClassTypeDef.Name);
            var typeReference = new FieldDefDeclaration {
                Name = name,
                Attributes = (FieldAttributes.Assembly | FieldAttributes.Static),
                FieldType = module.Cache.GetType(typeof(Type))
            };

            databaseClassIndexTypeDefinition.Fields.Add(typeReference);
        }
    }
}
