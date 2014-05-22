
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.Collections;
using PostSharp.Sdk.Extensibility;
using PostSharp.Sdk.Extensibility.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Starcounter.Internal.Weaver {
    /// <summary>
    /// A task that is responsible for the transformation of calls to
    /// a few <see cref="System.Thread"/> methods, like <see cref="System.Thread"/>, 
    /// to adapt user code to the cooperative scheduler.
    /// </summary>
    public sealed class ReplaceCEQWithEqualityCallTask : Task {
        IMethod runtimeEqualityMethod;

        /// <summary>
        /// Executes the current task, i.e. effectively looking for and
        /// trapping the thread methods we support.
        /// </summary>
        /// <returns><c>true</c> if successfull; <c>false</c> otherwise.
        /// </returns>
        public override Boolean Execute() {
            var module = Project.Module;
            runtimeEqualityMethod = module.FindMethod(typeof(HostedThread).GetMethod("RuntimeCompareEquality"), BindingOptions.Default);

            var methodsEnumerator = module.GetDeclarationEnumerator(TokenType.MethodDef);
            while (methodsEnumerator.MoveNext()) {
                var methodDef = (MethodDefDeclaration)methodsEnumerator.Current;
                if (!methodDef.HasBody) {
                    continue;
                }

                InstructionWriter writer = new InstructionWriter();
                ProcessBlock(
                    methodDef.MethodBody.RootInstructionBlock,
                    methodDef.MethodBody.CreateInstructionReader(false),
                    writer
                    );
            }

            return true;
        }

        void ProcessBlock(
            InstructionBlock block, InstructionReader reader, InstructionWriter writer) {
            if (block.HasChildrenBlocks) {
                InstructionBlock child = block.FirstChildBlock;
                while (child != null) {
                    ProcessBlock(child, reader, writer);
                    child = child.NextSiblingBlock;
                }
            } else {
                InstructionSequence sequence = block.FirstInstructionSequence;
                while (sequence != null) {
                    ProcessSequence(sequence, reader, writer);
                    sequence = sequence.NextSiblingSequence;
                }
            }
        }

        void ProcessSequence(
            InstructionSequence sequence, 
            InstructionReader reader, 
            InstructionWriter writer) {
            
            bool changed = false;
            reader.EnterInstructionSequence(sequence);
            writer.AttachInstructionSequence(sequence);
            while (reader.ReadInstruction()) {
                var opCodeNumber = reader.CurrentInstruction.OpCodeNumber;
                if (opCodeNumber == OpCodeNumber.Ceq) {
                    writer.EmitInstructionMethod(OpCodeNumber.Call, runtimeEqualityMethod);
                    changed = true;
                    continue;
                }
                reader.CurrentInstruction.Write(writer);
            }
            writer.DetachInstructionSequence(changed);
        }
    }
}