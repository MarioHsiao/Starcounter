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
        public virtual dynamic First {
            get {
                // The compiler does not allow us to declare this abstract although it is
                throw new NotImplementedException();
            }
        }

        public virtual IEnumerator GetEnumerator() {
            // The compiler does not allow us to declare this abstract although it is
            throw new NotImplementedException();
        }
    }


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
    public abstract class Rows<T> : Rows, IEnumerable<T> {
        public new T First {
            get {
                // The compiler does not allow us to declare this abstract although it is
                throw new NotImplementedException();
            }
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator() {
            // The compiler does not allow us to declare this abstract although it is
            return (IEnumerator<T>)GetEnumerator();
        }
    }
}
