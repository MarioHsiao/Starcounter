
using PostSharp.Sdk.CodeModel;
using Starcounter.Binding;
using Starcounter.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        AssemblySpecificationEmit assemblySpec;
        TypeDefDeclaration typeDef;
        TypeDefDeclaration emittedSpec;

        public FieldDefDeclaration TableHandle {
            get;
            private set;
        }

        public FieldDefDeclaration TypeBindingReference {
            get;
            private set;
        }

        public IField ThisHandle {
            get;
            private set;
        }

        public IField ThisIdentity {
            get;
            private set;
        }

        public IField ThisIdentityString {
            get;
            private set;
        }

        public IField ThisBinding {
            get;
            private set;
        }

        public TypeSpecificationEmit BaseSpecification {
            get {
                var parentType = typeDef.BaseType;
                var parentTypeDef = parentType as TypeDefDeclaration;
                return assemblySpec.GetSpecification(parentTypeDef);
            }
        }

        public TypeSpecificationEmit(AssemblySpecificationEmit assemblySpecEmit, TypeDefDeclaration typeDef) {
            this.assemblySpec = assemblySpecEmit;
            this.typeDef = typeDef;
            EmitSpecification();
        }

        void EmitSpecification() {
            emittedSpec = new TypeDefDeclaration {
                Name = TypeSpecification.Name,
                Attributes = TypeAttributes.Class | TypeAttributes.NestedFamily
            };
            typeDef.Types.Add(emittedSpec);

            var tableHandle = new FieldDefDeclaration {
                Name = TypeSpecification.TableHandleName,
                Attributes = (FieldAttributes.Public | FieldAttributes.Static),
                FieldType = assemblySpec.UInt16Type
            };
            emittedSpec.Fields.Add(tableHandle);
            this.TableHandle = tableHandle;

            var typeBindingReference = new FieldDefDeclaration {
                Name = TypeSpecification.TypeBindingName,
                Attributes = (FieldAttributes.Public | FieldAttributes.Static),
                FieldType = assemblySpec.TypeBindingType
            };
            emittedSpec.Fields.Add(typeBindingReference);
            this.TypeBindingReference = typeBindingReference;

            if (WeaverUtilities.IsDatabaseRoot(typeDef)) {
                var thisHandle = new FieldDefDeclaration {
                    Name = TypeSpecification.ThisHandleName,
                    Attributes = FieldAttributes.Family,
                    FieldType = assemblySpec.UInt64Type
                };
                typeDef.Fields.Add(thisHandle);
                this.ThisHandle = thisHandle;

                var thisId = new FieldDefDeclaration {
                    Name = TypeSpecification.ThisIdName,
                    Attributes = FieldAttributes.Family,
                    FieldType = assemblySpec.UInt64Type
                };
                typeDef.Fields.Add(thisId);
                this.ThisIdentity = thisId;

                var thisIdString = new FieldDefDeclaration {
                    Name = TypeSpecification.ThisIdStringName,
                    Attributes = FieldAttributes.Family,
                    FieldType = assemblySpec.UInt64Type
                };
                typeDef.Fields.Add(thisIdString);
                this.ThisIdentityString = thisIdString;

                var thisBinding = new FieldDefDeclaration {
                    Name = TypeSpecification.ThisBindingName,
                    Attributes = FieldAttributes.Family,
                    FieldType = assemblySpec.TypeBindingType
                };
                typeDef.Fields.Add(thisBinding);
                this.ThisBinding = thisBinding;

            } else {
                AssignInstanceLevelFields();
            }
        }

        public FieldDefDeclaration IncludeField(FieldDefDeclaration field) {
            var specType = this.emittedSpec;
            var columnHandle = new FieldDefDeclaration {
                Name = TypeSpecification.FieldNameToColumnHandleName(field.Name),
                Attributes = FieldAttributes.Public | FieldAttributes.Static,
                FieldType = assemblySpec.Int32Type
            };
            specType.Fields.Add(columnHandle);
            return columnHandle;
        }

        public IField GetColumnHandle(string typeNameDeclaring, string fieldName) {
            IType synonymTargetType;

            var module = assemblySpec.Module;
            var specificationName = typeNameDeclaring + "+" + TypeSpecification.Name;
            if (specificationName.Equals(this.emittedSpec.Name)) {
                synonymTargetType = this.emittedSpec;
            } else {
                synonymTargetType = (IType)module.FindType(specificationName, BindingOptions.OnlyExisting | BindingOptions.DontThrowException);
                if (synonymTargetType == null) {
                    var typeEnumerator = module.GetDeclarationEnumerator(TokenType.TypeRef);
                    while (typeEnumerator.MoveNext()) {
                        var typeRef = (TypeRefDeclaration)typeEnumerator.Current;
                        if (ScAnalysisTask.GetTypeReflectionName(typeRef).Equals(typeNameDeclaring)) {
                            synonymTargetType = (IType)typeRef.GetTypeDefinition().Module.FindType(specificationName, BindingOptions.OnlyExisting | BindingOptions.DontThrowException);
                            break;
                        }
                    }
                }
            }

            var endpoint = synonymTargetType.Fields.GetByName(TypeSpecification.FieldNameToColumnHandleName(fieldName)).TranslateField(typeDef.Module);
            return endpoint;
        }

        void AssignInstanceLevelFields() {
            this.ThisHandle = typeDef.FindField(TypeSpecification.ThisHandleName).Field.Translate(typeDef.Module);
            this.ThisBinding = typeDef.FindField(TypeSpecification.ThisBindingName).Field.Translate(typeDef.Module);
            this.ThisIdentity = typeDef.FindField(TypeSpecification.ThisIdName).Field.Translate(typeDef.Module);
            this.ThisIdentity = typeDef.FindField(TypeSpecification.ThisIdStringName).Field.Translate(typeDef.Module);
        }


    }
}
