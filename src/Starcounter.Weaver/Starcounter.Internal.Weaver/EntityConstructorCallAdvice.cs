// ***********************************************************************
// <copyright file="EntityConstructorCallAdvice.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Collections.Generic;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeWeaver;
using PostSharp.Sdk.Collections;
using Starcounter;
using IMethod = PostSharp.Sdk.CodeModel.IMethod;

namespace Starcounter.Internal.Weaver {
    /// <summary>
    /// Advice applied to constructor of entity classes. We replace calls to original constructors
    /// to calls to constructors that have been enhanced.
    /// </summary>
    internal sealed class EntityConstructorCallAdvice : IMethodLevelAdvice {
        /// <summary>
        /// The redirections
        /// </summary>
        private readonly Dictionary<MetadataDeclaration, IMethod> redirections =
            new Dictionary<MetadataDeclaration, IMethod>();

        /// <summary>
        /// The target constructors
        /// </summary>
        private readonly List<MethodDefDeclaration> targetConstructors = new List<MethodDefDeclaration>();

        /// <summary>
        /// Initialize a new <see cref="EntityConstructorCallAdvice" />.
        /// There should be one instance per type.
        /// </summary>
        public EntityConstructorCallAdvice() {
        }


        /// <summary>
        /// Adds a method call redirection to the advice, i.e. informs that the advice
        /// should replace calls of one method by another.
        /// </summary>
        /// <param name="originalConstructor">Method whose called have to be replaced.</param>
        /// <param name="enhancedConstructor">Method by which calls of <paramref name="originalConstructor" />
        /// have to be replaced.</param>
        /// <remarks>The signature of <paramref name="enhancedConstructor" /> should be exactly identic to
        /// the one of <paramref name="originalConstructor" />, but there should be an additional
        /// parameter at the end.</remarks>
        public void AddRedirection(IMethod originalConstructor, IMethod enhancedConstructor) {
            this.redirections.Add((MetadataDeclaration)originalConstructor, enhancedConstructor);
        }

        /// <summary>
        /// Adds a method that should be modified, i.e. in which constructor calls have
        /// to be replaced.
        /// </summary>
        /// <param name="wovenConstructor">The method that should be woven.</param>
        public void AddWovenConstructor(MethodDefDeclaration wovenConstructor) {
            this.targetConstructors.Add(wovenConstructor);
        }

        #region Implementation of IMethodLevelAdvice

        /// <summary>
        /// Gets the priority of this advice. We can return an aribitrary value since
        /// the current advice is the only one on this join point.
        /// </summary>
        /// <value>The priority.</value>
        public int Priority {
            get {
                return 0;
            }
        }

        /// <summary>
        /// Gets the set of methods to which this advice applies. We return a
        /// collection that should have been filled with all constructors of the
        /// type.
        /// </summary>
        /// <value>The target methods.</value>
        public IEnumerable<MethodDefDeclaration> TargetMethods {
            get {
                return this.targetConstructors;
            }
        }

        /// <summary>
        /// Gets the set of operands to which this advice applies, that is,
        /// which are the methods whose calls have to be changed.
        /// </summary>
        /// <value>The operands.</value>
        public IEnumerable<MetadataDeclaration> Operands {
            get {
                return this.redirections.Keys;
            }
        }

        /// <summary>
        /// Gets the kinds of join points we are interested in.
        /// </summary>
        /// <value>The join point kinds.</value>
        public JoinPointKinds JoinPointKinds {
            get {
                return JoinPointKinds.InsteadOfCall;
            }
        }

        /// <summary>
        /// Determines whether we are interested by a given join point.
        /// </summary>
        /// <param name="context">Join point context.</param>
        /// <returns>Always <b>true</b>.</returns>
        public bool RequiresWeave(WeavingContext context) {
            return true;
        }

        /// <summary>
        /// Called by the weaver so that we can inject instructions instead
        /// of the call to the original constructors.
        /// </summary>
        /// <param name="context">Join point context.</param>
        /// <param name="block">Block in which we can add our instructions.</param>
        public void Weave(WeavingContext context, InstructionBlock block) {
            IMethod method = context.JoinPoint.Instruction.MethodOperand;
            IMethod replacement = this.redirections[(MetadataDeclaration)method];
            ScTransformTrace.Instance.WriteLine("In {{{0}}}, replacing a call to {{{1}}} by {{{2}}}",
                                                context.Method, method, replacement);
            InstructionSequence sequence = block.MethodBody.CreateInstructionSequence();
            block.AddInstructionSequence(sequence, NodePosition.After, null);
            context.InstructionWriter.AttachInstructionSequence(sequence);
            context.InstructionWriter.EmitInstructionInt16(OpCodeNumber.Ldarg, (short)(context.Method.Parameters.Count - 2));
            context.InstructionWriter.EmitInstructionInt16(OpCodeNumber.Ldarg, (short)(context.Method.Parameters.Count - 1));
            context.InstructionWriter.EmitInstruction(OpCodeNumber.Ldnull);
            context.InstructionWriter.EmitInstructionMethod(OpCodeNumber.Call, replacement);
            context.InstructionWriter.DetachInstructionSequence();
        }

        #endregion
    }
}