// ***********************************************************************
// <copyright file="AppList.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using Starcounter;

using Starcounter.Templates;
using Starcounter.Advanced;
using System.Collections;
using Starcounter.Internal.XSON;
using System.Text;
using System.Diagnostics;

namespace Starcounter {



    public partial class Json {

        /// <summary>
        /// You can assign a result set from a SQL query operation directly to 
        /// a JSON array property.
        /// <example>
        /// myJson.Items = Db.SQL("SELECT i FROM Items i");
        /// </example>
        /// </summary>
        /// <param name="res">The SQL result set</param>
        /// <returns></returns>
        public static implicit operator Json(Rows res) {
            return new Json(res);
        }
        
        public Json(Json parent, TObjArr templ) {
            this.Template = templ;
            Parent = parent;
        }

        /// <summary>
        /// Creates a Json array bound to a enumerable data source such as
        /// for example a SQL query result.
        /// </summary>
        /// <param name="result">The data source</param>
        protected Json(IEnumerable result) {
            _data = result;
            _PendingEnumeration = true;
        }



    }
}

