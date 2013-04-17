
using Starcounter;

namespace Starcounter.Server.Client.MessageTypes {
    /// <summary>
    /// Represents a request to execute user code in a Starcounter
    /// host code process.
    /// </summary>
    /// <remarks>
    /// By design, the request to execute holds no representation
    /// of what host/database it targets. Such information is assumed
    /// to be given in addition to the actual request (for example,
    /// in the form of a database URI when making a REST call).
    /// </remarks>
    partial class ExecRequest : Json {
    }
}