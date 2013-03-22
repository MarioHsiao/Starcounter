
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
        ITypeSignature ulongType;
        ITypeSignature intType;
        Dictionary<TypeDefDeclaration, TypeDefDeclaration> typeToSpec;

        public FieldDefDeclaration TableHandle {
            get;
            private set;
        }

        public FieldDefDeclaration TypeBindingReference {
            get;
            private set;
        }

        public FieldDefDeclaration ThisHandle {
            get;
            private set;
        }

        public FieldDefDeclaration ThisIdentity {
            get;
            private set;
        }

        public FieldDefDeclaration ThisBinding {
            get;
            private set;
        }

        public TypeSpecificationEmit(ModuleDeclaration module) {
            this.module = module;
            typeBindingType = module.Cache.GetType(typeof(TypeBinding));
            ushortType = module.Cache.GetIntrinsic(IntrinsicType.UInt16);
            ulongType = module.Cache.GetIntrinsic(IntrinsicType.UInt64);
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
            this.TableHandle = tableHandle;

            var typeBindingReference = new FieldDefDeclaration {
                Name = TypeSpecification.TypeBindingName,
                Attributes = (FieldAttributes.FamORAssem | FieldAttributes.Static),
                FieldType = typeBindingType
            };
            typeSpec.Fields.Add(typeBindingReference);
            this.TypeBindingReference = typeBindingReference;

            if (ScTransformTask.InheritsObject(typeDef)) {
                var thisHandle = new FieldDefDeclaration {
                    Name = TypeSpecification.ThisHandleName,
                    Attributes = FieldAttributes.Family,
                    FieldType = ulongType
                };
                typeDef.Fields.Add(thisHandle);
                this.ThisHandle = thisHandle;

                var thisId = new FieldDefDeclaration {
                    Name = TypeSpecification.ThisIdName,
                    Attributes = FieldAttributes.Family,
                    FieldType = ulongType
                };
                typeDef.Fields.Add(thisId);
                this.ThisIdentity = thisId;

                var thisBinding = new FieldDefDeclaration {
                    Name = TypeSpecification.ThisBindingName,
                    Attributes = FieldAttributes.Family,
                    FieldType = typeBindingType
                };
                typeDef.Fields.Add(thisBinding);
                this.ThisBinding = thisBinding;
            }
        }

        public FieldDefDeclaration IncludeField(TypeDefDeclaration typeDef, FieldDefDeclaration field) {
            var specType = typeToSpec[typeDef];
            var columnHandle = new FieldDefDeclaration {
                Name = TypeSpecification.FieldNameToColumnHandleName(field.Name),
                Attributes = FieldAttributes.Public | FieldAttributes.Static,
                FieldType = intType
            };
            specType.Fields.Add(columnHandle);
            return columnHandle;
        }
    }
}
