// ***********************************************************************
// <copyright file="AstCase.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Text;
using Starcounter.Templates;
using Starcounter.XSON.Serializer.Parsetree;

namespace Starcounter.XSON.Serializer.Ast {
    internal class AstJsonProperty : AstBase {
        internal Template Template { get; set; }
		internal ParseNode ParseNode { get; set; }
		internal bool IsLast { get; set; }

        internal override string DebugString {
            get {
                return "Property(" + Template.TemplateName + ")";
            }
        }
    }
}
