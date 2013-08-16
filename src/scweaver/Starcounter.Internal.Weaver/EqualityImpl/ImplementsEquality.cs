
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
        private IMethod bindableGetRetriverMethod;
        private IMethod objectReferenceEqualsMethod;
        private IMethod objectEqualsMethod;
        private IMethod ulongGetHashCode;

        public ImplementsEquality(ModuleDeclaration module, InstructionWriter writer) {
            this.module = module;
            this.writer = writer;
            bindableType = module.FindType(typeof(IBindable), BindingOptions.Default);
            bindableGetIdentityMethod = module.FindMethod(typeof(IBindable).GetMethod("get_Identity"), BindingOptions.Default);
            bindableGetRetriverMethod = module.FindMethod(typeof(IBindable).GetMethod("get_Retriever"), BindingOptions.Default);
            objectReferenceEqualsMethod = module.FindMethod(
                typeof(object).GetMethod("ReferenceEquals", BindingFlags.Public | BindingFlags.Static),
                BindingOptions.Default);
            objectEqualsMethod = module.FindMethod(typeof(object).GetMethod("Equals", BindingFlags.Public | BindingFlags.Instance), BindingOptions.Default);
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
                var checkRefEqualityBranch = w.MethodBody.CreateInstructionSequence();
                var compareIdentityBranch = w.MethodBody.CreateInstructionSequence();
                var returnFalseBranch = w.MethodBody.CreateInstructionSequence();

                equals.MethodBody.RootInstructionBlock.AddInstructionSequence(
                    checkRefEqualityBranch, NodePosition.After, mainSequence);
                equals.MethodBody.RootInstructionBlock.AddInstructionSequence(
                    compareIdentityBranch, NodePosition.After, checkRefEqualityBranch);
                equals.MethodBody.RootInstructionBlock.AddInstructionSequence(
                    returnFalseBranch, NodePosition.After, compareIdentityBranch);

                var bindable = equals.MethodBody.RootInstructionBlock.DefineLocalVariable(bindableSignature, "bindable");
                var self = equals.MethodBody.RootInstructionBlock.DefineLocalVariable(bindableSignature, "self");
                equals.MethodBody.InitLocalVariables = true;

                // The C# version of the code we emit:
                //
                //public override bool Equals(object obj)
                //{
                //    IBindable bindable = obj as IBindable;
                //    if (bindable != null)
                //    {
                //        if (object.ReferenceEquals(this, obj))
                //        {
                //            return true;
                //        }
                //        if (bindable.Identity == this.__sc__this_id__)
                //        {
                //            IBindable self = this;
                //            return bindable.Retriever.Equals(self.Retriever);
                //        }
                //    }
                //    return false;
                //}

                w.EmitInstructionParameter(OpCodeNumber.Ldarg, objParameter);
                w.EmitInstructionType(OpCodeNumber.Isinst, bindableSignature);
                w.EmitInstructionLocalVariable(OpCodeNumber.Stloc, bindable);
                w.EmitInstructionLocalVariable(OpCodeNumber.Ldloc, bindable);
                w.EmitBranchingInstruction(OpCodeNumber.Brtrue_S, checkRefEqualityBranch);
                w.EmitInstruction(OpCodeNumber.Ldc_I4_0);
                w.EmitInstruction(OpCodeNumber.Ret);
                w.DetachInstructionSequence();
                
                w.AttachInstructionSequence(checkRefEqualityBranch);
                w.EmitInstruction(OpCodeNumber.Ldarg_0);
                w.EmitInstructionParameter(OpCodeNumber.Ldarg, objParameter);
                w.EmitInstructionMethod(OpCodeNumber.Call, objectReferenceEqualsMethod);
                w.EmitBranchingInstruction(OpCodeNumber.Brfalse_S, compareIdentityBranch);
                w.EmitInstruction(OpCodeNumber.Ldc_I4_1);
                w.EmitInstruction(OpCodeNumber.Ret);
                w.DetachInstructionSequence();

                w.AttachInstructionSequence(compareIdentityBranch);
                w.EmitInstructionLocalVariable(OpCodeNumber.Ldloc, bindable);
                w.EmitInstructionMethod(OpCodeNumber.Callvirt, bindableGetIdentityMethod);
                w.EmitInstruction(OpCodeNumber.Ldarg_0);
                w.EmitInstructionField(OpCodeNumber.Ldfld, identityField);
                w.EmitBranchingInstruction(OpCodeNumber.Bne_Un_S, returnFalseBranch);
                w.EmitInstruction(OpCodeNumber.Ldarg_0);
                w.EmitInstructionLocalVariable(OpCodeNumber.Stloc, self);
                w.EmitInstructionLocalVariable(OpCodeNumber.Ldloc, bindable);
                w.EmitInstructionMethod(OpCodeNumber.Callvirt, bindableGetRetriverMethod);
                w.EmitInstructionLocalVariable(OpCodeNumber.Ldloc, self);
                w.EmitInstructionMethod(OpCodeNumber.Callvirt, bindableGetRetriverMethod);
                w.EmitInstructionMethod(OpCodeNumber.Callvirt, objectEqualsMethod);
                w.EmitInstruction(OpCodeNumber.Ret);
                w.DetachInstructionSequence();

                w.AttachInstructionSequence(returnFalseBranch);
                w.EmitInstruction(OpCodeNumber.Ldc_I4_0);
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
