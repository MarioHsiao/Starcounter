// ***********************************************************************
// <copyright file="NPredefinedClass.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Templates;
namespace Starcounter.Internal.Application.CodeGeneration {

    /// <summary>
    /// Represents a class or type that is provided by a library
    /// (such as String, bool).
    /// </summary>
    public class NPredefinedType : NClass {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gen"></param>
        public NPredefinedType(DomGenerator gen) : base(gen) {
        }

        /// <summary>
        /// The _ fixed class name
        /// </summary>
        private string _FixedClassName;

        /// <summary>
        /// Sets the fixed name of the type
        /// </summary>
        /// <value>The name of the fixed class.</value>
        public string FixedClassName {
            set {
                _FixedClassName = value;
            }
        }

        /// <summary>
        /// As no declaring code is generated from these nodes,
        /// there is no need to track the inherited types
        /// </summary>
        /// <value>The inherits.</value>
        public override string Inherits {
            get { return null; }
        }

        /// <summary>
        /// The class name (type name) is provided as a fixed
        /// string
        /// </summary>
        /// <value>The name of the class.</value>
        public override string ClassName {
            get {
                return _FixedClassName;
            }
        }

    }
}
