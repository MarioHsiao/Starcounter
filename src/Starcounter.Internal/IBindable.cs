
using System;
namespace Starcounter.Advanced {

    /// <summary>
    /// Used to provide a way to store a reference to a bindable object (i.e. Entity)
    /// </summary>
    public interface IBindable {
        UInt64 UniqueID { get; }
    }
}
