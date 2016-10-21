
using System;
using PostSharp.Hosting;
using PostSharp.Sdk.CodeModel;

namespace Starcounter.Weaver {
    /// <summary>
    /// Thin class that extends <see cref="PostSharpLocalHost"/> to support running
    /// inside the PostSharp domain in case the host runs PS in a separate domain.
    /// Currently, we run both the host and PS in the same domain, so we don't need
    /// to pay any special attention to keep serialization under control.
    /// </summary>
    [Serializable]
    public class CodeWeaverInsidePostSharpDomain : PostSharpLocalHost {
        private CodeWeaver codeWeaver;

        public override void Initialize() {
            base.Initialize();
            this.codeWeaver = (CodeWeaver)this.RemoteHost;
        }

        public override ProjectInvocationParameters GetProjectInvocationParameters(ModuleDeclaration module) {
            // The job we need to do here: find out if the module is one we are to
            // process and if so, return the correct parameters to PostSharp on how
            // to process it.

            if (module.AssemblyManifest == null) {
                codeWeaver.Host.WriteDebug("Not considering module {0}: no assembly manifest.", module.Name);
                return null;
            }

            return codeWeaver.GetProjectInvocationParametersForAssemblies(module);
        }
    }
}
