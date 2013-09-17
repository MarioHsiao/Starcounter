// ***********************************************************************
// <copyright file="App.QuickTuple.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Text;
using Starcounter.Templates;
using System;
using System.Collections.Generic;
using Starcounter.Internal;

namespace Starcounter {
    /// <summary>
    /// Class App
    /// </summary>
    public partial class Json {

        /// <summary>
        /// The QUICKTUPLE implementation keeps the property values of an App in a simple array of 
        /// boxed CLR values. This implementation should never be used on the server side as the
        /// strain on the garbage collector and the memory consumption would be to great. Instead, the
        /// server side represetation should use the default session blob model.
        /// </summary>
        protected void _InitializeValues() {
                InitializeCache();
        }


    }
}
