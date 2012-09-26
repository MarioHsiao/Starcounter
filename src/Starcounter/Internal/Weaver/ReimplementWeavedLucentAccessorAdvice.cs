
using System;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeWeaver;
using PostSharp.Sdk.Collections;
using Starcounter.Internal.Weaver;
using Sc.Server.Internal;
using System.Reflection;

namespace Starcounter.LucentObjects
{
    /// <summary>
    /// Advice that changes the implementation of persistent getters and
    /// setters that has been pre-weaved by the IPC weaver.
    /// </summary>
    internal class ReimplementWeavedLucentAccessorAdvice : IAdvice
    {
        private int attributeIndex;
        private DbStateMethodProvider stateMethodProvider;
        private IMethod generatedGetter;
        private IMethod generatedSetter;

        public PropertyDeclaration AccessorProperty { get; private set; }

        public ReimplementWeavedLucentAccessorAdvice(
            DbStateMethodProvider methodProvider,
            PropertyDeclaration property,
            int attributeIndex)
        {
            this.stateMethodProvider = methodProvider;
            this.AccessorProperty = property;
            this.attributeIndex = attributeIndex;
        }

        /// <summary>
        /// Gets the aspect priority (not important since we have only one aspect).
        /// </summary>
        public int Priority
        {
            get
            {
                return 0;
            }
        }

        public bool RequiresWeave(WeavingContext context)
        {
            // We weave all get field instructions inside accessors, since we
            // know we have emitted only the instruction(s) to load the static
            // attribute index field.

            if (context.JoinPoint.JoinPointKind == JoinPointKinds.InsteadOfGetField)
                return true;

            // When the joinpoint represents a call, we make sure the call is a
            // call to one of the database access methods and that we really have
            // a replacement for it in the (optmized) generated state provider.
            // If not, we cancel weaving.

            MethodRefDeclaration targetMethod;
            IMethod generatedAccessMethod;

            targetMethod = (MethodRefDeclaration)context.JoinPoint.Instruction.MethodOperand;

            if (!targetMethod.DeclaringType.GetReflectionName().Equals(typeof(DbState).FullName))
                return false;

            if (!stateMethodProvider.TryGetGeneratedMethodByName(targetMethod.Name, out generatedAccessMethod))
                return false;

            if (targetMethod.Name.StartsWith("Read"))
                generatedGetter = generatedAccessMethod;
            else
                generatedSetter = generatedAccessMethod;

            return true;
        }

        public void Weave(WeavingContext context, InstructionBlock block)
        {
            InstructionSequence sequence;

            switch (context.JoinPoint.JoinPointKind)
            {
                case JoinPointKinds.InsteadOfGetField:

                    // No matter if we are in the getter or setter, we need to just
                    // replace the ldsfld with a lc.4 using our attribute index.

                    sequence = block.MethodBody.CreateInstructionSequence();
                    block.AddInstructionSequence(sequence, NodePosition.Before, null);
                    context.InstructionWriter.AttachInstructionSequence(sequence);

                    context.InstructionWriter.EmitInstructionInt32(OpCodeNumber.Ldc_I4, attributeIndex);

                    context.InstructionWriter.DetachInstructionSequence();
                    break;

                case JoinPointKinds.InsteadOfCall:
                    
                    MethodRefDeclaration targetMethod;
                    IMethod replacementMethod;
                    
                    targetMethod = (MethodRefDeclaration)context.JoinPoint.Instruction.MethodOperand;
                    replacementMethod = targetMethod.Name.StartsWith("Read")
                        ? generatedGetter
                        : generatedSetter;

                    sequence = block.MethodBody.CreateInstructionSequence();
                    block.AddInstructionSequence(sequence, NodePosition.Before, null);
                    context.InstructionWriter.AttachInstructionSequence(sequence);

                    ScTransformTrace.Instance.WriteLine(
                        "Replacing call to {0}.{1}, with call to {2}.{3}",
                        targetMethod.DeclaringType.GetReflectionName(ReflectionNameOptions.SkipNamespace),
                        targetMethod.Name,
                        replacementMethod.DeclaringType.GetReflectionName(ReflectionNameOptions.SkipNamespace),
                        replacementMethod.Name
                        );
                    
                    context.InstructionWriter.EmitInstructionMethod(OpCodeNumber.Call, replacementMethod);
                    context.InstructionWriter.DetachInstructionSequence();

                    break;

                default:
                    throw ErrorCode.ToException(Error.SCERRUNSPECIFIED);
            }
        }
    }
}
