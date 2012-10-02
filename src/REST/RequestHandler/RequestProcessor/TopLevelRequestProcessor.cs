using System.Collections.Generic;

namespace Starcounter.Internal.Uri {

    /// <summary>
    /// Used as a base class for the code generated request processor responsible for registring and handling
    /// the users rest style handlers.
    /// By matching and parsing the verb and URI, the correct user handler delegate will be called.
    /// <summary"/>
    /// <remarks>
    /// The top level request processor also keeps a dictionary of URI templates such that
    /// new handlers can be registred by the user.
    /// </remarks>
    public abstract class TopLevelRequestProcessor : RequestProcessor {

        /// <summary>
        /// The user registred rest style handlers (verb+uri and hanlder code). 
        /// </summary>
        public Dictionary<string, SingleRequestProcessorBase> Registrations = new Dictionary<string,SingleRequestProcessorBase>();

        /// <summary>
        /// Register a handler delegate for a given verb and URI
        /// </summary>
        /// <param name="verbAndUri">For example "GET /players/123</param>
        /// <param name="code">A boxed delegate to the users handler code</param>
        public void Register(string verbAndUri, object code) {
            Registrations[verbAndUri].CodeAsObj = code;
        }
    }

}