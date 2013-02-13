
using Starcounter.Advanced;
using System;
namespace Starcounter {

    public class NullData : IBindable {
        public UInt64 UniqueID { get { return 0; } }
    }

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
        public Transaction Transaction {
            get {
                if (_transaction != null)
                    return _transaction;

                App parent = GetNearestAppParent();
                if (parent != null)
                    return parent.Transaction;

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
