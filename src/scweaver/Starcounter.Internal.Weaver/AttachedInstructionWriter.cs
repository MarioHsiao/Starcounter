
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.Collections;
using System;

namespace Starcounter.Internal.Weaver {
    
    /// <summary>
    /// Utility class allowing the implementation of new method bodies
    /// to be carried out in a using-block.
    /// </summary>
    /// <example>
    /// using (var w = new AttachedWriter(writer, foo)) {
    ///   w.Writer.Emit(OpCode.Nop);
    /// }
    /// </example>
    internal class AttachedInstructionWriter : IDisposable {
        public readonly InstructionWriter Writer;

        public AttachedInstructionWriter(InstructionWriter writer, MethodDefDeclaration method) {
            this.Writer = writer;
            var rootBlock = method.MethodBody.CreateInstructionBlock();
            method.MethodBody.RootInstructionBlock = rootBlock;
            var instructions = method.MethodBody.CreateInstructionSequence();
            rootBlock.AddInstructionSequence(instructions, NodePosition.After, null);
            writer.AttachInstructionSequence(instructions);
        }

        void IDisposable.Dispose() {
            Writer.DetachInstructionSequence();
        }
    }
}