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

namespace Starcounter.Internal.Weaver.IObjectViewImpl {

    /// <summary>
    /// Provides the interface to all <see cref="IObjectView"/> emitters. Each
    /// method in the given interface shall have a corresponding emitter assigned
    /// in this class.
    /// </summary>
    /// <remarks>
    /// To extend the <see cref="IObjectView"/> interface with a new method, do
    /// this:
    /// 1) Add the method to IObjectView.
    /// 2) Implement a an method in this class that emits the code that should be
    /// written by the weaver.
    /// 3) Map your emitting method to the interface method as seen in the constructor
    /// of this class (i.e. make sure it ends up in the targets directory.
    /// </remarks>
    internal sealed class ImplementsIObjectView {
        Dictionary<string, Action<TypeDefDeclaration, MethodInfo, IMethod, MethodDefDeclaration>> targets;
        ModuleDeclaration module;
        Type interfaceNETType;
        ITypeSignature interfaceTypeSignature;
        InstructionWriter writer;
        IMethod notImplementedCtor;
        static MethodAttributes methodAttributes;
        static ImplementsIObjectView() {
            methodAttributes =
                MethodAttributes.Virtual |
                MethodAttributes.Private |
                MethodAttributes.Final |
                MethodAttributes.HideBySig |
                MethodAttributes.NewSlot;
        }

        internal ImplementsIObjectView(ModuleDeclaration module, InstructionWriter writer) {
            this.module = module;
            this.writer = writer;
            interfaceNETType = typeof(IObjectView);
            interfaceTypeSignature = module.FindType(interfaceNETType, BindingOptions.Default);
            targets = new Dictionary<string, Action<TypeDefDeclaration, MethodInfo, IMethod, MethodDefDeclaration>>();
            targets.Add("AssertEquals", AssertEquals);
            targets.Add("GetBoolean", GetBoolean);
            targets.Add("GetUInt32", GetUInt32);
        }

        public void ImplementOn(TypeDefDeclaration typeDef) {
            typeDef.InterfaceImplementations.Add(interfaceTypeSignature);
            
            foreach (var interfaceMethod in interfaceNETType.GetMethods()) {
                IMethod methodRef = module.FindMethod(interfaceMethod, BindingOptions.Default);

                var impl = new MethodDefDeclaration() {
                    Name = interfaceNETType.Name + "." + interfaceMethod.Name,
                    Attributes = methodAttributes,
                    CallingConvention = CallingConvention.HasThis,
                };
                typeDef.Methods.Add(impl);
                impl.InterfaceImplementations.Add(methodRef);

                ImplementInterfaceMethod(interfaceMethod, typeDef, methodRef, impl);
            }
        }

        public void ImplementInterfaceMethod(MethodInfo interfaceMethod, TypeDefDeclaration typeDef, IMethod methodRef, MethodDefDeclaration methodDef) {
            Action<TypeDefDeclaration, MethodInfo, IMethod, MethodDefDeclaration> emitter;
            if (!targets.TryGetValue(interfaceMethod.Name, out emitter)) {
                // Simplest possible emission for now: just add a stub for every interface
                // method and throw a NotImplementedException. The code will still not load,
                // since signatures are not considered, but we can view the result in tools
                // like .NET Reflector and we have the principles of interface emission
                // figured out.
                emitter = SLOPPY_FAKE_EMIT;
            }
            emitter(typeDef, interfaceMethod, methodRef, methodDef);
        }

        void AssertEquals(TypeDefDeclaration typeDef, MethodInfo netMethod, IMethod methodRef, MethodDefDeclaration impl) {
            // Signature: bool AssertEquals(IObjectView)
            impl.ReturnParameter = new ParameterDeclaration {
                Attributes = ParameterAttributes.Retval,
                ParameterType = module.Cache.GetIntrinsic(IntrinsicType.Boolean)
            };
            var otherParameter = new ParameterDeclaration(0, "other", interfaceTypeSignature);
            impl.Parameters.Add(otherParameter);

            using (var w = new AttachedInstructionWriter(writer, impl)) {
                EmitNotImplemented(w);
            }
        }

        void GetBoolean(TypeDefDeclaration typeDef, MethodInfo netMethod, IMethod methodRef, MethodDefDeclaration impl) {
            // Signature: Nullable<Boolean> GetBoolean(Int32 index)
            var returnTypeSignature = new GenericTypeInstanceTypeSignature(
                (INamedType)
                module.FindType(typeof(Nullable<>), BindingOptions.RequireGenericDefinition),
                new ITypeSignature[] { module.Cache.GetIntrinsic(IntrinsicType.Boolean) }
                );

            impl.ReturnParameter = new ParameterDeclaration {
                Attributes = ParameterAttributes.Retval,
                ParameterType = returnTypeSignature
            };
            var indexParameter = new ParameterDeclaration(0, "index", module.Cache.GetIntrinsic(IntrinsicType.Int32));
            impl.Parameters.Add(indexParameter);

            using (var w = new AttachedInstructionWriter(writer, impl)) {
                EmitNotImplemented(w);
            }
        }

        void GetUInt32(TypeDefDeclaration typeDef, MethodInfo netMethod, IMethod methodRef, MethodDefDeclaration impl) {
            // Signature: Nullable<UInt32> GetUInt32(Int32 index)
            var returnTypeSignature = new GenericTypeInstanceTypeSignature(
                (INamedType)
                module.FindType(typeof(Nullable<>), BindingOptions.RequireGenericDefinition),
                new ITypeSignature[] { module.Cache.GetIntrinsic(IntrinsicType.UInt32) }
                );

            impl.ReturnParameter = new ParameterDeclaration {
                Attributes = ParameterAttributes.Retval,
                ParameterType = returnTypeSignature
            };
            var indexParameter = new ParameterDeclaration(0, "index", module.Cache.GetIntrinsic(IntrinsicType.Int32));
            impl.Parameters.Add(indexParameter);

            using (var w = new AttachedInstructionWriter(writer, impl)) {
                EmitNotImplemented(w);
            }
        }

        void SLOPPY_FAKE_EMIT(TypeDefDeclaration typeDef, MethodInfo netMethod, IMethod methodRef, MethodDefDeclaration impl) {
            using (var w = new AttachedInstructionWriter(writer, impl)) {
                EmitNotImplemented(w);
            }
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
