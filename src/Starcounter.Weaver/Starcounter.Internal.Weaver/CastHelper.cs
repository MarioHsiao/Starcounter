// ***********************************************************************
// <copyright file="CastHelper.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeModel.Binding;
using PostSharp.Sdk.CodeModel.TypeSignatures;
using PostSharp.Sdk.Collections;

namespace Starcounter.Internal.Weaver {
    /// <summary>
    /// Provides the method <see cref="EmitCast" />, that emits instruction that change the
    /// type of a value, even using non-trivial transformations.
    /// </summary>
    internal class CastHelper {
        /// <summary>
        /// The module
        /// </summary>
        private ModuleDeclaration module;
        /// <summary>
        /// The nullable type
        /// </summary>
        private INamedType nullableType;
        /// <summary>
        /// The nullable has value method
        /// </summary>
        private IMethod nullableHasValueMethod;
        /// <summary>
        /// The nullable get value method
        /// </summary>
        private IMethod nullableGetValueMethod;
        /// <summary>
        /// The nullable non null constructor
        /// </summary>
        private IMethod nullableNonNullConstructor;

        /// <summary>
        /// Initializes a new <see cref="CastHelper" />.
        /// </summary>
        /// <param name="module">Module into which the instructions will be emitted.</param>
        public CastHelper(ModuleDeclaration module) {
            this.module = module;
            this.nullableType = (INamedType)
                                this.module.Cache.GetType(typeof(Nullable<>), BindingOptions.RequireGenericDefinition);
            this.nullableHasValueMethod =
                this.module.FindMethod(typeof(Nullable<>).GetProperty("HasValue").GetGetMethod(),
                                       BindingOptions.RequireGenericDefinition);
            this.nullableGetValueMethod =
                this.module.FindMethod(typeof(Nullable<>).GetProperty("Value").GetGetMethod(),
                                       BindingOptions.RequireGenericDefinition);
            this.nullableNonNullConstructor =
                this.module.FindMethod(
                    typeof(Nullable<>).GetConstructor(new Type[] { typeof(Nullable<>).GetGenericArguments()[0] }),
                    BindingOptions.RequireGenericDefinition);
        }

        /// <summary>
        /// Finds a <see cref="MethodRefDeclaration" /> in a <see cref="TypeSpecDeclaration" />.
        /// </summary>
        /// <param name="typeSpec">The <see cref="TypeSpecDeclaration" /> in which the method should be found
        /// (or added).</param>
        /// <param name="method">The method to be located.</param>
        /// <returns>The <see cref="MethodRefDeclaration" /> corresponding to
        /// <paramref name="method" /> in <paramref name="typeSpec" />.</returns>
        private static MethodRefDeclaration FindMethodRef(TypeSpecDeclaration typeSpec, IMethod method) {
            return
                (MethodRefDeclaration)
                typeSpec.MethodRefs.GetMethod(method.Name, method, BindingOptions.RequireGenericInstance);
        }

        /// <summary>
        /// Emits instructions that change the type of the value on the stack into another given type.
        /// </summary>
        /// <param name="sourceType">Type of the value on the stack.</param>
        /// <param name="targetType">Type into which the value on the stack should be converted.</param>
        /// <param name="writer">Writer into which instructions have to be written.</param>
        /// <param name="sequence">Current sequence. If this method needs to emit branching instructions,
        /// this parameter will be updated to the sequence where the next instructions have to be written.</param>
        /// <returns><b>true</b> if a cast was found and instructions were successfully emitted,
        /// <b>false</b> if no cast is known to convert <paramref name="sourceType" />
        /// into <paramref name="targetType" />.</returns>
        public bool EmitCast(ITypeSignature sourceType, ITypeSignature targetType, InstructionWriter writer,
                             ref InstructionSequence sequence) {
            if (sourceType.IsAssignableTo(targetType)) {
                // No conversion necessary.
                return true;
            } else if (targetType.IsAssignableTo(sourceType)) {
                // A simple cast is enough,
                writer.EmitInstructionType(OpCodeNumber.Castclass, targetType);
                return true;
            } else {
                GenericTypeInstanceTypeSignature genericTypeInstanceSourceType =
                    sourceType as GenericTypeInstanceTypeSignature;
                GenericTypeInstanceTypeSignature genericTypeInstanceTargetType =
                    targetType as GenericTypeInstanceTypeSignature;
                if (genericTypeInstanceTargetType != null && genericTypeInstanceSourceType != null) {
                    ITypeSignature sourceUnderlyingType = genericTypeInstanceSourceType.GenericArguments[0];
                    ITypeSignature targetUnderlyingType = genericTypeInstanceTargetType.GenericArguments[0];
                    if (genericTypeInstanceSourceType.GenericDefinition.Name == "System.Nullable`1" &&
                        genericTypeInstanceTargetType.GenericDefinition.Name == "System.Nullable`1" &&
                        targetUnderlyingType.IsAssignableTo(sourceUnderlyingType)
                       ) {
                        // This cast converts for instance int? <--> enum? or int? <--> bool?.
                        // We should generate: Nullable<ConsoleColor> b = a.HasValue ? new ConsoleColor?(a.Value) : new ConsoleColor?();
                        // Get the method specs.
                        TypeSpecDeclaration sourceNullableTypeSpec = this.module.TypeSpecs.GetBySignature(
                                                                         new GenericTypeInstanceTypeSignature(this.nullableType,
                                                                                 new ITypeSignature[] { sourceUnderlyingType }),
                                                                         true);
                        TypeSpecDeclaration targetNullableTypeSpec = this.module.TypeSpecs.GetBySignature(
                                                                         new GenericTypeInstanceTypeSignature(this.nullableType,
                                                                                 new ITypeSignature[] { targetUnderlyingType }),
                                                                         true);
                        IMethod nullableHasValueMethodRef =
                            FindMethodRef(sourceNullableTypeSpec, nullableHasValueMethod);
                        IMethod nullableGetValueMethodRef =
                            FindMethodRef(sourceNullableTypeSpec, nullableGetValueMethod);
                        IMethod nullableNonNullConstructorRef =
                            FindMethodRef(targetNullableTypeSpec, nullableNonNullConstructor);
                        // Create the necessary sequences.
                        InstructionSequence hasValueBranch = writer.MethodBody.CreateInstructionSequence();
                        InstructionSequence continueBranch = writer.MethodBody.CreateInstructionSequence();
                        sequence.ParentInstructionBlock.AddInstructionSequence(hasValueBranch, NodePosition.After,
                                                                               sequence);
                        sequence.ParentInstructionBlock.AddInstructionSequence(continueBranch, NodePosition.After,
                                                                               hasValueBranch);
                        // Define a local variable used to get a default value of the Nullable object.
                        LocalVariableSymbol nullValueSymbol =
                            sequence.ParentInstructionBlock.DefineLocalVariable(targetNullableTypeSpec, "~null~{0}");
                        // This local variable is to store the value to be casted, because we need to take the address.
                        LocalVariableSymbol valueSymbol =
                            sequence.ParentInstructionBlock.DefineLocalVariable(sourceNullableTypeSpec, "~value~{0}");
                        writer.EmitInstructionLocalVariable(OpCodeNumber.Stloc, valueSymbol);
                        writer.EmitInstructionLocalVariable(OpCodeNumber.Ldloca, valueSymbol);
                        writer.EmitInstructionMethod(OpCodeNumber.Call, nullableHasValueMethodRef);
                        writer.EmitBranchingInstruction(OpCodeNumber.Brtrue, hasValueBranch);
                        writer.EmitInstructionLocalVariable(OpCodeNumber.Ldloca, nullValueSymbol);
                        writer.EmitInstructionType(OpCodeNumber.Initobj, targetNullableTypeSpec);
                        writer.EmitInstructionLocalVariable(OpCodeNumber.Ldloc, nullValueSymbol);
                        writer.EmitBranchingInstruction(OpCodeNumber.Br, continueBranch);
                        writer.DetachInstructionSequence();
                        writer.AttachInstructionSequence(hasValueBranch);
                        writer.EmitInstructionLocalVariable(OpCodeNumber.Ldloca, valueSymbol);
                        writer.EmitInstructionMethod(OpCodeNumber.Call, nullableGetValueMethodRef);
                        writer.EmitInstructionMethod(OpCodeNumber.Newobj, nullableNonNullConstructorRef);
                        writer.DetachInstructionSequence();
                        writer.AttachInstructionSequence(continueBranch);
                        sequence = continueBranch;
                        return true;
                    }
                }
            }
            return false;
        }
    }
}