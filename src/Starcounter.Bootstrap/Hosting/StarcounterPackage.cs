
using Starcounter.Binding;
using Starcounter.Internal;
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

        protected override void UpdateDatabaseSchemaAndRegisterTypes(string fullAppId, TypeDef[] unregisteredTypeDefs, TypeDef[] allTypeDefs) {
            if (unregisteredTypeDefs.Length != 0) {
                Starcounter.SqlProcessor.SqlProcessor.PopulateRuntimeMetadata();

                OnRuntimeMetadataPopulated();
                // Call CLR class clean up
                Starcounter.SqlProcessor.SqlProcessor.CleanClrMetadata(ThreadData.ContextHandle);
                OnCleanClrMetadata();

                // Populate properties and columns .NET metadata
                Db.Scope(() => {
                    for (int i = 0; i < unregisteredTypeDefs.Length; i++) {
                        var metadataTblDef = LookupTable(unregisteredTypeDefs[i].Name);
                        unregisteredTypeDefs[i].PopulatePropertyDef(metadataTblDef, unregisteredTypeDefs);
                    }
                }, true);
                OnPopulateMetadataDefs();
                
                base.UpdateDatabaseSchemaAndRegisterTypes(fullAppId, unregisteredTypeDefs, allTypeDefs);
            }
        }
        
        /// <summary>
        /// Gets the metadata table definition based on the name. This method
        /// differs from the public method in Db class with that it uses internal 
        /// metadata API and never queries RawView for expexted layout.
        /// There can only ever be one layout for the metadata types as well so if more
        /// exists something is broken.
        /// </summary>
        /// <param name="name">The fullname of the table.</param>
        /// <returns>TableDef.</returns>
        protected override TableDef LookupTable(string name) {
            unsafe {
                ulong token = SqlProcessor.SqlProcessor.GetTokenFromName(name);
                if (token != 0) {
                    uint layoutInfoCount = 1;
                    sccoredb.STARI_LAYOUT_INFO layoutInfo;
                    var r = sccoredb.stari_context_get_layout_infos_by_token(
                        ThreadData.ContextHandle, token, &layoutInfoCount, &layoutInfo
                        );
                    if (r == 0) {
                        if (layoutInfoCount == 1)
                            return TableDef.ConstructTableDef(layoutInfo, layoutInfoCount, false);

                        if (layoutInfoCount > 1)
                            throw ErrorCode.ToException(Error.SCERRUNEXPMETADATA, "More than one database-layout exists for metadata type " + name);

                        return null;
                    }
                    throw ErrorCode.ToException(r);
                }
                return null;
            }
        }
        
        private void OnRuntimeMetadataPopulated() { Trace("Runtime meta-data tables were created and populated with initial data."); }
        private void OnCleanClrMetadata() { Trace("CLR view meta-data were deleted on host start."); }
        private void OnPopulateMetadataDefs() { Trace("Properties and columns were populated for the given meta-types."); }
    }
}