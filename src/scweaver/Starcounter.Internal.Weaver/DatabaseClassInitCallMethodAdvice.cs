// ***********************************************************************
// <copyright file="DatabaseClassInitCallMethodAdvice.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeWeaver;
using PostSharp.Sdk.Collections;
using Starcounter.Hosting;
using System;

namespace Starcounter.Internal.Weaver.BackingCode {

    /// <summary>
    /// Implements a feature enabling database classes to self-register to
    /// the host in which they are loaded upon first usage.
    /// </summary>
    internal sealed class DatabaseClassInitCallMethodAdvice : IAdvice {
        
        /// <summary>
        /// Initializes a new instance of the <see cref="DatabaseClassInitCallMethodAdvice" /> class.
        /// </summary>
        public DatabaseClassInitCallMethodAdvice() {
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public int Priority {
            get {
                return 0;
            }
        }

        /// <summary>
        /// Determines whether we are interested by the current join point. Always yes.
        /// </summary>
        /// <param name="context">Join point context.</param>
        /// <returns>Always <b>true</b></returns>
        public bool RequiresWeave(WeavingContext context) {
            return true;
        }

        /// <summary>
        /// Called when the weaver reaches the join points we are interested in.
        /// </summary>
        /// <param name="context">Weaving context.</param>
        /// <param name="block">Block into which we have to write our instructions.</param>
        public void Weave(WeavingContext context, InstructionBlock block) {
            var sequence = context.Method.MethodBody.CreateInstructionSequence();
            block.AddInstructionSequence(sequence, NodePosition.Before, null);
            context.InstructionWriter.AttachInstructionSequence(sequence);

            // Weave in a call such as the below as the first line of any static
            // constructor
            // [Starcounter.Hosting.]
            //    HostManager.InitTypeSpecification(currentType.TypeSpecification);
            //
            // Prepare for "typeof(xxx)" call.
            var typeGetMethod = context.Method.Module.FindMethod(
                typeof(Type).GetMethod("GetTypeFromHandle"), BindingOptions.Default);
            var specTypeFullName = context.Method.DeclaringType.GetReflectionName() + "+" + TypeSpecification.Name;
            var specificationType = context.Method.Module.FindType(specTypeFullName, BindingOptions.Default);

            // Generate typeof([currentType]).TypeSpefication)
            context.InstructionWriter.EmitInstructionType(OpCodeNumber.Ldtoken, specificationType);
            context.InstructionWriter.EmitInstructionMethod(OpCodeNumber.Call, typeGetMethod);

            // With the type on the stack, call HostManager.InitTypeSpecification
            var initMethod = context.Method.Module.FindMethod(
                typeof(HostManager).GetMethod("InitTypeSpecification",
                new[] { typeof(Type) }), BindingOptions.Default);
            context.InstructionWriter.EmitInstructionMethod(OpCodeNumber.Call, initMethod);

            context.InstructionWriter.DetachInstructionSequence();
        }
    }
}