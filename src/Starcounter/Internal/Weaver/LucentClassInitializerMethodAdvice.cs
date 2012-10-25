// ***********************************************************************
// <copyright file="LucentClassInitializerMethodAdvice.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Reflection;
using Sc.Server.Weaver;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeWeaver;
using PostSharp.Sdk.Collections;
using Starcounter.Internal.Weaver;

namespace Starcounter.LucentObjects {
    /// <summary>
    /// Class LucentClassInitializerMethodAdvice
    /// </summary>
    internal sealed class LucentClassInitializerMethodAdvice : IAdvice {
        /// <summary>
        /// Initializes a new instance of the <see cref="LucentClassInitializerMethodAdvice" /> class.
        /// </summary>
        public LucentClassInitializerMethodAdvice() {
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
            InstructionSequence sequence;

            sequence = context.Method.MethodBody.CreateInstructionSequence();
            block.AddInstructionSequence(sequence, NodePosition.Before, null);

            context.InstructionWriter.AttachInstructionSequence(sequence);

            MethodInfo typeGetType = typeof(Type).GetMethod("GetTypeFromHandle");
            IMethod typeGetMethod = context.Method.Module.FindMethod(typeGetType, BindingOptions.Default);

            ITypeSignature implementationType =
                context.Method.Module.FindType(WeaverNamingConventions.ImplementationDetailsTypeName, BindingOptions.Default);
            context.InstructionWriter.EmitInstructionType(OpCodeNumber.Ldtoken, implementationType);
            context.InstructionWriter.EmitInstructionMethod(OpCodeNumber.Call, typeGetMethod);

            context.InstructionWriter.EmitInstructionType(OpCodeNumber.Ldtoken, context.Method.DeclaringType);
            context.InstructionWriter.EmitInstructionMethod(OpCodeNumber.Call, typeGetMethod);

            var prepareAssembly = context.Method.Module.FindMethod(typeof(LucentObjectsRuntime).GetMethod("InitializeClientAssembly", new[] { typeof(Type), typeof(Type) }), BindingOptions.Default);
            context.InstructionWriter.EmitInstructionMethod(OpCodeNumber.Call, prepareAssembly);

            context.InstructionWriter.DetachInstructionSequence();
        }
    }
}