using Starcounter.Advanced;
using Starcounter.Internal;
using System;
namespace Starcounter
{
    /// <summary>
    /// Global configuration settings for the application.
    /// </summary>
    public static class NewConfig
    {
        /// <summary>
        /// Is this application a Starcounter Administrator?
        /// </summary>
        public static Boolean IsAdministratorApp = false;

        /// <summary>
        /// Default configuration parameters.
        /// </summary>
        public static class Default
        {
            /// <summary>
            /// User HTTP port.
            /// </summary>
            public static UInt16 UserHttpPort = StarcounterConstants.NetworkPorts.DefaultPersonalServerUserHttpPort;

            /// <summary>
            /// System HTTP port.
            /// </summary>
            public static UInt16 SystemHttpPort = StarcounterConstants.NetworkPorts.DefaultPersonalServerSystemHttpPort;
        }
    }
}
