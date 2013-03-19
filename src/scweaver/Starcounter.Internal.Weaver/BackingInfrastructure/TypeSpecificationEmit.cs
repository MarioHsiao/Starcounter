
using PostSharp.Sdk.CodeModel;
using Starcounter.Binding;
using Starcounter.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Internal.Weaver.BackingInfrastructure {
    /// <summary>
    /// Encapsulates the emission of type specifications according
    /// to <see cref="http://www.starcounter.com/internal/wiki/W3#Type_specification"/>.
    /// </summary>
    /// <remarks>
    /// Code hosts that want to safely consume the emitted infrastructure should
    /// use the corresponding <see cref="TypeSpecification"/> type.
    /// </remarks>
    internal sealed class TypeSpecificationEmit {
        ModuleDeclaration module;
        ITypeSignature typeBindingType;
        ITypeSignature ushortType;
        ITypeSignature intType;
        ITypeSignature objectRefType;
        Dictionary<TypeDefDeclaration, TypeDefDeclaration> typeToSpec;

        public TypeSpecificationEmit(ModuleDeclaration module) {
            this.module = module;
            typeBindingType = module.Cache.GetType(typeof(TypeBinding));
            objectRefType = module.Cache.GetType(typeof(ObjectRef));
            ushortType = module.Cache.GetIntrinsic(IntrinsicType.UInt16);
            intType = module.Cache.GetIntrinsic(IntrinsicType.Int32);
            typeToSpec = new Dictionary<TypeDefDeclaration, TypeDefDeclaration>();
        }

        public void EmitForType(TypeDefDeclaration typeDef) {
            var typeSpec = new TypeDefDeclaration {
                Name = TypeSpecification.Name,
                Attributes = TypeAttributes.Class | TypeAttributes.NestedAssembly | TypeAttributes.Sealed
            };
            typeDef.Types.Add(typeSpec);
            typeToSpec.Add(typeDef, typeSpec);

            var tableHandle = new FieldDefDeclaration {
                Name = TypeSpecification.TableHandleName,
                Attributes = (FieldAttributes.FamORAssem | FieldAttributes.Static),
                FieldType = ushortType
            };
            typeSpec.Fields.Add(tableHandle);

            var typeBindingReference = new FieldDefDeclaration {
                Name = TypeSpecification.TypeBindingName,
                Attributes = (FieldAttributes.FamORAssem | FieldAttributes.Static),
                FieldType = typeBindingType
            };
            typeSpec.Fields.Add(typeBindingReference);

            var thisHandle = new FieldDefDeclaration {
                Name = TypeSpecification.ThisHandleName,
                Attributes = FieldAttributes.Private,
                FieldType = objectRefType
            };
            typeDef.Fields.Add(thisHandle);

            var thisBinding = new FieldDefDeclaration {
                Name = TypeSpecification.ThisBindingName,
                Attributes = FieldAttributes.Private,
                FieldType = typeBindingType
            };
            typeDef.Fields.Add(thisBinding);
        }

        public void IncludeField(TypeDefDeclaration typeDef, FieldDefDeclaration field) {
            var specType = typeToSpec[typeDef];
            var columnHandle = new FieldDefDeclaration {
                Name = TypeSpecification.FieldNameToColumnHandleName(field.Name),
                Attributes = FieldAttributes.Public | FieldAttributes.Static,
                FieldType = intType
            };
            specType.Fields.Add(columnHandle);
        }
    }
}
