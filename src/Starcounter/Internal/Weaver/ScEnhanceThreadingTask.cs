
using System;
using System.Threading;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.Collections;
using PostSharp.Sdk.Extensibility;
using PostSharp.Sdk.Extensibility.Tasks;

namespace Starcounter.Internal.Weaver {
    public sealed class ScEnhanceThreadingTask : Task {
        public override Boolean Execute() {
            ScAnalysisTask analysisTask;
            ModuleDeclaration module;
            WeaverTransformationKind transformation;

            // Consult the weaver transformation kind established by the preceeding
            // analysis task to see if we need to execute this task.

            analysisTask = ScAnalysisTask.GetTask(this.Project);
            transformation = analysisTask.GetTransformationKind();

            if (!WeaverUtilities.IsTargetingDatabase(transformation)) {
                // The weaver of the current assembly/module runs with another
                // target than the database. We don't need to do any transformation
                // of threading calls.

                return true;
            }

            // Execute the logic of this task.

            module = Project.Module;
            Thread_Priority(module);
            Thread_SleepA(module);
            Thread_SleepB(module);
            return true;
        }

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
                                                         typeof(InterceptThread)
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
                typeof(InterceptThread)
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
                typeof(InterceptThread)
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

        private static Set<MethodDefDeclaration> FindAffectedMethods(MethodRefDeclaration methodRef) {
            Set<MethodDefDeclaration> affectedMethods = new Set<MethodDefDeclaration>(64);
            foreach (MethodDefDeclaration methodDef in IndexUsagesTask.GetUsedBy(methodRef)) {
                affectedMethods.AddIfAbsent(methodDef);
            }
            return affectedMethods;
        }

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