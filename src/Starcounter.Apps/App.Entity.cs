// ***********************************************************************
// <copyright file="App.Entity.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using Starcounter.Templates;

namespace Starcounter {
    /// <summary>
    /// Class App
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class App<T> : App where T : Entity {
        /// <summary>
        /// 
        /// </summary>
        public App() : base() {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="template"></param>
        public App(AppTemplate template) : base(template) {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="template"></param>
        /// <param name="initializeTransaction"></param>
        public App(AppTemplate template, Func<Entity> initializeTransaction)
            : base(template, initializeTransaction) {
        }

        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        /// <value>The data.</value>
        public new T Data 
        {
            get { return (T)_Data; } 
            set { 
                _Data = value;
                RefreshAllBoundValues();
                OnData();
            }  
        }
    }
}