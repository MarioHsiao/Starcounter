// ***********************************************************************
// <copyright file="App.QuickTuple.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Text;
using Starcounter.Templates;

namespace Starcounter {
    /// <summary>
    /// Class App
    /// </summary>
    public partial class Obj {

#if QUICKTUPLE
        /// <summary>
        /// The QUICKTUPLE implementation keeps the property values of an App in a simple array of 
        /// boxed CLR values. This implementation should never be used on the server side as the
        /// strain on the garbage collector and the memory consumption would be to great. Instead, the
        /// server side represetation should use the default session blob model.
        /// </summary>
        protected override void _InitializeValues() {
                var prop = Template.Properties;
                var vc = prop.Count;
                _Values = new object[vc];
                for (int t = 0; t < vc; t++) {
                    _Values[t] = ((Template)prop[t]).CreateInstance(this);
                }
        }

        /// <summary>
        /// Used by the naive reference implementation (see _InitializeValues).
        /// Stores the actual values of each app property. The value is stored according to the Index value
        /// of the Template of the property.
        /// </summary>
        internal dynamic[] _Values;
#endif

    }
}
