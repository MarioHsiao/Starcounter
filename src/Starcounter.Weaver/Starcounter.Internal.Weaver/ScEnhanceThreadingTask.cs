// ***********************************************************************
// <copyright file="ScEnhanceThreadingTask.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.Collections;
using PostSharp.Sdk.Extensibility;
using PostSharp.Sdk.Extensibility.Tasks;
using System;
using System.Threading;

namespace Starcounter.Internal.Weaver {
    /// <summary>
    /// A task that is responsible for the transformation of calls to
    /// a few <see cref="System.Thread"/> methods, like <see cref="System.Thread"/>, 
    /// to adapt user code to the cooperative scheduler.
    /// </summary>
    public sealed class ScEnhanceThreadingTask : Task {
        /// <summary>
        /// Executes the current task, i.e. effectively looking for and
        /// trapping the thread methods we support.
        /// </summary>
        /// <returns><c>true</c> if successfull; <c>false</c> otherwise.
        /// </returns>
        public override Boolean Execute() {
            var module = Project.Module;
            var binding = BindingOptions.OnlyExisting | BindingOptions.DontThrowException;

            // Check if the current module even reference System.Threading
            // and the Thread class. If not, no need to process it.
            
            var threadTypeRef = (TypeRefDeclaration)module.FindType(typeof(Thread), binding);    
            if (threadTypeRef != null) {
                var hostedThreadType = (TypeRefDeclaration)module.Cache.GetType(typeof(HostedThread));

                FindAndReplaceThreadSetPriorityCalls(module, threadTypeRef, hostedThreadType);
                FindAndReplaceThreadSleepInt32Calls(module, threadTypeRef, hostedThreadType);
                FindAndReplaceThreadSleepTimeSpanCalls(module, threadTypeRef, hostedThreadType);
            }
            
            return true;
        }

        /// <summary>
        /// Finds any call setting <see cref="Thread.Priority"/> in the
        /// given <paramref name="module"/> and replaces each such call
        /// with a call to <see cref="HostedThread.SetPriority"/>.
        /// </summary>
        /// <param name="module">The module to investigate.</param>
        /// <param name="threadTypeRef">The type reference to the .NET
        /// type <see cref="System.Thread"/>, referenced from the given
        /// <paramref name="module"/>.</param>
        /// <param name="hostedThreadType">The type reference to the
        /// <see cref="HostedThread"/> type, which we'll use as the new
        /// target fall the calls we are replacing.</param>
        private void FindAndReplaceThreadSetPriorityCalls(
            ModuleDeclaration module,
            TypeRefDeclaration threadTypeRef,
            TypeRefDeclaration hostedThreadType) {
            var threadSetPriorityMethod = (MethodRefDeclaration)
                threadTypeRef.MethodRefs.GetMethod("set_Priority",
                new MethodSignature(
                    module,
                    CallingConvention.Default,
                    module.Cache.GetIntrinsic(IntrinsicType.Void),
                    new[] { module.Cache.GetType(typeof(ThreadPriority)) },
                    0),
                    BindingOptions.Default
                    );
            
            var affectedMethods = FindAffectedMethods(threadSetPriorityMethod);
            if (affectedMethods.Count == 0) {
                return;
            }
            
            var hostedThreadSetPriorityMethod = (MethodRefDeclaration)
                hostedThreadType.MethodRefs.GetMethod("SetPriority",
                new MethodSignature(
                    module,
                    CallingConvention.Default,
                    module.Cache.GetIntrinsic(IntrinsicType.Void),
                    new[] {
                        module.Cache.GetType(typeof(Thread)),
                        module.Cache.GetType(typeof(ThreadPriority))
                    },
                    0),
                    BindingOptions.Default
                    );

            ReplaceMethodCalls(affectedMethods, threadSetPriorityMethod, hostedThreadSetPriorityMethod);
        }

        /// <summary>
        /// Finds any call to <see cref="Thread.Sleep(int32)"/> in the
        /// given <paramref name="module"/> and replaces each such call
        /// with a call to <see cref="HostedThread.Sleep(int32)"/>.
        /// </summary>
        /// <param name="module">The module to investigate.</param>
        /// <param name="threadTypeRef">The type reference to the .NET
        /// type <see cref="System.Thread"/>, referenced from the given
        /// <paramref name="module"/>.</param>
        /// <param name="hostedThreadType">The type reference to the
        /// <see cref="HostedThread"/> type, which we'll use as the new
        /// target fall the calls we are replacing.</param>
        private void FindAndReplaceThreadSleepInt32Calls(
            ModuleDeclaration module,
            TypeRefDeclaration threadTypeRef,
            TypeRefDeclaration hostedThreadType) {

            var threadSleepMethod = (MethodRefDeclaration)threadTypeRef.MethodRefs.GetMethod(
                "Sleep",
                new MethodSignature(
                    module,
                    CallingConvention.Default,
                    module.Cache.GetIntrinsic(IntrinsicType.Void),
                    new ITypeSignature[] { module.Cache.GetIntrinsic(IntrinsicType.Int32) },
                    0),
                    BindingOptions.Default
                    );
            var affectedMethods = FindAffectedMethods(threadSleepMethod);
            if (affectedMethods.Count == 0) {
                return;
            }

            var hostedThreadSleepMethod = (MethodRefDeclaration)hostedThreadType.MethodRefs.GetMethod(
                "Sleep",
                new MethodSignature(
                    module,
                    CallingConvention.Default,
                    module.Cache.GetIntrinsic(IntrinsicType.Void),
                    new ITypeSignature[] { module.Cache.GetIntrinsic(IntrinsicType.Int32) },
                    0),
                    BindingOptions.Default
                    );

            ReplaceMethodCalls(affectedMethods, threadSleepMethod, hostedThreadSleepMethod);
        }

        /// <summary>
        /// Finds any call to <see cref="Thread.Sleep(TimeSpan)"/> in the
        /// given <paramref name="module"/> and replaces each such call
        /// with a call to <see cref="HostedThread.Sleep(TimeSpan)"/>.
        /// </summary>
        /// <param name="module">The module to investigate.</param>
        /// <param name="threadTypeRef">The type reference to the .NET
        /// type <see cref="System.Thread"/>, referenced from the given
        /// <paramref name="module"/>.</param>
        /// <param name="hostedThreadType">The type reference to the
        /// <see cref="HostedThread"/> type, which we'll use as the new
        /// target fall the calls we are replacing.</param>
        private void FindAndReplaceThreadSleepTimeSpanCalls(
            ModuleDeclaration module, 
            TypeRefDeclaration threadTypeRef, 
            TypeRefDeclaration hostedThreadType) {
            var threadSleepMethod = (MethodRefDeclaration)threadTypeRef.MethodRefs.GetMethod(
                "Sleep",
                new MethodSignature(
                    module,
                    CallingConvention.Default,
                    module.Cache.GetIntrinsic(IntrinsicType.Void), 
                    new[] { module.Cache.GetType(typeof(TimeSpan)) },
                    0),
                    BindingOptions.Default
            );
            
            var affectedMethods = FindAffectedMethods(threadSleepMethod);
            if (affectedMethods.Count == 0) {
                return;
            }
            
            
            var hostedThreadSleepMethod = (MethodRefDeclaration)hostedThreadType.MethodRefs.GetMethod(
                "Sleep",
                new MethodSignature(
                    module,
                    CallingConvention.Default,
                    module.Cache.GetIntrinsic(IntrinsicType.Void),
                    new[] { module.Cache.GetType(typeof(TimeSpan)) },
                    0),
                    BindingOptions.Default
            );

            ReplaceMethodCalls(affectedMethods, threadSleepMethod, hostedThreadSleepMethod);
        }

        private static Set<MethodDefDeclaration> FindAffectedMethods(MethodRefDeclaration methodRef) {
            var affectedMethods = new Set<MethodDefDeclaration>(64);
#pragma warning disable 612
            foreach (MethodDefDeclaration methodDef in IndexUsagesTask.GetUsedBy(methodRef)) {
#pragma warning restore 612
                affectedMethods.AddIfAbsent(methodDef);
            }
            return affectedMethods;
        }

        private void ReplaceMethodCalls(Set<MethodDefDeclaration> affectedMethods, IMethod toReplace, IMethod replaceWith) {
            InstructionWriter writer = new InstructionWriter();
            foreach (MethodDefDeclaration methodDef in affectedMethods) {
                ProcessBlock(
                    methodDef.MethodBody.RootInstructionBlock,
                    methodDef.MethodBody.CreateInstructionReader(false),
                    writer,
                    toReplace,
                    replaceWith
                );
            }
        }

        private static void ProcessBlock(
            InstructionBlock block, InstructionReader reader, InstructionWriter writer, IMethod toReplace, IMethod replaceWith) {
            if (block.HasChildrenBlocks) {
                InstructionBlock child = block.FirstChildBlock;
                while (child != null) {
                    ProcessBlock(child, reader, writer, toReplace, replaceWith);
                    child = child.NextSiblingBlock;
                }
            } else {
                InstructionSequence sequence = block.FirstInstructionSequence;
                while (sequence != null) {
                    ProcessSequence(sequence, reader, writer, toReplace, replaceWith);
                    sequence = sequence.NextSiblingSequence;
                }
            }
        }

        private static void ProcessSequence(
            InstructionSequence sequence, InstructionReader reader, InstructionWriter writer, IMethod toReplace, IMethod replaceWith) {
            OpCodeNumber opCodeToReplace = toReplace.GetMethodDefinition().IsStatic ? OpCodeNumber.Call : OpCodeNumber.Callvirt;
            bool changed = false;
            reader.EnterInstructionSequence(sequence);
            writer.AttachInstructionSequence(sequence);
            while (reader.ReadInstruction()) {
                OpCodeNumber opCodeNumber = reader.CurrentInstruction.OpCodeNumber;
                if (opCodeNumber == opCodeToReplace) {
                    IMethod method = reader.CurrentInstruction.MethodOperand;
                    if (method.Equals(toReplace)) {
                        writer.EmitInstructionMethod(OpCodeNumber.Call, replaceWith);
                        changed = true;
                        continue;
                    }
                }
                reader.CurrentInstruction.Write(writer);
            }
            writer.DetachInstructionSequence(changed);
        }
    }
}