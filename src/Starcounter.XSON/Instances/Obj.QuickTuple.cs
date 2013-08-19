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
                _Dirty = false;
                _Values = new object[vc];
                _BoundDirtyCheck = new object[vc];
                _DirtyProperties = new bool[vc];
                for (int t = 0; t < vc; t++) {
                    _Values[t] = ((Template)prop[t]).CreateInstance(this);
                }
        }

        /// <summary>
        /// Use this property to access the values internally
        /// </summary>
        protected dynamic[] Values {
            get {
                if (_Values.Length < this.Template.Properties.Count) {
                    // We allow adding new properties to dynamic templates
                    // even after instsances have been created.
                    // For this reason, we need to allow the expansion of the 
                    // values.
                    var old = _Values;
                    var oldD = _DirtyProperties;
                    var oldB = _BoundDirtyCheck;
                    var prop = Template.Properties;
                    var vc = prop.Count;
                    _Values = new object[vc];
                    _DirtyProperties = new bool[vc];
                    _BoundDirtyCheck = new object[vc];
                    old.CopyTo(_Values, 0);
                    oldD.CopyTo(_DirtyProperties, 0);
                    oldB.CopyTo(_BoundDirtyCheck, 0);
                    for (int t = old.Length; t < _Values.Length; t++) {
                        _Values[t] = ((Template)prop[t]).CreateInstance(this);
                        _DirtyProperties[t] = false; // Reduntant
                        _BoundDirtyCheck[t] = _Values[t];
                    }
                }
                return _Values;
            }
        }

        /// <summary>
        /// Used by the naive reference implementation (see _InitializeValues).
        /// Stores the actual values of each app property. The value is stored according to the Index value
        /// of the Template of the property.
        /// </summary>
        internal dynamic[] _Values;

        /// <summary>
        /// The naive implementation keeps track of the changed values
        /// generate the JSON-Patch document
        /// </summary>
        internal bool[] _DirtyProperties;

        /// <summary>
        /// The naive implementation keeps track of the changed database objects to
        /// generate the JSON-Patch document
        /// </summary>
        internal dynamic[] _BoundDirtyCheck;
#endif
    }
}
