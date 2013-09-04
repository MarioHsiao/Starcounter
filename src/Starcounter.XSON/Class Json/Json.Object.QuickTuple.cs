// ***********************************************************************
// <copyright file="App.QuickTuple.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Text;
using Starcounter.Templates;
using System;
using System.Collections.Generic;

namespace Starcounter {
    /// <summary>
    /// Class App
    /// </summary>
    public partial class Container {

#if QUICKTUPLE
        /// <summary>
        /// The QUICKTUPLE implementation keeps the property values of an App in a simple array of 
        /// boxed CLR values. This implementation should never be used on the server side as the
        /// strain on the garbage collector and the memory consumption would be to great. Instead, the
        /// server side represetation should use the default session blob model.
        /// </summary>
        protected void _InitializeValues() {
            if (IsArray) {
                _Values = new List<object>();
            }
            else {
                _InitializeProperties( (TObject)Template);
            }
        }

        /// <summary>
        /// If this is an object, each value has a property template
        /// and each value needs to be set to its initial value
        /// </summary>
        /// <param name="template"></param>
        private void _InitializeProperties(TObject template) {
            var prop = template.Properties;
            var vc = prop.Count;
            _Dirty = false;
            _Values = new List<object>(vc);
            _BoundDirtyCheck = new List<object>(vc);
            _DirtyValues = new List<bool>(vc);
            for (int t = 0; t < vc; t++) {
                _Values[t] = ((Template)prop[t]).CreateInstance(this);
            }
        }

        /// <summary>
        /// Use this property to access the values internally
        /// </summary>
        protected List<object> Values {
            get {
                var template = (TObject)Template;
                if (IsArray) {
                    return _Values;
                }
                else {
                    while (_Values.Count < template.Properties.Count) {
                        // We allow adding new properties to dynamic templates
                        // even after instances have been created.
                        // For this reason, we need to allow the expansion of the 
                        // values.
                        var prop = template.Properties;
                        var newVal = ((Template)prop[_Values.Count]).CreateInstance(this);
                        _Values.Add(newVal);
                        _DirtyValues.Add(false); // Reduntant
                        _BoundDirtyCheck.Add(newVal);
                    }
                    return _Values;
                }
            }
        }

        /// <summary>
        /// Used by the naive reference implementation (see _InitializeValues).
        /// Stores the actual values of the JSON objects (the properties) or the
        /// JSON array (the array elements). The value is stored according to the Index value
        /// of the Template of the property.
        /// </summary>
        internal List<object> _Values;

        /// <summary>
        /// The naive implementation keeps track of the changed values
        /// generate the JSON-Patch document
        /// </summary>
        internal List<bool> _DirtyValues;

        /// <summary>
        /// The naive implementation keeps track of the changed database objects to
        /// generate the JSON-Patch document
        /// </summary>
        internal List<object> _BoundDirtyCheck;
#endif
    }
}
