
namespace Starcounter.Internal {

    /// <summary>
    /// Allow code snippets in the form of response converters to take part in
    /// the servers provision of responses based on POCO objects returned from
    /// user handlers. Certain extension points exist that enable instances of
    /// this interface to hook into the request pipeline.
    /// </summary>
    public interface IResponseConverter {

        /// <summary>
        /// Converts the given object, returned from a handler, to a representation
        /// of the given <paramref name="type"/>.
        /// </summary>
        /// <param name="request">The request that invoked handler returning the
        /// resource that are now to be converted.</param>
        /// <param name="resource">The resource to convert.</param>
        /// <param name="type">The preferred MIME type, as dictated by the incoming
        /// request.</param>
        /// <param name="resultingMimetype">The MIME type of the content actually
        /// returned by the implemented method.</param>
        /// <returns>A byte array with the content produced.</returns>
        byte[] Convert(Request request, IResource resource, MimeType type, out MimeType resultingMimetype);
    }
}