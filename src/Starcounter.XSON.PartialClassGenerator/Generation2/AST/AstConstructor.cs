// ***********************************************************************
// <copyright file="NConstructor.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Text;

namespace Starcounter.Internal.MsBuild.Codegen

{
    /// <summary>
    /// Represents a constructor
    /// </summary>
    public class AstConstructor : AstBase
    {

        public override string Name {
            get { return ""; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gen"></param>
        public AstConstructor(Gen2DomGenerator gen)
            : base(gen) {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return "NCONSTRUCTOR";
        }
    }
}
