
namespace Starcounter.Internal.Weaver
{
    /// <summary>
    /// Represents a set of transformations a user code assembly
    /// targeting Starcounter can be subject to.
    /// </summary>
    public enum WeaverTransformationKind
    {
        /// <summary>
        /// Represents no transformation.
        /// </summary>
        None,

        /// <summary>
        /// Represents the transformation from original user code
        /// to database access code.
        /// </summary>
        UserCodeToDatabase,

        /// <summary>
        /// Represents the transformation from original user code
        /// to IPC (interprocess) ready code.
        /// </summary>
        UserCodeToIPC,

        /// <summary>
        /// Represents the transformation from IPC (interprocess)
        /// to database access code.
        /// </summary>
        IPCToDatabase
    }
}