// ***********************************************************************
// <copyright file="NPrimitiveType.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using Starcounter.Templates;

namespace Starcounter.Internal.Application.CodeGeneration
{
    /// <summary>
    /// Class NPrimitiveType
    /// </summary>
    public class NPrimitiveType : NValueClass
    {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gen"></param>
        public NPrimitiveType(DomGenerator gen)
            : base(gen) {
        }

        /// <summary>
        /// Gets the inherits.
        /// </summary>
        /// <value>The inherits.</value>
        /// <exception cref="System.NotImplementedException"></exception>
        public override string Inherits
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Gets the name of the class.
        /// </summary>
        /// <value>The name of the class.</value>
        public override string ClassName
        {
            get
            {
                if (NTemplateClass.Template is TTrigger)
                    return "Action";

                var type = NTemplateClass.Template.InstanceType;
                if (type == typeof(Int64))
                {
                    return "long";
                }
                else if (type == typeof(Boolean))
                {
                    return "bool";
                }
                return type.Name;
            }
        }
    }
}
