
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.Collections;
using Starcounter.Advanced;
using Starcounter.Hosting;
using System;
using System.Reflection;

namespace Starcounter.Internal.Weaver.EqualityImpl {
    
    /// <summary>
    /// Governs the implementation of database class equality and
    /// if it should be done on a particular type or not.
    /// </summary>
    internal sealed class ImplementsEquality {
        ModuleDeclaration module;
        InstructionWriter writer;
        private ITypeSignature bindableType;
        private IMethod bindableGetIdentityMethod;
        private IMethod objectReferenceEqualsMethod;
        private IMethod ulongGetHashCode;

        public ImplementsEquality(ModuleDeclaration module, InstructionWriter writer) {
            this.module = module;
            this.writer = writer;
            bindableType = module.FindType(typeof(IBindable), BindingOptions.Default);
            bindableGetIdentityMethod = module.FindMethod(typeof(IBindable).GetMethod("get_Identity"), BindingOptions.Default);
            objectReferenceEqualsMethod = module.FindMethod(
                typeof(object).GetMethod("ReferenceEquals", BindingFlags.Public | BindingFlags.Static),
                BindingOptions.Default);
            ulongGetHashCode = module.FindMethod(typeof(ulong).GetMethod("GetHashCode"), BindingOptions.Default);
        }

        public bool ShouldImplementOn(TypeDefDeclaration typeDef) {
            var should = false;
            if (WeaverUtilities.IsDatabaseRoot(typeDef)) {
                var bindOps = BindingOptions.OnlyDefinition | BindingOptions.OnlyExisting | BindingOptions.DontThrowException;
                Predicate<IMethod> dummy = (IMethod ignored) => { return true; };
                var methods = typeDef.Methods;
                if (methods.GetMethod("Equals", bindOps, dummy) == null &&
                    methods.GetMethod("GetHashCode", bindOps, dummy) == null) {
                    should = true;
                }
            }
            return should;
        }

        public void ImplementOn(TypeDefDeclaration typeDef) {
            // Note: we don't have to look up the hierarchy when loading the variable
            // holding the identity, as we do when we implement other things, since we
            // currently only implement equality on database root classes and that is
            // also the level where we emit these variables.
            var identityField = typeDef.Fields.GetByName(TypeSpecification.ThisIdName);
            
            ImplementEquals(typeDef, identityField);
            ImplementGetHashCode(typeDef, identityField);
        }

        void ImplementEquals(TypeDefDeclaration typeDef, FieldDefDeclaration identityField) {
            var equals = new MethodDefDeclaration() {
                Name = "Equals",
                Attributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                CallingConvention = CallingConvention.HasThis
            };
            typeDef.Methods.Add(equals);
            equals.MethodBody.MaxStack = 2;
            equals.ReturnParameter = new ParameterDeclaration {
                Attributes = ParameterAttributes.Retval,
                ParameterType = module.Cache.GetIntrinsic(IntrinsicType.Boolean)
            };
            var objParameter = new ParameterDeclaration(0, "obj", module.FindType(typeof(object)));
            equals.Parameters.Add(objParameter);

            var bindableSignature = bindableType;
            using (var attached = new AttachedInstructionWriter(writer, equals)) {
                var w = attached.Writer;
                var mainSequence = w.CurrentInstructionSequence;
                var isBindableBranch = w.MethodBody.CreateInstructionSequence();
                var notReferenceEqBranch = w.MethodBody.CreateInstructionSequence();
                equals.MethodBody.RootInstructionBlock.AddInstructionSequence(
                    isBindableBranch, NodePosition.After, mainSequence);
                equals.MethodBody.RootInstructionBlock.AddInstructionSequence(
                    notReferenceEqBranch, NodePosition.After, isBindableBranch);
                var bindable = equals.MethodBody.RootInstructionBlock.DefineLocalVariable(bindableSignature, "bindable");
                equals.MethodBody.InitLocalVariables = true;

                // The C# version of the code we emit:
                //
                //IBindable bindable = obj as IBindable;
                //if (bindable == null) {
                //    return false;
                //}
                //return (object.ReferenceEquals(this, obj) || (bindable.Identity == this.__sc__this_id__));

                w.EmitInstructionParameter(OpCodeNumber.Ldarg, objParameter);
                w.EmitInstructionType(OpCodeNumber.Isinst, bindableSignature);
                w.EmitInstructionLocalVariable(OpCodeNumber.Stloc, bindable);
                w.EmitInstructionLocalVariable(OpCodeNumber.Ldloc, bindable);
                w.EmitBranchingInstruction(OpCodeNumber.Brtrue_S, isBindableBranch);
                w.EmitInstruction(OpCodeNumber.Ldc_I4_0);
                w.EmitInstruction(OpCodeNumber.Ret);
                w.DetachInstructionSequence();
                w.AttachInstructionSequence(isBindableBranch);
                w.EmitInstruction(OpCodeNumber.Ldarg_0);
                w.EmitInstructionParameter(OpCodeNumber.Ldarg, objParameter);
                w.EmitInstructionMethod(OpCodeNumber.Call, objectReferenceEqualsMethod);
                w.EmitBranchingInstruction(OpCodeNumber.Brfalse_S, notReferenceEqBranch);
                w.EmitInstruction(OpCodeNumber.Ldc_I4_1);
                w.EmitInstruction(OpCodeNumber.Ret);
                w.DetachInstructionSequence();
                w.AttachInstructionSequence(notReferenceEqBranch);
                w.EmitInstructionLocalVariable(OpCodeNumber.Ldloc, bindable);
                w.EmitInstructionMethod(OpCodeNumber.Callvirt, bindableGetIdentityMethod);
                w.EmitInstruction(OpCodeNumber.Ldarg_0);
                w.EmitInstructionField(OpCodeNumber.Ldfld, identityField);
                w.EmitInstruction(OpCodeNumber.Ceq);
                w.EmitInstruction(OpCodeNumber.Ret);
            }
        }

        void ImplementGetHashCode(TypeDefDeclaration typeDef, FieldDefDeclaration identityField) {
            var getHashCode = new MethodDefDeclaration() {
                Name = "GetHashCode",
                Attributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                CallingConvention = CallingConvention.HasThis
            };
            typeDef.Methods.Add(getHashCode);
            getHashCode.MethodBody.MaxStack = 8;
            getHashCode.ReturnParameter = new ParameterDeclaration {
                Attributes = ParameterAttributes.Retval,
                ParameterType = module.Cache.GetIntrinsic(IntrinsicType.Int32)
            };

            using (var attached = new AttachedInstructionWriter(writer, getHashCode)) {
                var w = attached.Writer;
                w.EmitInstruction(OpCodeNumber.Ldarg_0);
                w.EmitInstructionField(OpCodeNumber.Ldflda, identityField);
                w.EmitInstructionMethod(OpCodeNumber.Call, ulongGetHashCode);
                w.EmitInstruction(OpCodeNumber.Ret);
            }
        }
    }
}
