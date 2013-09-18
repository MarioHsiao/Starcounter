// ***********************************************************************
// <copyright file="AstCase.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Text;
using Starcounter.XSON.Serializer.Parsetree;

namespace Starcounter.XSON.Serializer.Ast {
    internal class AstCase : AstBase {
        internal ParseNode ParseNode { get; set; }

        internal override string DebugString {
            get {
                if (ParseNode.Match == 0) {
                    return "case '':";
                }
                return "case '" + (char)ParseNode.Match + "':";
            }
        }
    }
}
