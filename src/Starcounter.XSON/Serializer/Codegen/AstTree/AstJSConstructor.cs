// ***********************************************************************
// <copyright file="AstRpConstructor.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Text;
using Starcounter.XSON.Serializer.Parsetree;

namespace Starcounter.XSON.Serializer.Ast {
    internal class AstJSConstructor : AstBase {
        internal ParseNode ParseNode { get; set; }

        internal override string DebugString {
            get {

                return "static " + ((AstJsonSerializerClass)Parent).ClassName + "() {...}";
            }
        }
    }
}
