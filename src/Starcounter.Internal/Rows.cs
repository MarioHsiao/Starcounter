using System;
using System.Collections;
using System.Collections.Generic;

namespace Starcounter {
    /// <summary>
    /// 
    /// </summary>
    /// <remarks>
    /// We use an abstract class instead of an interface to allow implicit casts when setting
    /// array properties in Objs (Messages and Puppets) to SQL results.
    /// 
    /// This allows the programmer to do this:
    /// <example>
    /// Message myOrder = new OrderMsg();
    /// myOrder.Items = SQL("SELECT I FROM OrderItem I"); // Implicit conversion
    /// </example>
    /// 
    /// http://stackoverflow.com/questions/143485/implicit-operator-using-interfaces
    /// </remarks>
    public abstract class Rows : IEnumerable {
        protected abstract IEnumerator InternalGetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() {
            return InternalGetEnumerator();
        }
    }

    public abstract class Rows<T> : Rows, IEnumerable, IEnumerable<T> {
        public abstract T First {
            get;
        }

        public abstract IRowEnumerator<T> GetEnumerator();

        protected override IEnumerator InternalGetEnumerator() {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() {
            return GetEnumerator();
        }
    }
}
