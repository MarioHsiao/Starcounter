// ***********************************************************************
// <copyright file="ScTransactionScopeTask.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

// PSA 24/9 2012: Not aware of whats been decided about transcation
// scopes, but since they are not part of even the mock, I'm not doing
// anything to try to support them in the weaver.

#if false

using System;
using System.Collections.Generic;
using PostSharp.Reflection;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeModel.Helpers;
using PostSharp.Sdk.CodeWeaver;
using PostSharp.Sdk.Collections;
using PostSharp.Sdk.Extensibility;
using PostSharp.Sdk.Extensibility.Tasks;
using Starcounter;
using IMethod = PostSharp.Sdk.CodeModel.IMethod;
using System.Reflection;

namespace Starcounter.Internal.Weaver {
    /// <summary>
    /// Weaver for the <see cref="TransactionScopeAttribute"/> custom attribute.
    /// This class is principally an <see cref="IAdviceProvider"/>.
    /// </summary>
    public sealed class ScTransactionScopeTask : Task, IAdviceProvider {
        private IType _transactionScopeType;
        private IMethod _transactionScopeConstructor;
        private IMethod _setCompleteMethod;
        private IMethod _setAbortMethod;
        private IMethod _prepareRetryMethod;
        private ITypeSignature _transactionConflictExceptionType;
        private TypeDefDeclaration _transactionAttributeType;
        private TypeDefDeclaration _parallelTransactionAttributeType;
        private IMethod _transactionScopeIsBoundaryMethod;
        private IMethod _unhandledTransactionConflictExceptionConstructor;
        private ITypeSignature _exceptionType;

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override bool Execute() {
            AssemblyRefDeclaration scappAssemblyRef = null;
            ModuleDeclaration module = this.Project.Module;
            StringComparison strComp = StringComparison.InvariantCultureIgnoreCase;
            String afStr;

            // Find the reference to Starcounter.Application.
            foreach (AssemblyRefDeclaration assemblyRef in module.AssemblyRefs) {
                if (String.Equals(assemblyRef.Name, "Starcounter", strComp)) {
                    if (scappAssemblyRef != null) {
                        afStr = "Assembly {0} has more than one reference to Starcounter.dll";
                        throw new AssertionFailedException(String.Format(afStr, module.Name));
                    }
                    scappAssemblyRef = assemblyRef;
                }
            }

            if (scappAssemblyRef != null) {
                // Retrieve and save the TypeDef from Starcounter.Application, since the 
                // annotation cache containing the custom attributes will use a key with 
                // this type (and not the attribute from the real dll, Starcounter).
                _transactionAttributeType
                        = scappAssemblyRef.FindType(typeof(TransactionAttribute).FullName,
                                                    BindingOptions.Default).GetTypeDefinition();
                _parallelTransactionAttributeType
                        = scappAssemblyRef.FindType(typeof(ParallelTransactionAttribute).FullName,
                                                    BindingOptions.Default).GetTypeDefinition();
            }
            return true;
        }

        /// <summary>
        /// Entry point of the task. We just initialize some references.
        /// </summary>
        /// <returns><b>true</b> in case of success.</returns>
        public void InitializeTypesAndMembers() {
            IMethod method;
            MethodBase methodBase;
            ModuleDeclaration module = Project.Module;
            Type iTransConflictType = typeof(ITransactionConflictException);
            Type transConflictType = typeof(TransactionConflictException);
            Type transScopeType = typeof(TransactionScope);
            Type transScopeOptionsType = typeof(TransactionScopeOptions);
            Type unhandledExType = typeof(UnhandledTransactionConflictException);

            _transactionScopeType = (IType)module.Cache.GetType(typeof(TransactionScope));

            methodBase = transScopeType.GetConstructor(new[] { transScopeOptionsType });
            method = module.Cache.GetItem(() => module.FindMethod(methodBase,
                                                                  BindingOptions.Default));
            _transactionScopeConstructor = method;

            methodBase = transScopeType.GetMethod("SetAbort");
            method = module.Cache.GetItem(() => module.FindMethod(methodBase,
                                                                  BindingOptions.Default));
            _setAbortMethod = method;

            methodBase = transScopeType.GetMethod("SetComplete");
            method = module.Cache.GetItem(() => module.FindMethod(methodBase,
                                                                  BindingOptions.Default));
            _setCompleteMethod = method;

            methodBase = transScopeType.GetMethod("PrepareRetry");
            method = module.Cache.GetItem(() => module.FindMethod(methodBase,
                                                                  BindingOptions.Default));
            _prepareRetryMethod = method;

            _exceptionType = module.Cache.GetType(typeof(Exception));
            _transactionConflictExceptionType = module.Cache.GetType(iTransConflictType);

            methodBase = unhandledExType.GetConstructor(new[] { transConflictType });
            method = module.Cache.GetItem(() => module.FindMethod(methodBase,
                                                                  BindingOptions.Default));
            _unhandledTransactionConflictExceptionConstructor = method;

            methodBase = transScopeType.GetProperty("IsTransactionBoundary").GetGetMethod();
            method = module.Cache.GetItem(() => module.FindMethod(methodBase,
                                                                  BindingOptions.Default));
            _transactionScopeIsBoundaryMethod = method;
        }

        /// <summary>
        /// Adds advices to the aspect weaver.
        /// </summary>
        /// <param name="codeWeaver">The aspect weaver.</param>
        public void ProvideAdvices(PostSharp.Sdk.CodeWeaver.Weaver codeWeaver) {
            if (_transactionAttributeType == null) return;

            InitializeTypesAndMembers();

            // Enumerate custom attributes of type TransactionScopeAttribute.
            AnnotationRepositoryTask customAttributeDictionary =
                        AnnotationRepositoryTask.GetTask(this.Project);

            IEnumerator<IAnnotationInstance> customAttributes =
                    customAttributeDictionary.GetAnnotationsOfType(_transactionAttributeType,
                                                                   false);

            while (customAttributes.MoveNext()) {
                HandleCustomAttribute(customAttributes.Current,
                                      codeWeaver,
                                      TransactionScopeOptions.Default);
            }

            customAttributes =
                customAttributeDictionary.GetAnnotationsOfType(_parallelTransactionAttributeType,
                                                               false);
            while (customAttributes.MoveNext()) {
                HandleCustomAttribute(customAttributes.Current,
                                      codeWeaver,
                                      TransactionScopeOptions.RequiresNew);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="attribute"></param>
        /// <param name="codeWeaver"></param>
        /// <param name="options"></param>
        private void HandleCustomAttribute(IAnnotationInstance attribute,
                                           PostSharp.Sdk.CodeWeaver.Weaver codeWeaver,
                                           TransactionScopeOptions options) {
            InnerAdvice innerAdvice;
            MethodData methodData;
            MethodDefDeclaration methodDef;
            OuterAdvice outerAdvice;
            Singleton<MethodDefDeclaration> singleton;

            // Get the method to which the custom attribute is prepared.
            methodDef = (MethodDefDeclaration)attribute.TargetElement;

            // Define a variable in the method to store the TransactionScope object.
            methodData = new MethodData {
                _transactionScopeVariable =
                methodDef.MethodBody.RootInstructionBlock.DefineLocalVariable(
                    this._transactionScopeType, "~ts"),
                _triesVariable =
                methodDef.MethodBody.RootInstructionBlock.DefineLocalVariable(
                    this.Project.Module.Cache.GetIntrinsic(IntrinsicType.Int32),
                    "~tries")
            };
            methodDef.MethodBody.InitLocalVariables = true;
            methodData._retryInstructionSequence = methodDef.MethodBody.CreateInstructionSequence();
            methodData._successSequence = methodDef.MethodBody.CreateInstructionSequence();
            singleton = new Singleton<MethodDefDeclaration>(methodDef);

            // Add advices to the method. We add two, with different priority, to avoid having
            // the TransactionScope initialization in the protected block.
            outerAdvice = new OuterAdvice(this, options, methodData);
            innerAdvice = new InnerAdvice(this, options, methodData);

            codeWeaver.AddMethodLevelAdvice(
                outerAdvice,
                singleton,
                JoinPointKinds.BeforeMethodBody | JoinPointKinds.AfterMethodBodySuccess,
                null
            );

            codeWeaver.AddMethodLevelAdvice(
                innerAdvice,
                singleton,
                JoinPointKinds.AfterMethodBodyException |
                JoinPointKinds.AfterMethodBodySuccess,
                null
            );
        }

        private class MethodData {
            public LocalVariableSymbol _transactionScopeVariable;
            public LocalVariableSymbol _triesVariable;
            public InstructionSequence _retryInstructionSequence;
            public InstructionSequence _successSequence;
        }

        /// <summary>
        /// Advice implementing the <see cref="TransactionAttribute"/>
        /// custom attribute.
        /// </summary>
        private class InnerAdvice : IAdvice {
            private readonly ScTransactionScopeTask _task;
            private readonly TransactionScopeOptions _transactionTransactionScopeOption;
            private readonly MethodData _methodData;

            /// <summary>
            /// Initializes a new <see cref="InnerAdvice"/>.
            /// </summary>
            /// <param name="task">
            /// Reference to the <see cref="ScTransactionScopeTask"/> instance.
            /// </param>
            /// <param name="transactionTransactionScopeOption">
            /// Kind of transaction scope. 
            /// </param>
            /// <param name="methodData"> </param>
            public InnerAdvice(
                ScTransactionScopeTask task,
                TransactionScopeOptions transactionTransactionScopeOption,
                MethodData methodData) {
                _task = task;
                _transactionTransactionScopeOption = transactionTransactionScopeOption;
                _methodData = methodData;
            }

            /// <summary>
            /// Determines if we want to weave the current join point. We always want.
            /// </summary>
            /// <param name="context">Information about the current join point.</param>
            /// <returns>Always <b>true</b>.</returns>
            public Boolean RequiresWeave(WeavingContext context) {
                return true;
            }

            /// <summary>
            /// Inject instructions at the current join point.
            /// </summary>
            /// <param name="context">Information about the current join point.</param>
            /// <param name="block">Block into which we can add our instructions.</param>
            public void Weave(WeavingContext context, InstructionBlock block) {
                // We delegate the processing of each join point to a specific method.
                switch (context.JoinPoint.JoinPointKind) {
                    case JoinPointKinds.AfterMethodBodySuccess:
                        WeaveOnSuccess(context.InstructionWriter, block);
                        break;
                    case JoinPointKinds.AfterMethodBodyException:
                        WeaveOnException(context.InstructionWriter, block);
                        break;
                }
            }

            /// <summary>
            /// Injects instructions after the method, in case of success (no exception).
            /// </summary>
            /// <param name="writer">Some stock <see cref="InstructionWriter"/>.</param>
            /// <param name="block">Block into which we can add our instructions.</param>
            private void WeaveOnSuccess(InstructionWriter writer, InstructionBlock block) {
                InstructionSequence sequence = block.MethodBody.CreateInstructionSequence();
                block.AddInstructionSequence(sequence, NodePosition.Before, null);
                writer.AttachInstructionSequence(sequence);
                writer.EmitSymbolSequencePoint(SymbolSequencePoint.Hidden);
                writer.EmitInstructionLocalVariable(OpCodeNumber.Ldloca,
                                                    _methodData._transactionScopeVariable);
                writer.EmitInstructionMethod(OpCodeNumber.Call, _task._setCompleteMethod);
                writer.EmitBranchingInstruction(OpCodeNumber.Leave,
                                                _methodData._successSequence);
                writer.DetachInstructionSequence();
            }

            /// <summary>
            /// Injects instructions after the method body, on exception.
            /// </summary>
            /// <param name="writer">Some stock <see cref="InstructionWriter"/>.</param>
            /// <param name="block">Block into which we can add our instructions.</param>
            private void WeaveOnException(InstructionWriter writer, InstructionBlock block) {
                /* We have to generate this:
                 * ITransactionConflictExeption ~conflict = e as ITransactionConflictExeption;
                 * if ( ~conflict != null ) [else goto rethrowSequence]
                 * {
                 *    if ( ~ts.PrepareRetry( ~conflict, ~retries ) [else goto wrapSequence]
                 *    {
                 *      goto continue
                 *    }
                 *    [wrapSequence] else if ( ~ts.IsTransactionBoundary ) [else goto rethrowSequence]
                 *    {
                 *     ~ts.SetAbort();
                 *      throw new UnhandledTransactionConflictException(e);
                 *    }
                 * }
                 *
                 * [rethrowSequence]
                 * {
                 *   ~ts.SetAbort();
                 *   rethow;
                 * }
                 *
                 *    [continue]
                 * }
                 *
                 */
                InstructionSequence firstSequence = block.MethodBody.CreateInstructionSequence();
                InstructionSequence wrapSequence = block.MethodBody.CreateInstructionSequence();
                InstructionSequence rethrowSequence = block.MethodBody.CreateInstructionSequence();
                InstructionSequence continueSequence = block.MethodBody.CreateInstructionSequence();

                block.AddInstructionSequence(firstSequence, NodePosition.Before, null);
                block.AddInstructionSequence(wrapSequence, NodePosition.After, null);
                block.AddInstructionSequence(rethrowSequence, NodePosition.After, null);
                block.AddInstructionSequence(continueSequence, NodePosition.After, null);

                // Create a variable to store the exception.
                LocalVariableSymbol exceptionVariable
                    = block.DefineLocalVariable(_task._exceptionType, "~e");
                LocalVariableSymbol conflictVariable
                    = block.DefineLocalVariable(_task._transactionConflictExceptionType, "~conflict");

                // Emit the first sequence.
                writer.AttachInstructionSequence(firstSequence);
                writer.EmitSymbolSequencePoint(SymbolSequencePoint.Hidden);
                writer.EmitInstructionLocalVariable(OpCodeNumber.Stloc, exceptionVariable);
                writer.EmitInstructionLocalVariable(OpCodeNumber.Ldloc, exceptionVariable);
                writer.EmitInstructionType(OpCodeNumber.Isinst,
                                           _task._transactionConflictExceptionType);
                writer.EmitInstructionLocalVariable(OpCodeNumber.Stloc, conflictVariable);
                writer.EmitInstructionLocalVariable(OpCodeNumber.Ldloc, conflictVariable);
                writer.EmitBranchingInstruction(OpCodeNumber.Brfalse, rethrowSequence);
                writer.EmitInstructionLocalVariable(OpCodeNumber.Ldloca,
                                                    _methodData._transactionScopeVariable);
                writer.EmitInstructionLocalVariable(OpCodeNumber.Ldloc, conflictVariable);
                writer.EmitInstructionLocalVariable(OpCodeNumber.Ldloc, _methodData._triesVariable);
                writer.EmitInstructionMethod(OpCodeNumber.Call, _task._prepareRetryMethod);
                writer.EmitBranchingInstruction(OpCodeNumber.Brfalse, wrapSequence);
                writer.EmitBranchingInstruction(OpCodeNumber.Leave,
                                                _methodData._retryInstructionSequence);
                writer.DetachInstructionSequence();

                // Emit the 'wrap' sequence.
                writer.AttachInstructionSequence(wrapSequence);
                writer.EmitInstructionLocalVariable(OpCodeNumber.Ldloca,
                                                    _methodData._transactionScopeVariable);
                writer.EmitInstructionMethod(OpCodeNumber.Call,
                                             _task._transactionScopeIsBoundaryMethod);
                writer.EmitBranchingInstruction(OpCodeNumber.Brfalse, rethrowSequence);
                writer.EmitInstructionLocalVariable(OpCodeNumber.Ldloca,
                                                    _methodData._transactionScopeVariable);
                writer.EmitInstructionMethod(OpCodeNumber.Call, _task._setAbortMethod);
                writer.EmitInstructionLocalVariable(OpCodeNumber.Ldloc, exceptionVariable);
                writer.EmitInstructionMethod(OpCodeNumber.Newobj,
                                             _task._unhandledTransactionConflictExceptionConstructor);
                writer.EmitInstruction(OpCodeNumber.Throw);
                writer.DetachInstructionSequence();

                // Emit the 'rethrow' sequence.
                writer.AttachInstructionSequence(rethrowSequence);
                writer.EmitSymbolSequencePoint(SymbolSequencePoint.Hidden);
                writer.EmitInstructionLocalVariable(OpCodeNumber.Ldloca,
                                                    _methodData._transactionScopeVariable);
                writer.EmitInstructionMethod(OpCodeNumber.Call, _task._setAbortMethod);
                writer.EmitInstruction(OpCodeNumber.Rethrow);
                writer.DetachInstructionSequence();
            }

            /// <summary>
            /// Gets the advice priority.
            /// </summary>
            public int Priority {
                get {
                    return 1;
                }
            }
        }

        private class OuterAdvice : IAdvice {
            private readonly ScTransactionScopeTask _task;
            private readonly TransactionScopeOptions _transactionTransactionScopeOption;
            private readonly MethodData _methodData;

            /// <summary>
            /// Initializes a new <see cref="OuterAdvice"/>.
            /// </summary>
            /// <param name="task">
            /// Reference to the <see cref="ScTransactionScopeTask"/> instance.
            /// </param>
            /// <param name="transactionTransactionScopeOption">
            /// Kind of transaction scope.
            /// </param>
            /// <param name="methodData"></param>
            public OuterAdvice(
                ScTransactionScopeTask task,
                TransactionScopeOptions transactionTransactionScopeOption,
                MethodData methodData) {
                _task = task;
                _transactionTransactionScopeOption = transactionTransactionScopeOption;
                _methodData = methodData;
            }

            /// <summary>
            /// Determines if we want to weave the current join point. We always want.
            /// </summary>
            /// <param name="context">Information about the current join point.</param>
            /// <returns>Always <b>true</b>.</returns>
            public Boolean RequiresWeave(WeavingContext context) {
                return true;
            }

            /// <summary>
            /// Inject instructions at the current join point.
            /// </summary>
            /// <param name="context">Information about the current join point.</param>
            /// <param name="block">Block into which we can add our instructions.</param>
            public void Weave(WeavingContext context, InstructionBlock block) {
                // We delegate the processing of each join point to a specific method.
                switch (context.JoinPoint.JoinPointKind) {
                    case JoinPointKinds.BeforeMethodBody:
                        WeaveOnEntry(context.InstructionWriter, block);
                        break;
                    case JoinPointKinds.AfterMethodBodySuccess:
                        WeaveOnSuccess(context.InstructionWriter, block);
                        break;
                }
            }

            /// <summary>
            /// Injects instructions before the method body.
            /// </summary>
            /// <param name="writer">Some stock <see cref="InstructionWriter"/>.</param>
            /// <param name="block">Block into which we can add our instructions.</param>
            private void WeaveOnEntry(InstructionWriter writer, InstructionBlock block) {
                InstructionSequence sequence = block.MethodBody.CreateInstructionSequence();

                block.AddInstructionSequence(sequence, NodePosition.Before, null);
                writer.AttachInstructionSequence(sequence);
                writer.EmitSymbolSequencePoint(SymbolSequencePoint.Hidden);
                writer.EmitInstructionInt32(OpCodeNumber.Ldc_I4,
                                            (int)_transactionTransactionScopeOption);
                writer.EmitInstructionMethod(OpCodeNumber.Newobj,
                                             _task._transactionScopeConstructor);
                writer.EmitInstructionLocalVariable(OpCodeNumber.Stloc,
                                                    _methodData._transactionScopeVariable);
                writer.EmitInstruction(OpCodeNumber.Ldc_I4_0);
                writer.EmitInstructionLocalVariable(OpCodeNumber.Stloc,
                                                    _methodData._triesVariable);
                writer.DetachInstructionSequence();
                block.AddInstructionSequence(_methodData._retryInstructionSequence,
                                             NodePosition.After,
                                             sequence);
                writer.AttachInstructionSequence(_methodData._retryInstructionSequence);
                writer.EmitInstructionLocalVariable(OpCodeNumber.Ldloc, _methodData._triesVariable);
                writer.EmitInstruction(OpCodeNumber.Ldc_I4_1);
                writer.EmitInstruction(OpCodeNumber.Add);
                writer.EmitInstructionLocalVariable(OpCodeNumber.Stloc, _methodData._triesVariable);
                writer.DetachInstructionSequence();
            }

            /// <summary>
            /// Injects instructions after the method, in case of success (no exception).
            /// </summary>
            /// <param name="writer">Some stock <see cref="InstructionWriter"/>.</param>
            /// <param name="block">Block into which we can add our instructions.</param>
            private void WeaveOnSuccess(InstructionWriter writer, InstructionBlock block) {
                block.AddInstructionSequence(_methodData._successSequence, NodePosition.After, null);
            }

            /// <summary>
            /// Gets the advice priority.
            /// </summary>
            public int Priority {
                get {
                    return 0;
                }
            }
        }
    }
}
#endif