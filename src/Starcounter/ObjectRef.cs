
using Starcounter.Internal;

namespace Starcounter {
    
    /// <summary>
    /// Struct ObjectRef
    /// </summary>
    public struct ObjectRef {
        /// <summary>
        /// The object ID
        /// </summary>
        public ulong ObjectID;

        /// <summary>
        /// The ETI. Obsolete. Use Address instead.
        /// </summary>
        public ulong ETI;

        /// <summary>
        /// The address of the object.
        /// </summary>
        public ulong Address {
            get { return ETI; }
            set { ETI = value; }
        }

        /// <summary>
        /// Initializes the current instance to represent
        /// an invalid reference (i.e. one not pointing to
        /// any object in the kernel).
        /// </summary>
        internal void InitInvalid() {
            ObjectID = sccoredb.MDBIT_OBJECTID;
            ETI = 0;
        }
    }
}
