// ***********************************************************************
// <copyright file="NRoot.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Templates;
namespace Starcounter.Internal.MsBuild.Codegen {


    /// <summary>
    /// The single AST root
    /// </summary>
    public class NRoot : NBase {

        public override string Name {
            get { return ""; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gen"></param>
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
