using System.IO.Pipes;
using System.Text;

namespace Starcounter.Bootstrap.ABCIPC.Internal {

    /// <summary>
    /// Encapsulates the pipe protocol we use when ABC IPC extensions
    /// are used and pipes are utilized to receive and send requests and
    /// responses.
    /// </summary>
    internal sealed class PipeProtocol {
        readonly Encoding encoding;

        /// <summary>
        /// Initializes a <see cref="PipeProtocol"/> instance,
        /// specifying the encoding to be used.
        /// </summary>
        /// <param name="encoding"></param>
        internal PipeProtocol(Encoding encoding) {
            this.encoding = encoding;
        }

        /// <summary>
        /// Writes a opaque message to the given pipe, wrapping it up in
        /// the protocol encapsulated by the current instance.
        /// </summary>
        /// <seealso cref="ReadMessage"/>
        /// <param name="pipe">The pipe we are writing the message to.</param>
        /// <param name="message">The message to write.</param>
        internal void WriteMesssage(PipeStream pipe, string message) {
            // We write messages using the given encoding and we
            // prefix each outgoing message with the length.
            var outgoing = encoding.GetBytes(message);
            var count = outgoing.Length;
            if (count > ushort.MaxValue) {
                count = (int)ushort.MaxValue;
            }

            // Length first, then the content
            pipe.WriteByte((byte)(count / 256));
            pipe.WriteByte((byte)(count & 255));
            pipe.Write(outgoing, 0, count);

            // And flush.
            pipe.Flush();
        }

        /// <summary>
        /// Reads a message from the given stream, using the protocol encapsulated
        /// by the current instance.
        /// </summary>
        /// <seealso cref="WriteMessage"/>
        /// <param name="pipe">The pipe to read from.</param>
        /// <returns>The message read.</returns>
        internal string ReadMessage(PipeStream pipe) {
            // We expect the message to be prefixed with the length
            // and encoded compatible with the encoding used for the
            // protocol.
            var count = pipe.ReadByte();
            count *= 256;
            count += pipe.ReadByte();

            var incoming = new byte[count];
            pipe.Read(incoming, 0, count);

            return encoding.GetString(incoming);
        }
    }
}
