
namespace Starcounter {
    
    /// <summary>
    /// Defines the interface of a database entity.
    /// </summary>
    public interface IEntity {
        /// <summary>
        /// Invoked on any database class when it is being deleted.
        /// By implementing this interface and this method, we can
        /// support manual, cascading deletes.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The transaction is locked on the thread during the call to this
        /// method (as long as called from Delete that is). Implementation is
        /// not allowed to change the current transaction or modify the state
        /// of the current transaction (like committing or rolling back the
        /// transaction). If the state of the transaction is changed by another
        /// thread the delete operation will be aborted.
        /// </para>
        /// </remarks>
        void OnDelete();
    }
}