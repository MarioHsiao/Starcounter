using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.ABCIPC {

    public sealed class Server {
        internal readonly Func<string> receive;
        internal readonly Action<string> reply;
        internal Dictionary<string, Action<Request>> handlers;

        public Server(Func<string> recieve, Action<string> reply) {
            this.receive = recieve;
            this.reply = reply;
            handlers = new Dictionary<string, Action<Request>>(StringComparer.InvariantCultureIgnoreCase);
        }

        public void Receive() {
            Request request;
            Action<Request> handler;
            bool shutdown = false;

            while (!shutdown) {
                string s = receive();

                // Allow installing a preparser, so that we can support simpler
                // syntax from the console, for example.
                // TODO:

                // Parse the input, validate it and invoke the handler.
                //
                // Make a try/catch and send back a parser error if it
                // didn't parse properly. Include s + e.Message.
                // TODO:

                request = Request.Protocol.Parse(this, s);

                // It was OK according to specification. Find and invoke
                // the handler.

                handler = null;
                try {
                    handler = GetHandler(request.Message);
                } catch (KeyNotFoundException) {
                    // Check if it's one of the built-in messages, like
                    // shutdown. If it is, it means there need not to be
                    // any handler installed.

                    if (!request.IsShutdown) {
                        
                        // Unsupported message. By default, we answer
                        // back to the client with a well-known reply
                        // and gets back for the next request.

                        request.RespondToUnknownMessage();
                        continue;
                    }
                }

                // Install handler around request and send that back
                // as a "handler exception" and then rethrow the exception
                // on the server.
                // TODO:

                if (handler != null) {
                    handler(request);
                }

                // How do we handle requests not responded to? We send
                // a response indicating it was a success.

                if (!request.IsResponded) {
                    request.Respond(true);
                }

                // Check if the reguest was marked to shutdown the server.
                // If so, we stop receiving.

                shutdown = request.IsShutdown;
            }
        }

        public void Handle(string message, Action<Request> handler) {
            handlers[message] = handler;
        }

        // We have a built-in reporting mechanism for "invalid signature".

        //public void Handle2(string message, Action<Request> handler) {
        //    // The framework assures that Send(string) was used.
        //}

        //public void Handle2(string message, string parameter, Action<string, Request> handler) {
        //    // The framework assures that Send(string, string) was used.
        //}

        //public void Handle2(string message, string[] parameters, Action<string[], Request> handler) {
        //    // The framework assures that Send(string, string[]) was used.
        //}

        //public void Handle2(string message, Dictionary<string, string> parameters, Action<Dictionary<string, string>, Request> handler) {
        //    // The framework assures that Send(string, Dictionary<string, string>) was used.
        //}

        Action<Request> GetHandler(string message) {
            return handlers[message];
        }
    }
}
