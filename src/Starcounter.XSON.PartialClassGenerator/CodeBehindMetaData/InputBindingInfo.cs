// ***********************************************************************
// <copyright file="InputBindingInfo.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;

namespace Starcounter.XSON.Metadata {
    /// <summary>
    /// Class InputBindingInfo
    /// </summary>
    public class InputBindingInfo {
        /// <summary>
        /// Initializes a new instance of the <see cref="InputBindingInfo" /> class.
        /// </summary>
        public InputBindingInfo() { }

        /// <summary>
        /// The namespace of the class where the Handle method is declared.
        /// </summary>
        public String DeclaringClassNamespace;

        /// <summary>
        /// The name of the class where the Handle method is declared.
        /// </summary>
        public String DeclaringClassName;

        /// <summary>
        /// The fullname of the inputtype specified in the Handle method.
        /// </summary>
        public String FullInputTypeName;
    }
}
