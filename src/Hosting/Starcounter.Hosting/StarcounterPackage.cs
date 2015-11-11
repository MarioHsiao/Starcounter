
using Starcounter.Binding;
using Starcounter.Internal.Metadata;
using System.Diagnostics;

namespace Starcounter.Hosting {

    /// <summary>
    /// The package provided by Starcounter, containing types that are to be
    /// loaded and maintained in every host.
    /// </summary>
    public class StarcounterPackage : Package {
        private StarcounterPackage(TypeDef[] types, Stopwatch watch) : base(types, watch) {}

        /// <summary>
        /// Create a <see cref="StarcounterPackage"/>, governing the right set of
        /// type definitions.
        /// </summary>
        /// <param name="watch">Stop watch for diagnostics</param>
        /// <returns>The package, ready to be processed.</returns>
        public static StarcounterPackage Create(Stopwatch watch) {
            var defs = MetaBinder.Instance.GetDefinitions();
            return new StarcounterPackage(defs, watch);
        }

        protected override void InitTypeSpecifications() {
            // User-level classes are self registering and report in to
            // the installed host manager on first use (via an emitted call
            // in the static class constructor). For system classes, we
            // have to do this by hand.

            var metaBinder = MetaBinder.Instance;
            foreach (var type in metaBinder.GetSpecifications()) {
                HostManager.InitTypeSpecification(type);
            }
        }
    }
}