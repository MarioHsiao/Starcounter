// ***********************************************************************
// <copyright file="ReimplementWeavedLucentAccessorAdvice.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeWeaver;
using PostSharp.Sdk.Collections;
using Starcounter.Internal;
using Starcounter.Internal.Weaver;
using System.Reflection;

namespace Starcounter.LucentObjects
{
    /// <summary>
    /// Advice that changes the implementation of persistent getters and
    /// setters that has been pre-weaved by the IPC weaver.
    /// </summary>
    internal class ReimplementWeavedLucentAccessorAdvice : IAdvice
    {
        /// <summary>
        /// The attribute index
        /// </summary>
        private int attributeIndex;
        /// <summary>
        /// The state method provider
        /// </summary>
        private DbStateMethodProvider stateMethodProvider;
        /// <summary>
        /// The generated getter
        /// </summary>
        private IMethod generatedGetter;
        /// <summary>
        /// The generated setter
        /// </summary>
        private IMethod generatedSetter;

        /// <summary>
        /// Gets the accessor property.
        /// </summary>
        /// <value>The accessor property.</value>
        public PropertyDeclaration AccessorProperty { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReimplementWeavedLucentAccessorAdvice" /> class.
        /// </summary>
        /// <param name="methodProvider">The method provider.</param>
        /// <param name="property">The property.</param>
        /// <param name="attributeIndex">Index of the attribute.</param>
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
        /// <value>The priority.</value>
        public int Priority
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// Requireses the weave.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
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

        /// <summary>
        /// Weaves the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="block">The block.</param>
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
