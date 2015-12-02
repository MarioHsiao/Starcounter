
namespace Starcounter.Query {
    /// <summary>
    /// Interface we support for custom applications to support
    /// conversion of <see cref="QueryResultRows<T>"/> instances to
    /// <see cref="Response"/> objects.
    /// </summary>
    public interface IQueryRowsResponse {
        /// <summary>
        /// Creates a <see cref="Response"/> reflecting <paramref name="rows"/>.
        /// </summary>
        /// <typeparam name="T">The type of records</typeparam>
        /// <param name="rows">Rows to create response from.</param>
        /// <returns>The response.</returns>
        Response Respond<T>(QueryResultRows<T> rows);
    }
}