// ***********************************************************************
// <copyright file="NRoot.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Templates;
namespace Starcounter.Internal.Application.CodeGeneration {

    // The source code root keeps track of the namespace and the single root
    // generated App class
    /// <summary>
    /// Class NRoot
    /// </summary>
    public class NRoot : NBase {

        public NRoot(DomGenerator gen)
            : base(gen) {
        }

        /// <summary>
        /// The app class class node
        /// </summary>
        public NAppClass AppClassClassNode;

      //  public TObj DefaultObjTemplate;
    }
}
