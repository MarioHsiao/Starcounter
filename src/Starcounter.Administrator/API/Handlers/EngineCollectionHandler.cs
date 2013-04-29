
using Starcounter.Advanced;
using Starcounter.Server.Rest;
using Starcounter.Server.Rest.Resources;
using Starcounter.Server.PublicModel;
using Starcounter.Administrator.API.Utilities;

namespace Starcounter.Administrator.API.Handlers {    
    using EngineReference = EngineCollection.EnginesApp;
    
    /// <summary>
    /// Excapsulates the admin server functionality acting on the
    /// engine collection resource. 
    /// </summary>
    internal static partial class EngineCollectionHandler {
        /// <summary>
        /// Provides a set of utility methods for working on strongly typed
        /// Engine JSON representations.
        /// </summary>
        internal static class JSON {
            /// <summary>
            /// Populates a strongly typed JSON engine reference representation
            /// from supplied values.
            /// </summary>
            /// <param name="engineRef">The engine reference to populate.</param>
            /// <param name="state">The database whose semantics the JSON
            /// representation represents.</param>
            internal static void PopulateRefRepresentation(EngineReference engineRef, DatabaseInfo state) {
                var name = state.Name;
                var engineUriTemplate = RootHandler.API.Uris.Engine;
                engineRef.Uri = RootHandler.MakeAbsoluteUri(engineUriTemplate, name);
                engineRef.Name = name;
                engineRef.NoDb = state.HasNoDbSwitch();
                engineRef.LogSteps = state.HasLogStepsSwitch();
            }
        }

        /// <summary>
        /// Install handlers for the resource represented by this class and
        /// performs custom setup.
        internal static void Setup() {
            var uri = RootHandler.API.Uris.Engines;
            
            Handle.POST<Request>(uri, OnPOST);
            Handle.GET<Request>(uri, OnGET);
            RootHandler.Register405OnAllUnsupported(uri, new string[] { "GET", "POST" });
        }
    }
}