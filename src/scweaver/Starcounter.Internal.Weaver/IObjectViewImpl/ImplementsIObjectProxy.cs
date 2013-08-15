
// Allows us to force the implementation of every method
// to be implemented as throwing a NotImplementedException.
// This is particulary useful when we want to compare the
// generated signatures- and metadata to that of a class
// being default-implemented by Visual Studio.
// #define THROW_NOT_IMPLEMENTED

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter;
using PostSharp.Sdk.CodeModel;
using System.Reflection;
using System.Diagnostics;
using PostSharp.Sdk.Collections;
using Starcounter.Internal.Weaver;
using PostSharp.Sdk.CodeModel.TypeSignatures;
using Starcounter.Binding;
using Starcounter.Hosting;
using PostSharp.Extensibility;
using Starcounter.Advanced;

namespace Starcounter.Internal.Weaver.IObjectViewImpl {

    /// <summary>
    /// Provides the interface to all <see cref="IObjectProxy"/> emitters. Each
    /// method in the given interface shall have a corresponding emitter assigned
    /// in this class.
    /// </summary>
    /// <remarks>
    /// To extend the <see cref="IObjectProxy"/>/<see cref="IObjectView"/> interfaces
    /// with a new method, do this:
    /// 1) Add the method to the interface
    /// 2) Implement a method in this class that emits the code that should be
    /// written by the weaver.
    /// 3) Map your emitting method to the interface method as seen in the constructor
    /// of this class (i.e. make sure it ends up in the "targets" directory.
    /// </remarks>
    internal sealed class ImplementsIObjectProxy {
        Dictionary<string, Action<TypeDefDeclaration, MethodInfo, IMethod, MethodDefDeclaration>> targets;
        ModuleDeclaration module;
        Type viewNETType;
        ITypeSignature viewTypeSignature;
        Type proxyNETType;
        ITypeSignature proxyTypeSignature;
        Type bindableNETType;
        ITypeSignature bindableTypeSignature;
        InstructionWriter writer;
        IMethod notImplementedCtor;
        static MethodAttributes methodAttributes;
        FieldDefDeclaration thisHandleField;
        FieldDefDeclaration thisIdentityField;
        FieldDefDeclaration thisBindingField;
        DbStateMethodProvider.ViewAccessors viewAccessLayer;
        static FieldInfo retreiverInstanceFieldInfo;
        IField retreiverInstanceField;

        static ImplementsIObjectProxy() {
            methodAttributes =
                MethodAttributes.Virtual |
                MethodAttributes.Private |
                MethodAttributes.Final |
                MethodAttributes.HideBySig |
                MethodAttributes.NewSlot;
            retreiverInstanceFieldInfo = typeof(DatabaseObjectRetriever).GetField(
                "Instance", BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.Static);
        }

        internal ImplementsIObjectProxy(ModuleDeclaration module, InstructionWriter writer, DbStateMethodProvider.ViewAccessors viewGetMethods) {
            this.module = module;
            this.writer = writer;
            viewNETType = typeof(IObjectView);
            viewTypeSignature = module.FindType(viewNETType, BindingOptions.Default);
            proxyNETType = typeof(IObjectProxy);
            proxyTypeSignature = module.FindType(proxyNETType, BindingOptions.Default);
            bindableNETType = typeof(IBindable);
            bindableTypeSignature = module.FindType(bindableNETType, BindingOptions.Default);
            viewAccessLayer = viewGetMethods;
            retreiverInstanceField = module.FindField(retreiverInstanceFieldInfo, BindingOptions.DontThrowException);

            targets = new Dictionary<string, Action<TypeDefDeclaration, MethodInfo, IMethod, MethodDefDeclaration>>();
            targets.Add("Bind", Bind);
            targets.Add("get_ThisHandle", GetThisHandle);
            targets.Add("get_Identity", GetThisIdentity);
            targets.Add("get_Retriever", GetBindableRetriever);
            targets.Add("GetBoolean", GetBoolean);
            targets.Add("GetByte", GetByte);
            targets.Add("GetBinary", GetBinary);
            targets.Add("GetDateTime", GetDateTime);
            targets.Add("GetDecimal", GetDecimal);
            targets.Add("GetDouble", GetDouble);
            targets.Add("GetInt16", GetInt16);
            targets.Add("GetUInt16", GetUInt16);
            targets.Add("GetInt32", GetInt32);
            targets.Add("GetUInt32", GetUInt32);
            targets.Add("GetInt64", GetInt64);
            targets.Add("GetUInt64", GetUInt64);
            targets.Add("GetObject", GetObject);
            targets.Add("GetSByte", GetSByte);
            targets.Add("GetSingle", GetSingle);
            targets.Add("GetString", GetString);
            targets.Add("get_TypeBinding", GetTypeBinding);
            targets.Add("AssertEquals", AssertEquals);
            targets.Add("EqualsOrIsDerivedFrom", EqualsOrIsDerivedFrom);
        }

        public bool ShouldImplementOn(TypeDefDeclaration typeDef) {
            return WeaverUtilities.IsDatabaseRoot(typeDef);
        }

        public void ImplementOn(TypeDefDeclaration typeDef) {
            thisHandleField = typeDef.Fields.GetByName(TypeSpecification.ThisHandleName);
            thisIdentityField = typeDef.Fields.GetByName(TypeSpecification.ThisIdName);
            thisBindingField = typeDef.Fields.GetByName(TypeSpecification.ThisBindingName);

            typeDef.InterfaceImplementations.Add(bindableTypeSignature);
            typeDef.InterfaceImplementations.Add(viewTypeSignature);
            typeDef.InterfaceImplementations.Add(proxyTypeSignature);

            foreach (var interfaceProperty in bindableNETType.GetProperties()) {
                var p = new PropertyDeclaration() {
                    PropertyType = module.FindType(interfaceProperty.PropertyType),
                    Name = bindableNETType.FullName + "." + interfaceProperty.Name,
                    CallingConvention = CallingConvention.HasThis
                };

                typeDef.Properties.Add(p);
                if (interfaceProperty.CanRead) {
                    var getSemantic = new MethodSemanticDeclaration() {
                        Semantic = MethodSemantics.Getter
                    };
                    p.Members.Add(getSemantic);
                }
                if (interfaceProperty.CanWrite) {
                    var setSemantic = new MethodSemanticDeclaration() {
                        Semantic = MethodSemantics.Setter
                    };
                    p.Members.Add(setSemantic);
                }
            }

            foreach (var interfaceMethod in bindableNETType.GetMethods()) {
                IMethod methodRef = module.FindMethod(interfaceMethod, BindingOptions.Default);

                var impl = new MethodDefDeclaration() {
                    Name = bindableNETType.FullName + "." + interfaceMethod.Name,
                    Attributes = methodAttributes,
                    CallingConvention = CallingConvention.HasThis,
                };
                typeDef.Methods.Add(impl);
                impl.InterfaceImplementations.Add(methodRef);

                ImplementInterfaceMethod(interfaceMethod, typeDef, methodRef, impl);
                #region #ifdef THROW_NOT_IMPLEMENTED
#if THROW_NOT_IMPLEMENTED
                impl.MethodBody = new MethodBodyDeclaration();
                using (var attached = new AttachedInstructionWriter(writer, impl)) {
                    EmitNotImplemented(attached);
                }
#endif
                #endregion
            }

            foreach (var interfaceProperty in viewNETType.GetProperties()) {
                var p = new PropertyDeclaration() {
                    PropertyType = module.FindType(interfaceProperty.PropertyType),
                    Name = viewNETType.FullName + "." + interfaceProperty.Name,
                    CallingConvention = CallingConvention.HasThis
                };
                
                typeDef.Properties.Add(p);
                if (interfaceProperty.CanRead) {
                    var getSemantic = new MethodSemanticDeclaration() {
                        Semantic = MethodSemantics.Getter
                    };
                    p.Members.Add(getSemantic);
                }
                if (interfaceProperty.CanWrite) {
                    var setSemantic = new MethodSemanticDeclaration() {
                        Semantic = MethodSemantics.Setter
                    };
                    p.Members.Add(setSemantic);
                }
            }

            foreach (var interfaceMethod in viewNETType.GetMethods()) {
                IMethod methodRef = module.FindMethod(interfaceMethod, BindingOptions.Default);

                var impl = new MethodDefDeclaration() {
                    Name = viewNETType.FullName + "." + interfaceMethod.Name,
                    Attributes = methodAttributes,
                    CallingConvention = CallingConvention.HasThis,
                };
                typeDef.Methods.Add(impl);
                impl.InterfaceImplementations.Add(methodRef);

                ImplementInterfaceMethod(interfaceMethod, typeDef, methodRef, impl);
                #region #ifdef THROW_NOT_IMPLEMENTED
#if THROW_NOT_IMPLEMENTED
                impl.MethodBody = new MethodBodyDeclaration();
                using (var attached = new AttachedInstructionWriter(writer, impl)) {
                    EmitNotImplemented(attached);
                }
#endif
                #endregion
            }

            foreach (var interfaceProperty in proxyNETType.GetProperties()) {
                var p = new PropertyDeclaration() {
                    PropertyType = module.FindType(interfaceProperty.PropertyType),
                    Name = proxyNETType.FullName + "." + interfaceProperty.Name,
                    CallingConvention = CallingConvention.HasThis
                };
                typeDef.Properties.Add(p);
                if (interfaceProperty.CanRead) {
                    var getSemantic = new MethodSemanticDeclaration() {
                        Semantic = MethodSemantics.Getter
                    };
                    p.Members.Add(getSemantic);
                }
                if (interfaceProperty.CanWrite) {
                    var setSemantic = new MethodSemanticDeclaration() {
                        Semantic = MethodSemantics.Setter
                    };
                    p.Members.Add(setSemantic);
                }
            }

            foreach (var interfaceMethod in proxyNETType.GetMethods()) {
                IMethod methodRef = module.FindMethod(interfaceMethod, BindingOptions.Default);

                var impl = new MethodDefDeclaration() {
                    Name = proxyNETType.FullName + "." + interfaceMethod.Name,
                    Attributes = methodAttributes,
                    CallingConvention = CallingConvention.HasThis,
                };
                typeDef.Methods.Add(impl);
                impl.InterfaceImplementations.Add(methodRef);

                ImplementInterfaceMethod(interfaceMethod, typeDef, methodRef, impl);
                #region #ifdef THROW_NOT_IMPLEMENTED
#if THROW_NOT_IMPLEMENTED
                impl.MethodBody = new MethodBodyDeclaration();
                using (var attached = new AttachedInstructionWriter(writer, impl)) {
                    EmitNotImplemented(attached);
                }
#endif
                #endregion
            }
        }

        void ImplementInterfaceMethod(MethodInfo interfaceMethod, TypeDefDeclaration typeDef, IMethod methodRef, MethodDefDeclaration methodDef) {
            Action<TypeDefDeclaration, MethodInfo, IMethod, MethodDefDeclaration> emitter;

            emitter = targets[interfaceMethod.Name];
            Trace.Assert(emitter != null, "Missing interface emitter method for " + interfaceMethod.Name);
            
            emitter(typeDef, interfaceMethod, methodRef, methodDef);
        }

        void Bind(TypeDefDeclaration typeDef, MethodInfo netMethod, IMethod methodRef, MethodDefDeclaration impl) {
            // Signature: void IObjectProxy.Bind(ulong addr, ulong oid, TypeBinding typeBinding)
            var ulongSignature = module.Cache.GetIntrinsic(IntrinsicType.UInt64);
            var parameter = new ParameterDeclaration(0, "address", ulongSignature);
            impl.Parameters.Add(parameter);
            parameter = new ParameterDeclaration(1, "oid", ulongSignature);
            impl.Parameters.Add(parameter);
            parameter = new ParameterDeclaration(2, "typeBinding", module.FindType(typeof(TypeBinding), BindingOptions.Default));
            impl.Parameters.Add(parameter);

            using (var attached = new AttachedInstructionWriter(writer, impl)) {
                var w = attached.Writer;

                impl.MethodBody.MaxStack = 8;
                w.EmitInstruction(OpCodeNumber.Ldarg_0);
                w.EmitInstruction(OpCodeNumber.Ldarg_1);
                w.EmitInstructionField(OpCodeNumber.Stfld, thisHandleField);
                w.EmitInstruction(OpCodeNumber.Ldarg_0);
                w.EmitInstruction(OpCodeNumber.Ldarg_2);
                w.EmitInstructionField(OpCodeNumber.Stfld, thisIdentityField);
                w.EmitInstruction(OpCodeNumber.Ldarg_0);
                w.EmitInstruction(OpCodeNumber.Ldarg_3);
                w.EmitInstructionField(OpCodeNumber.Stfld, thisBindingField);
                w.EmitInstruction(OpCodeNumber.Ret);
            }
        }

        void GetThisHandle(TypeDefDeclaration typeDef, MethodInfo netMethod, IMethod methodRef, MethodDefDeclaration impl) {
            // ulong IObjectProxy.get_ThisHandle()
            impl.Attributes |= MethodAttributes.SpecialName;
            impl.ReturnParameter = new ParameterDeclaration {
                Attributes = ParameterAttributes.Retval,
                ParameterType = module.Cache.GetIntrinsic(IntrinsicType.UInt64)
            };
            var propertyName = proxyNETType.FullName + "." + "ThisHandle";
            var getter = typeDef.Properties.GetOneByName(propertyName).Members.GetBySemantic(MethodSemantics.Getter);
            getter.Method = impl;

            using (var attached = new AttachedInstructionWriter(writer, impl)) {
                var w = attached.Writer;
                impl.MethodBody.MaxStack = 8;
                w.EmitInstruction(OpCodeNumber.Ldarg_0);
                w.EmitInstructionField(OpCodeNumber.Ldfld, thisHandleField);
                w.EmitInstruction(OpCodeNumber.Ret);
            }
        }

        void GetThisIdentity(TypeDefDeclaration typeDef, MethodInfo netMethod, IMethod methodRef, MethodDefDeclaration impl) {
            // Signature: ulong IBindable.get_Identity()
            impl.Attributes |= MethodAttributes.SpecialName;
            impl.ReturnParameter = new ParameterDeclaration {
                Attributes = ParameterAttributes.Retval,
                ParameterType = module.Cache.GetIntrinsic(IntrinsicType.UInt64)
            };
            var propertyName = bindableNETType.FullName + "." + "Identity";
            var getter = typeDef.Properties.GetOneByName(propertyName).Members.GetBySemantic(MethodSemantics.Getter);
            getter.Method = impl;

            using (var attached = new AttachedInstructionWriter(writer, impl)) {
                var w = attached.Writer;
                impl.MethodBody.MaxStack = 8;
                w.EmitInstruction(OpCodeNumber.Ldarg_0);
                w.EmitInstructionField(OpCodeNumber.Ldfld, thisIdentityField);
                w.EmitInstruction(OpCodeNumber.Ret);
            }
        }

        void GetBindableRetriever(TypeDefDeclaration typeDef, MethodInfo netMethod, IMethod methodRef, MethodDefDeclaration impl) {
            // Signature: IBindableRetriever IBindable.get_Retriever()
            impl.Attributes |= MethodAttributes.SpecialName;
            var returnType = module.FindType(typeof(IBindableRetriever), BindingOptions.Default);
            impl.ReturnParameter = new ParameterDeclaration {
                Attributes = ParameterAttributes.Retval,
                ParameterType = returnType
            };

            var propertyName = bindableNETType.FullName + "." + "Retriever";
            var getter = typeDef.Properties.GetOneByName(propertyName).Members.GetBySemantic(MethodSemantics.Getter);
            getter.Method = impl;

            using (var attached = new AttachedInstructionWriter(writer, impl)) {
                var w = attached.Writer;
                impl.MethodBody.MaxStack = 8;
                w.EmitInstructionField(OpCodeNumber.Ldsfld, retreiverInstanceField);
                w.EmitInstruction(OpCodeNumber.Ret);
            }
        }

        void GetBoolean(TypeDefDeclaration typeDef, MethodInfo netMethod, IMethod methodRef, MethodDefDeclaration impl) {
            BuildIObjectViewIntristicGetterSignature(IntrinsicType.Boolean, impl);

            using (var attached = new AttachedInstructionWriter(writer, impl)) {
                EmitGetterBody(attached, impl, viewAccessLayer.GetBoolean);
            }
        }

        void GetByte(TypeDefDeclaration typeDef, MethodInfo netMethod, IMethod methodRef, MethodDefDeclaration impl) {
            BuildIObjectViewIntristicGetterSignature(IntrinsicType.Byte, impl);

            using (var attached = new AttachedInstructionWriter(writer, impl)) {
                EmitGetterBody(attached, impl, viewAccessLayer.GetByte);
            }
        }

        void GetBinary(TypeDefDeclaration typeDef, MethodInfo netMethod, IMethod methodRef, MethodDefDeclaration impl) {
            BuildIObjectViewNonIntristicGetterSignature(typeof(Binary), true, impl);
            using (var w = new AttachedInstructionWriter(writer, impl)) {
                EmitGetterBody(w, impl, viewAccessLayer.GetBinary);
            }

        }
        
        void GetDateTime(TypeDefDeclaration typeDef, MethodInfo netMethod, IMethod methodRef, MethodDefDeclaration impl) {
            BuildIObjectViewNonIntristicGetterSignature(typeof(DateTime), true, impl);
            using (var w = new AttachedInstructionWriter(writer, impl)) {
                EmitGetterBody(w, impl, viewAccessLayer.GetDateTime);
            }

        }
        
        void GetDecimal(TypeDefDeclaration typeDef, MethodInfo netMethod, IMethod methodRef, MethodDefDeclaration impl) {
            BuildIObjectViewNonIntristicGetterSignature(typeof(decimal), true, impl);
            using (var w = new AttachedInstructionWriter(writer, impl)) {
                EmitGetterBody(w, impl, viewAccessLayer.GetDecimal);
            }
        }
        
        void GetDouble(TypeDefDeclaration typeDef, MethodInfo netMethod, IMethod methodRef, MethodDefDeclaration impl) {
            BuildIObjectViewIntristicGetterSignature(IntrinsicType.Double, impl);
            using (var w = new AttachedInstructionWriter(writer, impl)) {
                EmitGetterBody(w, impl, viewAccessLayer.GetDouble);
            }

        }
        
        void GetInt16(TypeDefDeclaration typeDef, MethodInfo netMethod, IMethod methodRef, MethodDefDeclaration impl) {
            BuildIObjectViewIntristicGetterSignature(IntrinsicType.Int16, impl);
            using (var w = new AttachedInstructionWriter(writer, impl)) {
                EmitGetterBody(w, impl, viewAccessLayer.GetInt16);
            }

        }
        
        void GetInt32(TypeDefDeclaration typeDef, MethodInfo netMethod, IMethod methodRef, MethodDefDeclaration impl) {
            BuildIObjectViewIntristicGetterSignature(IntrinsicType.Int32, impl);
            using (var w = new AttachedInstructionWriter(writer, impl)) {
                EmitGetterBody(w, impl, viewAccessLayer.GetInt32);
            }

        }
        
        void GetInt64(TypeDefDeclaration typeDef, MethodInfo netMethod, IMethod methodRef, MethodDefDeclaration impl) {
            BuildIObjectViewIntristicGetterSignature(IntrinsicType.Int64, impl);
            using (var w = new AttachedInstructionWriter(writer, impl)) {
                EmitGetterBody(w, impl, viewAccessLayer.GetInt64);
            }

        }
        
        void GetObject(TypeDefDeclaration typeDef, MethodInfo netMethod, IMethod methodRef, MethodDefDeclaration impl) {
            BuildIObjectViewNonIntristicGetterSignature(typeof(IObjectView), false, impl);
            using (var w = new AttachedInstructionWriter(writer, impl)) {
                EmitGetterBody(w, impl, viewAccessLayer.GetObject);
            }
        }
        
        void GetSByte(TypeDefDeclaration typeDef, MethodInfo netMethod, IMethod methodRef, MethodDefDeclaration impl) {
            BuildIObjectViewIntristicGetterSignature(IntrinsicType.SByte, impl);
            using (var w = new AttachedInstructionWriter(writer, impl)) {
                EmitGetterBody(w, impl, viewAccessLayer.GetSByte);
            }
        }
        
        void GetSingle(TypeDefDeclaration typeDef, MethodInfo netMethod, IMethod methodRef, MethodDefDeclaration impl) {
            BuildIObjectViewIntristicGetterSignature(IntrinsicType.Single, impl);
            using (var w = new AttachedInstructionWriter(writer, impl)) {
                EmitGetterBody(w, impl, viewAccessLayer.GetSingle);
            }
        }
        
        void GetString(TypeDefDeclaration typeDef, MethodInfo netMethod, IMethod methodRef, MethodDefDeclaration impl) {
            BuildIObjectViewNonIntristicGetterSignature(typeof(string), false, impl);
            using (var w = new AttachedInstructionWriter(writer, impl)) {
                EmitGetterBody(w, impl, viewAccessLayer.GetString);
            }
        }
        
        void GetUInt16(TypeDefDeclaration typeDef, MethodInfo netMethod, IMethod methodRef, MethodDefDeclaration impl) {
            BuildIObjectViewIntristicGetterSignature(IntrinsicType.UInt16, impl);
            using (var w = new AttachedInstructionWriter(writer, impl)) {
                EmitGetterBody(w, impl, viewAccessLayer.GetUInt16);
            }
        }
        void GetUInt32(TypeDefDeclaration typeDef, MethodInfo netMethod, IMethod methodRef, MethodDefDeclaration impl) {
            BuildIObjectViewIntristicGetterSignature(IntrinsicType.UInt32, impl);
            using (var w = new AttachedInstructionWriter(writer, impl)) {
                EmitGetterBody(w, impl, viewAccessLayer.GetUInt32);
            }
        }

        void GetUInt64(TypeDefDeclaration typeDef, MethodInfo netMethod, IMethod methodRef, MethodDefDeclaration impl) {
            BuildIObjectViewIntristicGetterSignature(IntrinsicType.UInt64, impl);
            using (var w = new AttachedInstructionWriter(writer, impl)) {
                EmitGetterBody(w, impl, viewAccessLayer.GetUInt64);
            }
        }
        
        void GetTypeBinding(TypeDefDeclaration typeDef, MethodInfo netMethod, IMethod methodRef, MethodDefDeclaration impl) {
            //public ITypeBinding TypeBinding {get;} = ITypeBindinng get_TypeBinding()
            impl.Attributes |= MethodAttributes.SpecialName;
            impl.ReturnParameter = new ParameterDeclaration {
                Attributes = ParameterAttributes.Retval,
                ParameterType = module.FindType(typeof(ITypeBinding))
            };
            var propertyName = viewNETType.FullName + "." + "TypeBinding";
            var getter = typeDef.Properties.GetOneByName(propertyName).Members.GetBySemantic(MethodSemantics.Getter);
            getter.Method = impl;

            using (var attached = new AttachedInstructionWriter(writer, impl)) {
                var w = attached.Writer;
                impl.MethodBody.MaxStack = 8;
                w.EmitInstruction(OpCodeNumber.Ldarg_0);
                w.EmitInstructionField(OpCodeNumber.Ldfld, thisBindingField);
                w.EmitInstruction(OpCodeNumber.Ret);
            }
        }

        //public bool EqualsOrIsDerivedFrom(IObjectView obj)
        void EqualsOrIsDerivedFrom(TypeDefDeclaration typeDef, MethodInfo netMethod, IMethod methodRef, MethodDefDeclaration impl) {
            var retSign = module.Cache.GetIntrinsic(IntrinsicType.Boolean);
            impl.ReturnParameter = new ParameterDeclaration {
                Attributes = ParameterAttributes.Retval,
                ParameterType = retSign
            };

            var objParameter = new ParameterDeclaration(0, "obj", module.FindType(typeof(IObjectView)));
            impl.Parameters.Add(objParameter);

            using (var w = new AttachedInstructionWriter(writer, impl)) {
                EmitNotImplemented(w);
            }
        }

        void AssertEquals(TypeDefDeclaration typeDef, MethodInfo netMethod, IMethod methodRef, MethodDefDeclaration impl) {
            // Signature: bool AssertEquals(IObjectView)
            impl.ReturnParameter = new ParameterDeclaration {
                Attributes = ParameterAttributes.Retval,
                ParameterType = module.Cache.GetIntrinsic(IntrinsicType.Boolean)
            };
            var otherParameter = new ParameterDeclaration(0, "other", viewTypeSignature);
            impl.Parameters.Add(otherParameter);

            using (var w = new AttachedInstructionWriter(writer, impl)) {
                EmitNotImplemented(w);
            }
        }

        void BuildIObjectViewIntristicGetterSignature(IntrinsicType intristic, MethodDefDeclaration impl) {
            // Every intristic IObjectView getter return Nullable<type>,
            // e.g. int? IObjectView.GetInt32(Int32 index);
            // Thats the kind of signature we produce here.

            var retSign = new GenericTypeInstanceTypeSignature(
                (INamedType)
                module.FindType(typeof(Nullable<>), BindingOptions.RequireGenericDefinition),
                new ITypeSignature[] { module.Cache.GetIntrinsic(intristic) }
                );
            impl.ReturnParameter = new ParameterDeclaration {
                Attributes = ParameterAttributes.Retval,
                ParameterType = retSign
            };

            var indexParameter = new ParameterDeclaration(0, "index", module.Cache.GetIntrinsic(IntrinsicType.Int32));
            impl.Parameters.Add(indexParameter);
        }

        void BuildIObjectViewNonIntristicGetterSignature(Type t, bool nullable, MethodDefDeclaration impl) {
            ITypeSignature retSign;
            ITypeSignature retDataType;

            // Every non-intristic IObjectView getter return Nullable<type>
            // or <type>, e.g. DateTime? IObjectView.GetDateTime(Int32 index),
            // or IObjectView IObjectView.GetObject(Int32 index)
            // Thats the kind of signature we produce here.
            
            retSign = retDataType = module.FindType(t, BindingOptions.Default);
            if (nullable) {
                retSign = new GenericTypeInstanceTypeSignature(
                    (INamedType)
                    module.FindType(typeof(Nullable<>), BindingOptions.RequireGenericDefinition),
                    new ITypeSignature[] { retDataType }
                );
            }

            impl.ReturnParameter = new ParameterDeclaration {
                Attributes = ParameterAttributes.Retval,
                ParameterType = retSign
            };

            var indexParameter = new ParameterDeclaration(0, "index", module.Cache.GetIntrinsic(IntrinsicType.Int32));
            impl.Parameters.Add(indexParameter);
        }

        // Emits a method body such as
        // return DbState.View.Read[datatype](this.__sc__this_binding__, index, this);
        void EmitGetterBody(AttachedInstructionWriter attachedWriter, MethodDefDeclaration impl, IMethod getterAPI) {
            var w = attachedWriter.Writer;
            impl.MethodBody.MaxStack = 8;
            w.EmitInstruction(OpCodeNumber.Ldarg_0);
            w.EmitInstructionField(OpCodeNumber.Ldfld, thisBindingField);
            w.EmitInstruction(OpCodeNumber.Ldarg_1);
            w.EmitInstruction(OpCodeNumber.Ldarg_0);
            w.EmitInstructionMethod(OpCodeNumber.Call, getterAPI);
            w.EmitInstruction(OpCodeNumber.Ret);
        }

        void EmitNotImplemented(AttachedInstructionWriter attachedWriter) {
            if (notImplementedCtor == null) {
                notImplementedCtor = module.FindMethod(
                    typeof(NotImplementedException).GetConstructor(Type.EmptyTypes),
                    BindingOptions.Default
                );
            }
            attachedWriter.Writer.EmitInstruction(OpCodeNumber.Nop);
            attachedWriter.Writer.EmitInstructionMethod(OpCodeNumber.Newobj, notImplementedCtor);
            attachedWriter.Writer.EmitInstruction(OpCodeNumber.Throw);
        }
    }
}