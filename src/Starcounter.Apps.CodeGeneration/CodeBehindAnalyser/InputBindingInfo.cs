using System;
using System.Collections.Generic;

namespace Starcounter.Internal.Application.CodeGeneration
{
    public class InputBindingInfo
    {
        internal InputBindingInfo(String classNs, String className, String fullInputTypename)
        {
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
