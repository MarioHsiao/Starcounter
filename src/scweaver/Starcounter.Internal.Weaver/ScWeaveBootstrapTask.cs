
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PostSharp.Sdk.CodeModel;
using PostSharp.Sdk.Collections;
using PostSharp.Sdk.CodeWeaver;
using PostSharp.Sdk.Extensibility;

namespace Starcounter.Internal.Weaver {

    using Weaver = PostSharp.Sdk.CodeWeaver.Weaver;

    /// <summary>
    /// Implements the weaver that injects the proper calls to allow
    /// shell bootstraping of Starcounter executables.
    /// </summary>
    public class ScWeaveBootstrapTask : Task {

        /// <summary>
        /// Implements the advice we provide the low-level code weaver with
        /// to make the call from the entrypoint to our bootstrapper method.
        /// </summary>
        private class WeaveExeBootstrapperCallAdvice : IAdvice {
            private readonly IMethod bootstrapper;

            internal WeaveExeBootstrapperCallAdvice(IMethod bootstrapper) {
                this.bootstrapper = bootstrapper;
            }

            public int Priority {
                get { return 0; }
            }

            public bool RequiresWeave(WeavingContext context) {
                return true;
            }

            public void Weave(WeavingContext context, InstructionBlock block) {
                var sequence = block.MethodBody.CreateInstructionSequence();
                block.AddInstructionSequence(sequence, NodePosition.Before, null);
                context.InstructionWriter.AttachInstructionSequence(sequence);
                context.InstructionWriter.EmitInstructionMethod(OpCodeNumber.Call, bootstrapper);
                context.InstructionWriter.DetachInstructionSequence();
            }
        }

        /// <summary>
        /// Executes the bootstrapper weaver, effectively injecting the code
        /// into the entrypoint type to support bootstraping.
        /// </summary>
        /// <returns></returns>
        public override bool Execute() {
            var module = this.Project.Module;
            var entrypoint = module.EntryPoint;
            var bootstrapperMethod = module.FindMethod(typeof(Starcounter.CLI.Shell).GetMethod("BootInHost"), BindingOptions.Default);
            var methods = new List<MethodDefDeclaration>(1);
            methods.Add(entrypoint);

            // About referencing and the premises to use when to actually
            // do our thingy on the entrypoint:
            //
            // We should check only direct reference, or else we'll end up
            // iterating a lot of modules and we have to make sure this task
            // is snappy, not loading anything uneccessary.
            //
            // We have a few options here:
            // 1) Increase the likelyhood of a reference by adding an assembly-
            // level attribute - first we make sure we reference Starcounter even
            // if they remove entities and/or Apps-stuff, and secondly, it makes
            // it fast to check this - we know that if the custom attribute is
            // there, we should find the reference to Starcounter.
            // 2) We check for a reference to ANY Starcounter assembly. If we find
            // one, we add one to Starcounter.
            // 3) We look for no references - if we are given an executable, we
            // always emit the calls. Even though this will force us to "assure"
            // the reference.
            // I strongly vote for [1].
            // The current implementation checks no reference at all, just produces
            // the call.
            //
            // TODO:
            // var assemblyReferenceToStarcounter = ScAnalysisTask.FindStarcounterAssemblyReference(module);
            
            // Create a weaver, add our advice and execute the weaver.
            var w = new Weaver(this.Project);
            w.AddMethodLevelAdvice(new WeaveExeBootstrapperCallAdvice(bootstrapperMethod), methods, JoinPointKinds.BeforeMethodBody, null);
            w.Weave();

            return true;
        }
    }
}
