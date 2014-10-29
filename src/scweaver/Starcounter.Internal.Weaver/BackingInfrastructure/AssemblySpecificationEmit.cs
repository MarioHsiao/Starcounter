
using PostSharp.Sdk.CodeModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.Hosting;
using System.Reflection;
using Starcounter.Binding;

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
        TypeDefDeclaration assemblyTypeDefinition;
        TypeDefDeclaration databaseClassIndexTypeDefinition;
        Dictionary<TypeDefDeclaration, TypeSpecificationEmit> typeToSpec;
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

        public ModuleDeclaration Module;
        public ITypeSignature TypeBindingType { get; private set; }
        public ITypeSignature UInt16Type { get; private set; }
        public ITypeSignature UInt64Type { get; private set; }
        public ITypeSignature Int32Type { get; private set; }

        public AssemblySpecificationEmit(ModuleDeclaration module) {
            this.Module = module;
            TypeBindingType = module.Cache.GetType(typeof(TypeBinding));
            UInt16Type = module.Cache.GetIntrinsic(IntrinsicType.UInt16);
            UInt64Type = module.Cache.GetIntrinsic(IntrinsicType.UInt64);
            Int32Type = module.Cache.GetIntrinsic(IntrinsicType.Int32);
            typeToSpec = new Dictionary<TypeDefDeclaration, TypeSpecificationEmit>();
            EmitSpecification();
        }

        internal TypeSpecificationEmit IncludeDatabaseClass(TypeDefDeclaration databaseClassTypeDef) {
            TypeSpecificationEmit emitter;

            if (typeToSpec.TryGetValue(databaseClassTypeDef, out emitter))
                return emitter;

            var parentType = databaseClassTypeDef.BaseType;
            var parentTypeDef = parentType as TypeDefDeclaration;
            if (parentTypeDef != null) {
                IncludeDatabaseClass(parentTypeDef);
            }

            var name = AssemblySpecification.TypeNameToClassIndexName(databaseClassTypeDef.GetReflectionName());
            var typeReference = new FieldDefDeclaration {
                Name = name,
                Attributes = (FieldAttributes.Public | FieldAttributes.Static),
                FieldType = Module.Cache.GetType(typeof(Type))
            };

            databaseClassIndexTypeDefinition.Fields.Add(typeReference);

            emitter = new TypeSpecificationEmit(this, databaseClassTypeDef);
            typeToSpec.Add(databaseClassTypeDef, emitter);

            return emitter;
        }

        public TypeSpecificationEmit GetSpecification(TypeDefDeclaration type) {
            return typeToSpec[type];
        }

        void EmitSpecification() {
            assemblyTypeDefinition = new TypeDefDeclaration {
                Name = AssemblySpecification.Name,
                Attributes = specificationTypeAttributes
            };
            Module.Types.Add(assemblyTypeDefinition);

            databaseClassIndexTypeDefinition = new TypeDefDeclaration {
                Name = AssemblySpecification.DatabaseClassIndexName,
                Attributes = databaseClassIndexTypeAttributes
            };
            assemblyTypeDefinition.Types.Add(databaseClassIndexTypeDefinition);
        }
    }
}
