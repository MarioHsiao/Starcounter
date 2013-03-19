
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Binding {
    
    /// <summary>
    /// Expose a low-level state related interface to managed database
    /// objects. A proxy is a view with a small additional API to allow
    /// modification of the underlying state.
    /// </summary>
    public interface IObjectProxy : IObjectView {
        /// <summary>
        /// Gets the underlying "this handle".
        /// </summary>
        /// <remarks>
        /// To assign the handle, use <see cref="Bind"/>.
        /// </remarks>
        ObjectRef ThisHandle { get; }

        /// <summary>
        /// Binds the underlying object to the given address, object id
        /// and type binding.
        /// </summary>
        /// <param name="addr">The objects (opaque) address.</param>
        /// <param name="oid">The objects unique identity.</param>
        /// <param name="typeBinding">The <see cref="TypeBinding"/> to
        /// assign the underlying object.</param>
        void Bind(ulong addr, ulong oid, TypeBinding typeBinding);
    }
}