﻿
using System;
using System.Collections.Generic;

namespace Starcounter.Advanced {


    /// <summary>
    /// Handlers registrations of user code handlers (delegates) of REST style requests.
    /// Usually, the developer uses the global static functions (GET/POST/etc.)
    /// in the StarcounterBase class rather.
    /// </summary>
    public interface IREST {

        /// <summary>
        /// Allows you to register event listeners that trigger whenever the developer
        /// registers a new request handler.
        /// </summary>
        List<Action<string>> RegistrationListeners { get; }

        /// <summary>
        /// Registers a handler with no parameters
        /// </summary>
        /// <param name="verbAndUri">The verb and uri of the request. For example GET /test</param>
        /// <param name="handler">The code to call when receiving the request</param>
        void RegisterHandler(string verbAndUri, Func<object> handler);

        /// <summary>
        /// Registers a handler with one parameter
        /// </summary>
        /// <typeparam name="T">The type of the parameter.</typeparam>
        /// <param name="verbAndUri">The verb and uri template of the request. For example GET /products/{?}</param>
        /// <param name="handler">The code to call when receiving the request</param>
        void RegisterHandler<T>(string verbAndUri, Func<T, object> handler);

        /// <summary>
        /// Registers a handler with two parameters
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <param name="verbAndUri">The verb and uri template of the request. For example GET /things/{?}/{?}</param>
        /// <param name="handler">The code to call when receiving the request</param>
        void RegisterHandler<T1, T2>(string verbAndUri, Func<T1, T2, object> handler);

        /// <summary>
        /// Registers a handler with three parameters
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="T3">The type of the third parameter.</typeparam>
        /// <param name="verbAndUri">The verb and uri template of the request.</param>
        /// <param name="handler">The code to call when receiving the request</param>
        void RegisterHandler<T1, T2, T3>(string verbAndUri, Func<T1, T2, T3, object> handler);

        /// <summary>
        /// Registers a handler with four parameters
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="T3">The type of the third parameter.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter.</typeparam>
        /// <param name="verbAndUri">The verb and uri template of the request.</param>
        /// <param name="handler">The code to call when receiving the request</param>
        void RegisterHandler<T1, T2, T3, T4>(string verbAndUri, Func<T1, T2, T3, T4, object> handler);

        /// <summary>
        /// Registers a handler with five parameters
        /// </summary>
        /// <typeparam name="T1">The type of the first parameter.</typeparam>
        /// <typeparam name="T2">The type of the second parameter.</typeparam>
        /// <typeparam name="T3">The type of the third parameter.</typeparam>
        /// <typeparam name="T4">The type of the fourth parameter.</typeparam>
        /// <typeparam name="T5">The type of the fifth parameter.</typeparam>
        /// <param name="verbAndUri">The verb and uri template of the request.</param>
        /// <param name="handler">The code to call when receiving the request</param>
        void RegisterHandler<T1, T2, T3, T4, T5>(string verbAndUri, Func<T1, T2, T3, T4, T5, object> handler);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        object Get(string uri);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        object Request(HttpRequest request);

        object RawRequest(byte[] request);
    }
}
