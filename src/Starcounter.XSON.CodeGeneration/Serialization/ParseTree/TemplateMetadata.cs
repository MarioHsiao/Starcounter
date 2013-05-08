// ***********************************************************************
// <copyright file="RequestProcessorMetaData.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using Starcounter.Templates;

namespace Starcounter.Internal.Application.CodeGeneration {
    /// <summary>
    /// 
    /// </summary>
    internal class TemplateMetadata  {
        private byte[] nameArr = null;

        /// <summary>
        /// The name of the template.
        /// </summary>
        internal string TemplateName { get { return Template.PropertyName; } }

        /// <summary>
        /// The name of the template converted to a UTF8 bytearray.
        /// </summary>
        internal byte[] TemplateNameArr { get { return nameArr; } } 

       /// <summary>
        /// The Template.
        /// </summary>
        internal readonly Template Template;

        /// <summary>
        /// 
        /// </summary>
        internal Int32 TemplateIndex;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="template"></param>
        internal TemplateMetadata(Template template) {
            Template = template;
            nameArr = System.Text.Encoding.UTF8.GetBytes(Template.PropertyName);
        }
    }

}