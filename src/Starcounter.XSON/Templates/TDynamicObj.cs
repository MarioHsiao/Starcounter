using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Starcounter.Templates {

    /// <summary>
    /// The template used for dynamic Objs. I.e. objs using this template
    /// work like expando objects (or Javascript objects) where you can
    /// assign a property to a value without the property having to pre-exist.
    /// </summary>
    public class temp___TDynamicObj : TObj {

        /// <summary>
        /// Called when the user attempted to set a value on a dynamic obj without the object having
        /// the property defined (in its template). In the TDynamicObj template, this method will
        /// add the property to the template definition.
        /// </summary>
        /// <param name="property">The name of the missing property</param>
        /// <param name="Type">The type of the value being set</typeparam>
        //internal override void OnSetUndefinedProperty(string property, Type type) {
        //
//        }
    }
}

