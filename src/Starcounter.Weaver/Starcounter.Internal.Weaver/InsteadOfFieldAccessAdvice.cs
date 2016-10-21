// ***********************************************************************
// <copyright file="InsteadOfFieldAccessAdvice.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeWeaver;
using PostSharp.Sdk.Collections;
using PostSharp.Extensibility;
using Sc.Server.Weaver;
using Starcounter;
using AssertionFailedException = PostSharp.Sdk.AssertionFailedException;
using IMethod = PostSharp.Sdk.CodeModel.IMethod;

namespace Starcounter.Internal.Weaver {
    /// <summary>
    /// Advice of the low-level code weaver that changes field accesses to method calls.
    /// </summary>
    internal class InsteadOfFieldAccessAdvice : IAdvice {
        /// <summary>
        /// The next instruction
        /// </summary>
        private OpCodeNumber nextInstruction;
        /// <summary>
        /// The get method
        /// </summary>
        private readonly IMethod getMethod;
        /// <summary>
        /// The set method
        /// </summary>
        private readonly IMethod setMethod;
        /// <summary>
        /// The field
        /// </summary>
        private readonly IField field;

        /// <summary>
        /// Initializes a new <see cref="InsteadOfFieldAccessAdvice" />.
        /// </summary>
        /// <param name="field">Field whose accesses should be replaced.</param>
        /// <param name="getMethod">Method getting the field value.</param>
        /// <param name="setMethod">Method setting the field value.</param>
        public InsteadOfFieldAccessAdvice(IField field, IMethod getMethod, IMethod setMethod) {
            this.field = field;
            this.getMethod = getMethod;
            this.setMethod = setMethod;
        }

        /// <summary>
        /// Gets the aspect priority (not important since we have only one aspect).
        /// </summary>
        /// <value>The priority.</value>
        public int Priority {
            get {
                return 0;
            }
        }

        /// <summary>
        /// Determines whether we are 'interested' by the current join point.
        /// We answer always <b>true</b>.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <returns><b>true</b></returns>
        public bool RequiresWeave(WeavingContext context) {
            // Stuff the instruction following this load field instruction;
            // we must do this to later determine what kind of instruction
            // the compiler has emitted. The MS compiler produces an initobj
            // for value types, and that fails when using Nullable persistent
            // fields
            InstructionReaderBookmark bookmark = context.InstructionReader.CreateBookmark();
            nextInstruction = OpCodeNumber.Nop;
            if (context.InstructionReader.ReadInstruction()) {
                this.nextInstruction = context.InstructionReader.CurrentInstruction.OpCodeNumber;
                context.InstructionReader.GoToBookmark(bookmark);
            }
            return true;
        }

        /// <summary>
        /// Gets the field to which the current advice applis.
        /// </summary>
        /// <value>The field.</value>
        public IField Field {
            get {
                return this.field;
            }
        }


        /// <summary>
        /// Main method: called by the weaver when a 'field store' or 'field load' instruction
        /// is found.
        /// </summary>
        /// <param name="context">Context.</param>
        /// <param name="block">Block into which we have to write our instructions.</param>
        /// <exception cref="AssertionFailedException"></exception>
        public void Weave(WeavingContext context, InstructionBlock block) {
            InstructionSequence sequence = context.InstructionBlock.MethodBody.CreateInstructionSequence();
            block.AddInstructionSequence(sequence, NodePosition.After, null);
            context.InstructionWriter.AttachInstructionSequence(sequence);
            ScTransformTrace.Instance.WriteLine("Weaving instruction {{{0}}} for field {{{1}}}.",
                                                context.JoinPoint.Instruction.OpCodeNumber,
                                                context.JoinPoint.Instruction.FieldOperand);
            if (context.JoinPoint.Instruction.SymbolSequencePoint != null) {
                context.InstructionWriter.EmitSymbolSequencePoint(context.JoinPoint.Instruction.SymbolSequencePoint);
            }

            if (context.Method.Name == ".ctor" &&
                ScAnalysisTask.IsInitializationBlock(block.ParentBlock) &&
                context.JoinPoint.Instruction.OpCodeNumber == OpCodeNumber.Stfld) {
                // We are preventing field store instructions in the initialization
                // block of constructors - see ValidateConstructor in ScAnalysisTask.
                // So we should never get here any more. Let's assert we don't for a
                // while so what haven't missed something.
                throw new AssertionFailedException(
                    string.Format("Detected unexpected Stfld instruction in constructor initialization block, method: {0}", context.Method.GetDisplayName()));

            } else {
                switch (context.JoinPoint.Instruction.OpCodeNumber) {
                    case OpCodeNumber.Ldfld:
                        context.InstructionWriter.EmitInstructionMethod(OpCodeNumber.Call, this.getMethod);
                        break;
                    case OpCodeNumber.Stfld:
                        if (this.setMethod != null) {
                            context.InstructionWriter.EmitInstructionMethod(OpCodeNumber.Call, this.setMethod);
                        } else {
                            // There is no set method. This means that the field is read-only.
                            // Replace the 'store field' instruction by a stack-equivalent sequence
                            // and emit an error.
                            context.InstructionWriter.EmitInstruction(OpCodeNumber.Pop);
                            context.InstructionWriter.EmitInstruction(OpCodeNumber.Pop);
                            // Now emit the error.
                            ScMessageSource.Write(
                                SeverityType.Error, 
                                "SCATV03", 
                                new object[] { ((INamedType) field.DeclaringType).Name, field.Name});
                        }
                        break;
                    case OpCodeNumber.Ldflda: {
                            // Instead of "load field address", we store the field value in a local variable
                            // and we give the address of this local variable. This value should not be modified!
                            LocalVariableSymbol fieldValueLocal =
                                context.Method.MethodBody.RootInstructionBlock.DefineLocalVariable(
                                    field.FieldType, "~fieldValue~" + field.Name + "~{0}");
                            context.Method.MethodBody.InitLocalVariables = true;
                            if (this.nextInstruction == OpCodeNumber.Initobj) {
                                // This is a special case when setting persistent nullable fields
                                // to null. In such case, the MS IL-compiler emits a different
                                // sequence compared to sets that has a value, namely an initobj
                                // instruction (since null is the default value) after loading the
                                // plain field-address. We have to protect against this
                                // Instead, load the local (stack) variable address and init that,
                                // and put the initialized value on the stack...
                                context.InstructionWriter.EmitInstructionLocalVariable(OpCodeNumber.Ldloca, fieldValueLocal);
                                context.InstructionWriter.EmitInstructionType(OpCodeNumber.Initobj, field.FieldType);
                                context.InstructionWriter.EmitInstructionLocalVariable(OpCodeNumber.Ldloc, fieldValueLocal);
                                // ... before we invoke the set
                                IMethod method = field.DeclaringType.Methods.GetMethod(this.setMethod.Name, this.setMethod,
                                                                                       BindingOptions.Default);
                                context.InstructionWriter.EmitInstructionMethod(OpCodeNumber.Call, method);
                                // Load the dummy variable address again, to not mess up the upcoming
                                // initobj instruction
                                context.InstructionWriter.EmitInstructionLocalVariable(OpCodeNumber.Ldloca, fieldValueLocal);
                            } else {
                                context.InstructionWriter.EmitInstructionMethod(OpCodeNumber.Call, this.getMethod);
                                context.InstructionWriter.EmitInstructionLocalVariable(OpCodeNumber.Stloc, fieldValueLocal);
                                context.InstructionWriter.EmitInstructionLocalVariable(OpCodeNumber.Ldloca, fieldValueLocal);
                            }
                        }
                        break;
                    default:
                        throw new AssertionFailedException(
                            string.Format("Unexpected opcode: {0}.", context.JoinPoint.Instruction.OpCodeNumber));
                }
            }
            context.InstructionWriter.DetachInstructionSequence();
        }
    }
}