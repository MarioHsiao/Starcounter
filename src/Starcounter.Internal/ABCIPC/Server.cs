// ***********************************************************************
// <copyright file="Server.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.ABCIPC {

    /// <summary>
    /// Encapsulates the simplest kind of server, supporting handlers to
    /// be defined for incoming requests and a recieving message loop that
    /// reads requests from an opaque delegate, invokes a handler and
    /// replies to the request on an equal opaque output receiver delegate.
    /// </summary>
    public class Server {
        readonly Dictionary<string, Action<Request>> handlers;

        /// <summary>
        /// Gets or sets the receiving function of the server.
        /// </summary>
        protected Func<string> Receiver { get; set; }
        
        /// <summary>
        /// Gets the method to use when replying to requests. The first
        /// parameter should hold the reply and the second bool parameter
        /// indicates if the reply ends the request - if it's not, at
        /// least one more reply will come from the same request.
        /// </summary>
        internal Action<string, bool> Reply { get; set; }

        /// <summary>
        /// Event that occurs when the server receives a new request.
        /// </summary>
        public event EventHandler<string> ReceivedRequest;

        /// <summary>
        /// Initializes a <see cref="Server"/>.
        /// </summary>
        /// <remarks>
        /// Intended for internal use only, and only by inheritors that
        /// wish to declare receive and reply delegates after the server
        /// has been created.
        /// </remarks>
        protected Server() {
            handlers = new Dictionary<string, Action<Request>>(StringComparer.InvariantCultureIgnoreCase);
            this.Receiver = () => { throw new NotImplementedException("Receiver function has not been set."); };
            this.Reply = (x, y) => { throw new NotImplementedException("Reply function has not been set."); };
        }

        /// <summary>
        /// Initializes a <see cref="Server"/>.
        /// </summary>
        /// <param name="recieve">The receiving method, feeding the ABCIPC
        /// server with incoming requests.</param>
        /// <param name="reply">The replying method, used by the server to
        /// send replies. The boolean parameter indicates if the reply
        /// being sent ends the request.</param>
        public Server(Func<string> recieve, Action<string, bool> reply) {
            if (recieve == null)
                throw new ArgumentNullException("recieve");
            if (reply == null)
                throw new ArgumentNullException("reply");

            handlers = new Dictionary<string, Action<Request>>(StringComparer.InvariantCultureIgnoreCase);
            this.Receiver = recieve;
            this.Reply = reply;
        }

        /// <summary>
        /// Recieves messages until a shutdown is coming in.
        /// </summary>
        public void Receive() {
            Request request;
            Action<Request> handler;
            bool shutdown = false;

            while (!shutdown) {
                string s = Receiver();
                OnRequestReceived(s);

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
                    try {
                        handler(request);
                    } catch (Exception exception) {
                        request.RespondToExceptionInHandler(exception);
                        throw;
                    }
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

        /// <summary>
        /// Handles the specified message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="handler">The handler.</param>
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

        /// <summary>
        /// Notified on the arrival of a new request. Invoked before it
        /// is parsed and handled.
        /// </summary>
        /// <param name="request">The request that just came in.</param>
        protected virtual void OnRequestReceived(string request) {
            if (this.ReceivedRequest != null) {
                ReceivedRequest(this, request);
            }
        }
    }
}
