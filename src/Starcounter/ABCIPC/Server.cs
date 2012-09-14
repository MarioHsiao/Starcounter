﻿using System;
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

            do {
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
                    // Unsupported message. The default sends back
                    // such an error to the client and goes back to
                    // receive. This can be overridden by a certain
                    // handler.
                    // TODO:
                    // Implement this in Request.UnsupportedMessage.
                }

                // Install handler around request and send that back
                // as a "handler exception" and then rethrow the exception
                // on the server.
                // TODO:

                handler(request);

                // How do we handle requests not responded to? We send
                // a response indicating it was a success.

                if (!request.IsResponded) {
                    request.Respond(true);
                }

                // Check if the reguest was marked to shutdown the server.
                // If so, we stop receiving. Use predefined message.
                // TODO:

            } while (!request.IsShutdown);
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
