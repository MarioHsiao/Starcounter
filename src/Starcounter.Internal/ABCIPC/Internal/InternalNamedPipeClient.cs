using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Pipes;

namespace Starcounter.ABCIPC.Internal {
    /// <summary>
    /// Encapsulates the functionality of a client writing requests to,
    /// and reading replies from, a named pipe.
    /// </summary>
    internal sealed class InternalNamedPipeClient : Client {
        NamedPipeClientStream pipe;
        readonly string pipeName;
        PipeProtocol protocol;
        
        internal InternalNamedPipeClient(string pipeName) : base() {
            this.pipeName = pipeName;
            this.protocol = new PipeProtocol(Encoding.UTF8);
            Bind(SendRequestOnPipe, ReceiveReplyOnPipe);
        }

        void SendRequestOnPipe(string request) {
            pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut);
            pipe.Connect(3000);
            protocol.WriteMesssage(pipe, request);
        }

        string ReceiveReplyOnPipe() {
            var reply = protocol.ReadMessage(pipe);
            pipe.Close();
            return reply;
        }
    }
}