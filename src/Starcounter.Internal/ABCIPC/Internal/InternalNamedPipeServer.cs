
using System.IO.Pipes;
using System.Text;

namespace Starcounter.ABCIPC.Internal {

    /// <summary>
    /// Encapsulates the functionality of a server reading requests from,
    /// and writing replies to, a named pip.e
    /// </summary>
    internal sealed class InternalNamedPipeServer : Server {
        NamedPipeServerStream pipe;
        PipeProtocol protocol;

        internal InternalNamedPipeServer(string pipeName)
            : base() {
            this.pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1, PipeTransmissionMode.Message);
            this.protocol = new PipeProtocol(Encoding.UTF8);
            this.Receiver = ReadRequestFromPipe;
            this.Reply = SendReplyOnPipe;
        }

        string ReadRequestFromPipe() {
            pipe.WaitForConnection();
            return protocol.ReadMessage(pipe);
        }

        void SendReplyOnPipe(string reply, bool endsRequest) {
            protocol.WriteMesssage(pipe, reply);
            if (endsRequest) {
                pipe.WaitForPipeDrain();
                if (pipe.IsConnected) {
                    pipe.Disconnect();
                }
            }
        }
    }
}
