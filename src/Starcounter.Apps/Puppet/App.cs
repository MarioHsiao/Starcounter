
using Starcounter.Advanced;
using System;
namespace Starcounter {

    /// <summary>
    /// 
    /// </summary>
    public class NullData : IBindable {
        /// <summary>
        /// 
        /// </summary>
        public UInt64 UniqueID { get { return 0; } }
    }

    /// <summary>
    /// See App with generics 
    /// </summary>
    public class App : App<NullData> {


        /// <summary>
        /// Performs an implicit conversion from <see cref="System.String" /> to <see cref="App" />.
        /// </summary>
        /// <param name="str">The STR.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator App(string str) {
            return new App() { Media = str };
        }
    }

    /// <summary>
    /// An App is a live view model object controlled by your C# application code.
    /// It is mirrored between the server and the client in an MVVM or MVC application.
    /// App objects can be used to drive MVVM views or other such model driven clients.
    /// </summary>
    /// <remarks>An App object is modelled to simulate a JSON object. The App object can have properties such as strings, booleans
    /// arrays and other app objects. In this way you can build JSON like trees. These trees are then bound to your GUI.
    /// Whenever you change properties in the tree, such as changing values or adding and removing elements in the arrays,
    /// the UI gets updated. Likewise, when the user clicks or writes text inside your UI, the App view model tree gets updated.
    /// This is a very efficient way to connect a user interface to your application logic and will result in clean, simple
    /// and easy to understand and maintain code. This model view controller pattern (MVC) pattern is sometimes referred to as
    /// MVVM (model view view-model) or MDV (model driven views).    An App is a view model object (the VM in the MVVM pattern) and the controller of said view model (the C in the MVC pattern).
    /// If your JSON object adds an event (like a command when a button is clicked),
    /// your C# code will be called. If you make a property editable, changes by the user will change App object (and an event will be triggered
    /// in case you which to validate the change).
    /// An App is a view model object (the VM in the MVVM pattern) and the controller of said view model (the C in the MVC pattern).
    /// If your JSON object adds an event (like a command when a button is clicked),
    /// your C# code will be called. If you make a property editable, changes by the user will change App object (and an event will be triggered
    /// in case you which to validate the change).
    /// </remarks>
    public class App<T> : Obj<T> where T : IBindable {

        /// <summary>
        /// Performs an implicit conversion from <see cref="System.String" /> to <see cref="App" />.
        /// </summary>
        /// <param name="str">The STR.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator App<T>(string str) {
            return new App<T>() { Media = str };
        }

        /// <summary>
        /// Logs the change such that it can be mirrored to the client
        /// </summary>
        /// <param name="property">The property that changed</param>
        protected override void HasChanged(Property property) {
            ChangeLog.UpdateValue(this, property);
        }

        /// <summary>
        /// Commits this instance.
        /// </summary>
        public virtual void Commit() {
            if (_transaction != null) {
                _transaction.Commit();
            }
        }

        /// <summary>
        /// Aborts this instance.
        /// </summary>
        public virtual void Abort() {
            if (_transaction != null) {
                _transaction.Rollback();
            }
        }


        /// <summary>
        /// 
        /// </summary>
        private Transaction _transaction;

        internal override void InternalSetData(IBindable data) {
            if (Transaction == null) {
                Transaction = Transaction._current;
            }
            base.InternalSetData(data);
        }


        /// <summary>
        /// Gets the closest transaction for this app looking up in the tree.
        /// Sets this transaction.
        /// </summary>
        public new Transaction Transaction {
            get {
                if (_transaction != null)
                    return _transaction;

                Obj parent = GetNearestObjParent();
                if (parent != null)
                    return ((App)parent).Transaction;

                return null;
            }
            set {
                if (_transaction != null) {
                    throw new Exception("An transaction is already set for this App. Changing transaction is not allowed.");
                }
                _transaction = value;
            }
        }

        /// <summary>
        /// Returns the transaction that is set on this app. Does NOT
        /// look in parents.
        /// </summary>
        internal Transaction TransactionOnThisApp {
            get { return _transaction; }
        }


    }
}
