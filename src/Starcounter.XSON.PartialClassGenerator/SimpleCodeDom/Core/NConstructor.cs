// ***********************************************************************
// <copyright file="NConstructor.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Text;

namespace Starcounter.Internal.Application.CodeGeneration
{
    /// <summary>
    /// Represents a constructor
    /// </summary>
    public class NConstructor : NBase
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="gen"></param>
        public NConstructor(DomGenerator gen)
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
