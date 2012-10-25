// ***********************************************************************
// <copyright file="PrimitivePropertyBinding.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

namespace Starcounter.Binding
{

    /// <summary>
    /// Class PrimitivePropertyBinding
    /// </summary>
    public abstract class PrimitivePropertyBinding : PropertyBinding
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="PrimitivePropertyBinding" /> class.
        /// </summary>
        public PrimitivePropertyBinding() : base() { }

        /// <summary>
        /// Gets the type binding.
        /// </summary>
        /// <value>The type binding.</value>
        public override sealed ITypeBinding TypeBinding
        {
            get
            {
                return null;
            }
        }
    }
}
