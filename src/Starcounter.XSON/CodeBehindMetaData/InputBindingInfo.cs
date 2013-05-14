// ***********************************************************************
// <copyright file="InputBindingInfo.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;

namespace Starcounter.XSON.Metadata {
    /// <summary>
    /// Class InputBindingInfo
    /// </summary>
    public class InputBindingInfo {
        /// <summary>
        /// Initializes a new instance of the <see cref="InputBindingInfo" /> class.
        /// </summary>
        /// <param name="classNs">The class ns.</param>
        /// <param name="className">Name of the class.</param>
        /// <param name="fullInputTypename">The full input typename.</param>
        public InputBindingInfo(String classNs, String className, String fullInputTypename) {
            DeclaringClassNamespace = classNs;
            DeclaringClassName = className;
            FullInputTypeName = fullInputTypename;
        }

        /// <summary>
        /// The namespace of the class where the Handle method is declared.
        /// </summary>
        public readonly String DeclaringClassNamespace;

        /// <summary>
        /// The name of the class where the Handle method is declared.
        /// </summary>
        public readonly String DeclaringClassName;

        /// <summary>
        /// The fullname of the inputtype specified in the Handle method.
        /// </summary>
        public readonly String FullInputTypeName;
    }
}
