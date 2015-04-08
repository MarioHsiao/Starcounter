using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Internal {
    /// <summary>
    /// Serves as a implicit base type for database classes that have
    /// no database class as their parent, nor define themselves by
    /// inheriting <see cref="Entity"/>.
    /// </summary>
    /// <remarks>
    /// This type is not really needed, but the code host loader and
    /// the metadata module doesn't currently function properly without
    /// it. It is maintained by Starcounter, and should be used only
    /// to make sure a correct binding; it will never be a real base
    /// class (as in a .NET base class) for any user defined database
    /// class.
    /// </remarks>
    public abstract class ImplicitEntity {

        #region Workaround for #2061 and #2428
        public Entity __ScImplicitType {
            get { return null; }
            set { throw new InvalidOperationException(); }
        }

        public Entity __ScImplicitInherits {
            get { return null; }
            set { throw new InvalidOperationException(); }
        }

        public string __ScImplicitName {
            get { return null; }
            set { throw new InvalidOperationException(); }
        }

        public bool __ScImplicitIsType {
            get { return false; }
            set { throw new InvalidOperationException(); }
        }

        public int __ScImplicitInstantiates {
            get { return sccoredb.STAR_INVALID_TABLE_ID; }
            set { throw new InvalidOperationException(); }
        }

        #endregion
    }
}
