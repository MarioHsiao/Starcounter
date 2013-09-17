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

        public static implicit operator Json(Rows res) {
            return new Json(res);
        }
        
        public Json(Json parent, TObjArr templ) {
            this.Template = templ;
            Parent = parent;
        }

        protected Json(IEnumerable result) {
            _data = result;
            _PendingEnumeration = true;
        }



    }
}

