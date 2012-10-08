
using PostSharp.Reflection;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.CodeWeaver;
using PostSharp.Sdk.Collections;
using Starcounter;
using AssertionFailedException = PostSharp.Sdk.AssertionFailedException;
using IMethod = PostSharp.Sdk.CodeModel.IMethod;
using System.Threading;

namespace Starcounter.Internal.Weaver {

    /// <summary>
    /// Tracks the number of active static constructors of database classes.
    /// </summary>
    /// <remarks>
    /// The purpose of this object is to forbid some operations during static constructors.
    /// </remarks>
    public static class StaticConstructorTracker {
        private static int activeStaticConstructorCount = 0;

        /// <summary>
        /// Method called by woven user code (call added by the weaver)
        /// when entering the static constructor of a database class.
        /// </summary>
        public static void IncrementActiveStaticConstructors() {
            Interlocked.Increment(ref activeStaticConstructorCount);
        }

        /// <summary>
        /// Method called by woven user code (call added by the weaver)
        /// when leaving the static constructor of a database class.
        /// </summary>
        public static void DecrementActiveStaticConstructors() {
            Interlocked.Decrement(ref activeStaticConstructorCount);
        }

        /// <summary>
        /// Determines whether there is an active static constructor.
        /// </summary>
        public static bool HasActiveStaticConstructor {
            get {
                return activeStaticConstructorCount > 0;
            }
        }
    }

    /// <summary>
    /// Advice applied to static constructor of database classes. Enclose them with calls
    /// to <see cref="StaticConstructorTracker.IncrementActiveStaticConstructors"/>
    /// and <see cref="StaticConstructorTracker.DecrementActiveStaticConstructors"/>.
    /// </summary>
    /// <remarks>
    /// We use a single instance of this advice for all static constructors of a single module,
    /// since there is no specific state.
    /// </remarks>
    internal sealed class StaticConstructorAdvice : IAdvice {
        private readonly IMethod incrementMethod;
        private readonly IMethod decrementMethod;

        /// <summary>
        /// Initializes a new <see cref="StaticConstructorAdvice"/>
        /// for a given module.
        /// </summary>
        /// <param name="module">Module on which this advice will be applied.</param>
        public StaticConstructorAdvice(ModuleDeclaration module) {
            this.incrementMethod =
                module.FindMethod(typeof(StaticConstructorTracker).GetMethod("IncrementActiveStaticConstructors"),
                                  BindingOptions.Default);
            this.decrementMethod =
                module.FindMethod(typeof(StaticConstructorTracker).GetMethod("DecrementActiveStaticConstructors"),
                                  BindingOptions.Default);
        }

        /// <summary>
        /// Gets the advice priority. We can return anything since we have only once.
        /// </summary>
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
        /// We should inject calls to accessors instead of field accesses.
        /// </summary>
        /// <param name="context">Weaving context.</param>
        /// <param name="block">Block into which we have to write our instructions.</param>
        public void Weave(WeavingContext context, InstructionBlock block) {
            InstructionSequence sequence = context.Method.MethodBody.CreateInstructionSequence();
            block.AddInstructionSequence(sequence, NodePosition.After, null);
            context.InstructionWriter.AttachInstructionSequence(sequence);
            context.InstructionWriter.EmitSymbolSequencePoint(SymbolSequencePoint.Hidden);
            switch (context.JoinPoint.JoinPointKind) {
                case JoinPointKinds.BeforeMethodBody:
                    ScTransformTrace.Instance.WriteLine("Decorating the static constructor {{{0}}} with a call to {{{1}}}.",
                                                        context.Method, this.incrementMethod);
                    context.InstructionWriter.EmitInstructionMethod(OpCodeNumber.Call, this.incrementMethod);
                    break;
                case JoinPointKinds.AfterMethodBodyAlways:
                    ScTransformTrace.Instance.WriteLine("Decorating the static constructor {{{0}}} with a call to {{{1}}}.",
                                                        context.Method, this.decrementMethod);
                    context.InstructionWriter.EmitInstructionMethod(OpCodeNumber.Call, this.decrementMethod);
                    break;
                default:
                    throw new AssertionFailedException(string.Format("Unexpected join point kind: {0}",
                                                                     context.JoinPoint.JoinPointKind));
            }
            context.InstructionWriter.DetachInstructionSequence();
        }
    }
}