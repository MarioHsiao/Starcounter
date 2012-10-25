// ***********************************************************************
// <copyright file="RequestProcessorBuilder.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Dynamic;
using System.Collections.Generic;
using System.Text;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Starconter.WebServer.Tests")]

namespace Starcounter.Internal.Uri {

    /// <summary>
    /// For fast execution, Starcounter will generate code to match incomming HTTP requests.
    /// By matching and parsing the verb and URI, the correct user handler delegate will be called.
    /// This class is responsible to accept registration of user handlers and also to
    /// generate code for and instantiate the top level and sub level RequestProcessors
    /// needed to perform this task. The code generation and instantiation is performed as late
    /// as possible. If additional handlers are registred after code has been generated, a new
    /// version is generated and replaces the old version (not yet implemented).
    /// </summary>
    public partial class RequestProcessorBuilder {

        /// <summary>
        /// The handlers
        /// </summary>
        public List<RequestProcessorMetaData> Handlers = new List<RequestProcessorMetaData>();

        /// <summary>
        /// The _ registration listeners
        /// </summary>
        private List<Action<string>> _RegistrationListeners = new List<Action<string>>();

        /// <summary>
        /// Gets the registration listeners.
        /// </summary>
        /// <value>The registration listeners.</value>
        public List<Action<string>> RegistrationListeners {
            get {
                return _RegistrationListeners;
            }
        }

        /// <summary>
        /// Creates the compiler.
        /// </summary>
        /// <returns>RequestProcessorCompiler.</returns>
        public RequestProcessorCompiler CreateCompiler() {
            var compiler = new RequestProcessorCompiler();
            compiler.Handlers = Handlers;
            return compiler;
        }

        /// <summary>
        /// Registers a handler with no parameters
        /// </summary>
        /// <param name="verbAndUri">The verb and uri of the request. For example GET /things/123</param>
        /// <param name="handler">The code to call when receiving the request</param>
        public void RegisterHandler(string verbAndUri, Func<object> handler) {
            InvokeListeners(verbAndUri);
            var m = new RequestProcessorMetaData();
            m.Code = handler;
            m.UnpreparedVerbAndUri = verbAndUri; // Encoding.UTF8.GetBytes(uri);
            Handlers.Add(m);
            m.HandlerIndex = Handlers.IndexOf(m);
        }

        /// <summary>
        /// Registers a handler with one parameter
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="verbAndUri">The verb and uri of the request. For example GET /things/123</param>
        /// <param name="handler">The code to call when receiving the request</param>
        public void RegisterHandler<T>(string verbAndUri, Func<T, object> handler) {
            InvokeListeners(verbAndUri);
            var m = new RequestProcessorMetaData();
            m.ParameterTypes.Add(typeof(T));
            m.Code = handler;
            m.UnpreparedVerbAndUri = verbAndUri; // Encoding.UTF8.GetBytes(uri);
            Handlers.Add(m);
            m.HandlerIndex = Handlers.IndexOf(m);
        }

        /// <summary>
        /// Registers a handler with two parameters
        /// </summary>
        /// <typeparam name="T1">The type of the t1.</typeparam>
        /// <typeparam name="T2">The type of the t2.</typeparam>
        /// <param name="verbAndUri">The verb and uri of the request. For example GET /things/123</param>
        /// <param name="handler">The code to call when receiving the request</param>
        public void RegisterHandler<T1, T2>(string verbAndUri, Func<T1, T2, object> handler) {
            InvokeListeners(verbAndUri);
            var m = new RequestProcessorMetaData();
            m.ParameterTypes.Add(typeof(T1));
            m.ParameterTypes.Add(typeof(T2));
            m.Code = handler;
            m.UnpreparedVerbAndUri = verbAndUri; // Encoding.UTF8.GetBytes(uri);
            Handlers.Add(m);
            m.HandlerIndex = Handlers.IndexOf(m);
        }

        /// <summary>
        /// Registers a handler with three parameters
        /// </summary>
        /// <typeparam name="T1">The type of the t1.</typeparam>
        /// <typeparam name="T2">The type of the t2.</typeparam>
        /// <typeparam name="T3">The type of the t3.</typeparam>
        /// <param name="verbAndUri">The verb and uri of the request. For example GET /things/123</param>
        /// <param name="handler">The code to call when receiving the request</param>
        public void RegisterHandler<T1, T2, T3>(string verbAndUri, Func<T1, T2, T3, object> handler) {
            InvokeListeners(verbAndUri);
            var m = new RequestProcessorMetaData();
            m.ParameterTypes.Add(typeof(T1));
            m.ParameterTypes.Add(typeof(T2));
            m.ParameterTypes.Add(typeof(T3));
            m.Code = handler;
            m.UnpreparedVerbAndUri = verbAndUri; // Encoding.UTF8.GetBytes(uri);
            Handlers.Add(m);
            m.HandlerIndex = Handlers.IndexOf(m);
        }

        /// <summary>
        /// Creates the parse tree.
        /// </summary>
        /// <returns>ParseNode.</returns>
        public ParseNode CreateParseTree() {
            return ParseTreeGenerator.BuildParseTree(Handlers);
        }

        /// <summary>
        /// Creates the ast tree.
        /// </summary>
        /// <returns>AstNamespace.</returns>
        public AstNamespace CreateAstTree() {
            return AstTreeGenerator.BuildAstTree(CreateParseTree());
        }

        /// <summary>
        /// Invokes the listeners.
        /// </summary>
        /// <param name="verbAndUri">The verb and URI.</param>
        private void InvokeListeners(string verbAndUri) {
            foreach (var listener in RegistrationListeners) {
                listener.Invoke(verbAndUri);
            }
        }
    }

}