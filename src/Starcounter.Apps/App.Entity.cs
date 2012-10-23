// ***********************************************************************
// <copyright file="App.Entity.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

namespace Starcounter {
    /// <summary>
    /// Class App
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class App<T> : App where T : Entity {
        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        /// <value>The data.</value>
        public new T Data { get; set; }
    }
}