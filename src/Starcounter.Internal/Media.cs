namespace Starcounter.Advanced {

    /// <summary>
    /// Struct Media
    /// </summary>
    public struct Media {
        /// <summary>
        /// The content
        /// </summary>
        public Response Content;

        /// <summary>
        /// Performs an implicit conversion from <see cref="System.String" /> to <see cref="Media" />.
        /// </summary>
        /// <param name="str">The STR.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator Media(string str) {
            return new Media() { Content = new Response(str) };
        }
        /// <summary>
        /// Performs an implicit conversion from <see cref="Response" /> to <see cref="Media" />.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator Media(Response content) {
            return new Media() { Content = content };
        }
    }
}