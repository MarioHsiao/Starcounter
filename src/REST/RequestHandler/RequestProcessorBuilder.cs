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
using System.Security.Cryptography;
using System.Diagnostics;
using System.IO;
using System.Reflection;

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
        /// The application developers Verb + URI handlers. A handler is registred
        /// using the HTTP verb and URI together with a delegate function. For instance,
        /// GET("/demo", () => { return "HelloWorld"; } );
        /// </summary>
        public List<RequestProcessorMetaData> Handlers = new List<RequestProcessorMetaData>();

        /// <summary>
        /// The handler checksum is a probabilistic identifier for a set of handlers.
        /// It is used to recognize a cached code generated request processor.
        /// In this way, Starcounter can reuse an assembly with precompiled code to save start up time.
        /// </summary>
        /// <remarks>
        /// The Sha-1 checksum is deemed as a unique identifier for the purpose. If you whish to
        /// remove the probabilistic of a false positive, you should remove all cached RequestProcessor
        /// assemblies when you deploy a new version of your application. These files begin with the
        /// "RequestProc_" prefix.
        /// 
        /// I.e. the file "RequestProc_F894E0A7E54CBC4AEA31999A624665E75CE70F30.dll" contains
        /// the parser  for GET /players/{?} (int playerId), GET /dashboard/{?} (int playerId)
        /// and some other verb and uri templates.
        /// </remarks>
        public string HandlerSetChecksum {
            get {
                var total = "";

                foreach (var h in Handlers) {
                    total += "\r\n" + h.PreparedVerbAndUri;
                }
                
                var str = "";
                using (var sha1 = new SHA1Managed()) {
                    byte[] hash = sha1.ComputeHash( Encoding.UTF8.GetBytes(total) );

                    foreach (byte b in hash) {
                        str += b.ToString("X2");
                    }
                }

                // Console.WriteLine("SHA-1 " + str + " encaplulates " + total);

                return str;
            }
        }

        /// <summary>
        /// To allow dynamically added REST handlers, each generated request processor needs a unique namespace.
        /// This is aschieved by using the SHA-1 checksum of the verb and uri strings for all handlers in the
        /// processor.
        /// </summary>
        public string Namespace {
            get {
                return "__RP_" + HandlerSetChecksum;
            }
        }

        /// <summary>
        /// Underlying variable for the RegistrationListeners property.
        /// </summary>
        private List<Action<string>> _RegistrationListeners = new List<Action<string>>();

        /// <summary>
        /// You can register listeners that will trigger whenever a new handler is registred or replaced.
        /// </summary>
        /// <remarks>
        /// Starcounter uses these listeners internally to allow the Starcounter Network gateway to
        /// keep track on how to route incomming HTTP requests to the correct datababase.
        /// </remarks>
        public List<Action<string>> RegistrationListeners {
            get {
                return _RegistrationListeners;
            }
        }

        /// <summary>
        /// Creates an object that can be used to generate and compile sourced code that
        /// processes incomming HTTP requests by matching them and potentially invoking them.
        /// </summary>
        /// <returns>The object capable of code generation/compilation</returns>
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

        /// <summary>
        /// This is the main method to generate and instantiate a top level request processor, i.e. the
        /// processor that matches and parses all request handlers registed to the application domain.
        /// </summary>
        /// <remarks>
        /// The file system is checked for a cached assembly containing the request processor for the
        /// current set of user handlers. If no assembly is found, a request generator is code generated.
        /// </remarks>
        /// <returns>The processor</returns>
        public TopLevelRequestProcessor InstantiateRequestProcessor() {
            Stopwatch sw = new Stopwatch();

            TopLevelRequestProcessor topRp;

            string fileName = Namespace + ".dll";

            if (File.Exists(fileName)) {
                sw.Start();
                MemoryStream ms = new MemoryStream();
                using (var fs = File.Open(fileName,FileMode.Open)) {
                    fs.CopyTo(ms);
                    fs.Close();
                }
                var a = Assembly.Load(ms.GetBuffer());
                topRp = (TopLevelRequestProcessor)a.CreateInstance(Namespace + ".GeneratedRequestProcessor");
                sw.Stop();
                Console.WriteLine(String.Format("Found cached assembly {0} ({1} seconds spent).", fileName, (double)sw.ElapsedMilliseconds / 1000));
            }
            else {
                sw.Start();
                var compiler = CreateCompiler();
                var pt = ParseTreeGenerator.BuildParseTree(Handlers);
                var ast = AstTreeGenerator.BuildAstTree(pt);
                ast.Namespace = Namespace;
                topRp = compiler.CreateMatcher(ast,fileName);
                sw.Stop();
                Console.WriteLine(String.Format("Time to create request processor is {0} seconds.", (double)sw.ElapsedMilliseconds / 1000));
            }

            foreach (var h in Handlers) {
                topRp.Register(h.PreparedVerbAndUri, h.Code);
            }



            return topRp;
        }


    }

}