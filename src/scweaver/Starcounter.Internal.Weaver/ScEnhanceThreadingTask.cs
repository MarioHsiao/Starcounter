// ***********************************************************************
// <copyright file="ScEnhanceThreadingTask.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Threading;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.Collections;
using PostSharp.Sdk.Extensibility;
using PostSharp.Sdk.Extensibility.Tasks;

namespace Starcounter.Internal.Weaver {
    /// <summary>
    /// A task that is responsible for the transformation of calls to
    /// a few System.Thread methods, like Thread.Sleep, to adapt user
    /// code to the cooperative scheduler.
    /// </summary>
    /// <remarks>
    /// This code is currently not called during weaving, since it is
    /// kind of out-of-date. If we must reintroduce it, we have to move
    /// the code that is weaved into the user code - i.e. the InterceptThread
    /// class - to the Starcounter assembly, since it will otherwise result
    /// in a reference to the weaver executable when weaved and recompiled.
    /// </remarks>
    public sealed class ScEnhanceThreadingTask : Task {
        /// <summary>
        /// Executes this instance.
        /// </summary>
        /// <returns>Boolean.</returns>
        public override Boolean Execute() {
            var module = Project.Module;
            Thread_Priority(module);
            Thread_SleepA(module);
            Thread_SleepB(module);
            return true;
        }

        /// <summary>
        /// Thread_s the priority.
        /// </summary>
        /// <param name="module">The module.</param>
        private void Thread_Priority(ModuleDeclaration module) {
            TypeRefDeclaration threadTypeRef = (TypeRefDeclaration)module.FindType(
                                                   typeof(Thread),
                                                   BindingOptions.OnlyExisting | BindingOptions.DontThrowException
                                               );
            if (threadTypeRef == null) {
                return;
            }
            MethodRefDeclaration threadSetPriorityMethod = (MethodRefDeclaration)threadTypeRef.MethodRefs.GetMethod(
                                                               "set_Priority",
                                                               new MethodSignature(
                                                                   module,
                                                                   CallingConvention.Default,
                                                                   module.Cache.GetIntrinsic(IntrinsicType.Void),
                                                                   new[] { module.Cache.GetType(typeof(ThreadPriority)) },
                                                                   0),
                                                               BindingOptions.Default
                                                           );
            Set<MethodDefDeclaration> affectedMethods = FindAffectedMethods(threadSetPriorityMethod);
            if (affectedMethods.Count == 0) {
                return;
            }
            TypeRefDeclaration interceptThreadType = (TypeRefDeclaration)module.Cache.GetType(
                                                         typeof(HostedThread)
                                                     );
            MethodRefDeclaration interceptThreadSetPriorityMethod = (MethodRefDeclaration)interceptThreadType.MethodRefs.GetMethod(
                                                                        "set_Priority",
                                                                        new MethodSignature(
                                                                                module,
                                                                                CallingConvention.Default,
                                                                                module.Cache.GetIntrinsic(IntrinsicType.Void),
                                                                                new[]
        {
            module.Cache.GetType(typeof(Thread)),
            module.Cache.GetType(typeof(ThreadPriority))
        },
            0
                                                                        ),
                                                                        BindingOptions.Default
                                                                    );
            ReplaceMethodCalls(affectedMethods, threadSetPriorityMethod, interceptThreadSetPriorityMethod);
        }

        /// <summary>
        /// Thread_s the sleep A.
        /// </summary>
        /// <param name="module">The module.</param>
        private void Thread_SleepA(ModuleDeclaration module) {
            TypeRefDeclaration threadType = (TypeRefDeclaration)module.FindType(
                                                typeof(Thread),
                                                BindingOptions.OnlyExisting | BindingOptions.DontThrowException
                                            );
            if (threadType == null) {
                return;
            }
            MethodRefDeclaration threadSleepMethod = (MethodRefDeclaration)threadType.MethodRefs.GetMethod(
                                                         "Sleep",
                                                         new MethodSignature(
                                                             module,
                                                             CallingConvention.Default,
                                                             module.Cache.GetIntrinsic(IntrinsicType.Void),
                                                             new ITypeSignature[] { module.Cache.GetIntrinsic(IntrinsicType.Int32) },
            0),
                                                         BindingOptions.Default
                                                     );
            Set<MethodDefDeclaration> affectedMethods = FindAffectedMethods(threadSleepMethod);
            if (affectedMethods.Count == 0) {
                return;
            }
            TypeRefDeclaration interceptThreadType = (TypeRefDeclaration)module.Cache.GetType(
                typeof(HostedThread)
            );
            MethodRefDeclaration interceptThreadSleepMethod = (MethodRefDeclaration)interceptThreadType.MethodRefs.GetMethod(
                "Sleep",
                new MethodSignature(
                    module,
                    CallingConvention.Default,
                    module.Cache.GetIntrinsic(IntrinsicType.Void),
                    new ITypeSignature[] { module.Cache.GetIntrinsic(IntrinsicType.Int32) },
            0
                ),
                BindingOptions.Default
            );
            ReplaceMethodCalls(affectedMethods, threadSleepMethod, interceptThreadSleepMethod);
        }

        /// <summary>
        /// Thread_s the sleep B.
        /// </summary>
        /// <param name="module">The module.</param>
        private void Thread_SleepB(ModuleDeclaration module) {
            TypeRefDeclaration threadType = (TypeRefDeclaration)module.FindType(
                typeof(Thread),
                BindingOptions.OnlyExisting | BindingOptions.DontThrowException
            );
            if (threadType == null) {
                return;
            }
            MethodRefDeclaration threadSleepMethod = (MethodRefDeclaration)threadType.MethodRefs.GetMethod(
                "Sleep",
                new MethodSignature(
                    module,
                    CallingConvention.Default,
                    module.Cache.GetIntrinsic(IntrinsicType.Void),
            new[] { module.Cache.GetType(typeof(TimeSpan)) },
            0),
                BindingOptions.Default
            );
            Set<MethodDefDeclaration> affectedMethods = FindAffectedMethods(threadSleepMethod);
            if (affectedMethods.Count == 0) {
                return;
            }
            TypeRefDeclaration interceptThreadType = (TypeRefDeclaration)module.Cache.GetType(
                typeof(HostedThread)
            );
            MethodRefDeclaration interceptThreadSleepMethod = (MethodRefDeclaration)interceptThreadType.MethodRefs.GetMethod(
                "Sleep",
                new MethodSignature(
                    module,
                    CallingConvention.Default,
                    module.Cache.GetIntrinsic(IntrinsicType.Void),
            new[] { module.Cache.GetType(typeof(TimeSpan)) },
            0
                ),
                BindingOptions.Default
            );
            ReplaceMethodCalls(affectedMethods, threadSleepMethod, interceptThreadSleepMethod);
        }

        /// <summary>
        /// Finds the affected methods.
        /// </summary>
        /// <param name="methodRef">The method ref.</param>
        /// <returns>Set{MethodDefDeclaration}.</returns>
        private static Set<MethodDefDeclaration> FindAffectedMethods(MethodRefDeclaration methodRef) {
            Set<MethodDefDeclaration> affectedMethods = new Set<MethodDefDeclaration>(64);
#pragma warning disable 612
            foreach (MethodDefDeclaration methodDef in IndexUsagesTask.GetUsedBy(methodRef)) {
#pragma warning restore 612
                affectedMethods.AddIfAbsent(methodDef);
            }
            return affectedMethods;
        }

        /// <summary>
        /// Replaces the method calls.
        /// </summary>
        /// <param name="affectedMethods">The affected methods.</param>
        /// <param name="toReplace">To replace.</param>
        /// <param name="replaceWith">The replace with.</param>
        private void ReplaceMethodCalls(Set<MethodDefDeclaration> affectedMethods, IMethod toReplace,
        IMethod replaceWith) {
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

        /// <summary>
        /// Processes the block.
        /// </summary>
        /// <param name="block">The block.</param>
        /// <param name="reader">The reader.</param>
        /// <param name="writer">The writer.</param>
        /// <param name="toReplace">To replace.</param>
        /// <param name="replaceWith">The replace with.</param>
        private static void ProcessBlock(InstructionBlock block, InstructionReader reader, InstructionWriter writer,
        IMethod toReplace, IMethod replaceWith) {
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

        /// <summary>
        /// Processes the sequence.
        /// </summary>
        /// <param name="sequence">The sequence.</param>
        /// <param name="reader">The reader.</param>
        /// <param name="writer">The writer.</param>
        /// <param name="toReplace">To replace.</param>
        /// <param name="replaceWith">The replace with.</param>
        private static void ProcessSequence(InstructionSequence sequence, InstructionReader reader, InstructionWriter writer,
        IMethod toReplace, IMethod replaceWith) {
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