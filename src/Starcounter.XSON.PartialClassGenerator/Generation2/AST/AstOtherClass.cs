// ***********************************************************************
// <copyright file="NOtherClass.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

namespace Starcounter.Internal.MsBuild.Codegen {


    /// <summary>
    /// Used for classes where a simple class name and inherits name
    /// is sufficient for code generation
    /// </summary>
    public class AstOtherClass : AstClass {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gen"></param>
        public AstOtherClass(Gen2DomGenerator gen)
            : base(gen) {
        }

        /// <summary>
        /// The _ class name
        /// </summary>
        public string _ClassName;
        /// <summary>
        /// The _ inherits
        /// </summary>
        public string _Inherits;

        /// <summary>
        /// Gets the name of the class.
        /// </summary>
        /// <value>The name of the class.</value>
        public override string ClassName {
            get { return _ClassName; }
        }
        /// <summary>
        /// Gets the inherits.
        /// </summary>
        /// <value>The inherits.</value>
        public override string Inherits {
            get { return _Inherits; }
        }
    }

}