// ***********************************************************************
// <copyright file="SingleRequestProcessor.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
namespace Starcounter.Internal.Uri {

    /// <summary>
    /// Class SingleRequestProcessorBase
    /// </summary>
    public abstract class SingleRequestProcessorBase : RequestProcessor {
        //        public abstract override bool Process(byte[] fragment, bool invoke, HttpRequest request, out SingleRequestProcessorBase handler, out App resource );
        /// <summary>
        /// Gets or sets the code as obj.
        /// </summary>
        /// <value>The code as obj.</value>
        public abstract object CodeAsObj { get; set; }
    }

    /// <summary>
    /// Class SingleRequestProcessor
    /// </summary>
    public abstract class SingleRequestProcessor : SingleRequestProcessorBase {
        /// <summary>
        /// Gets or sets the code.
        /// </summary>
        /// <value>The code.</value>
        public Func<object> Code { get; set; }
        /// <summary>
        /// Gets or sets the code as obj.
        /// </summary>
        /// <value>The code as obj.</value>
        public override object CodeAsObj { get { return Code; } set { Code = (Func<object>)value; } }

        //public override App Invoke(byte[] uri, HttpRequest request) {
        //    return Code.Invoke();
        //}
    }

    /// <summary>
    /// Class SingleRequestProcessor
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class SingleRequestProcessor<T> : SingleRequestProcessorBase {
        /// <summary>
        /// Gets or sets the code.
        /// </summary>
        /// <value>The code.</value>
        public Func<T, object> Code { get; set; }
        /// <summary>
        /// Gets or sets the code as obj.
        /// </summary>
        /// <value>The code as obj.</value>
        public override object CodeAsObj { get { return Code; } set { Code = (Func<T, object>)value; } }
    }

    /// <summary>
    /// Class SingleRequestProcessor
    /// </summary>
    /// <typeparam name="T1">The type of the t1.</typeparam>
    /// <typeparam name="T2">The type of the t2.</typeparam>
    public abstract class SingleRequestProcessor<T1, T2> : SingleRequestProcessorBase {
        /// <summary>
        /// Gets or sets the code.
        /// </summary>
        /// <value>The code.</value>
        public Func<T1, T2, object> Code { get; set; }
        /// <summary>
        /// Gets or sets the code as obj.
        /// </summary>
        /// <value>The code as obj.</value>
        public override object CodeAsObj { get { return Code; } set { Code = (Func<T1, T2, object>)value; } }
    }

    /// <summary>
    /// Class SingleRequestProcessor
    /// </summary>
    /// <typeparam name="T1">The type of the t1.</typeparam>
    /// <typeparam name="T2">The type of the t2.</typeparam>
    /// <typeparam name="T3">The type of the t3.</typeparam>
    public abstract class SingleRequestProcessor<T1, T2, T3> : SingleRequestProcessorBase {
        /// <summary>
        /// Gets or sets the code.
        /// </summary>
        /// <value>The code.</value>
        public Func<T1, T2, T3, object> Code { get; set; }
        /// <summary>
        /// Gets or sets the code as obj.
        /// </summary>
        /// <value>The code as obj.</value>
        public override object CodeAsObj { get { return Code; } set { Code = (Func<T1, T2, T3, object>)value; } }
    }
}