// ***********************************************************************
// <copyright file="RequestProcessorMetaData.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System.Collections.Generic;
using System;
using Starcounter.Templates;

namespace Starcounter.CodeGeneration.Serialization {
    /// <summary>
    /// 
    /// </summary>
    internal class TemplateMetadata  {
        /// <summary>
        /// The template index.
        /// </summary>
        internal int TemplateIndex { get { return Template.TemplateIndex } }

        /// <summary>
        /// The name of the template.
        /// </summary>
        internal string TemplateName { get { return Template.PropertyName; } }

       /// <summary>
        /// The Template.
        /// </summary>
        internal Template Template;

        /// <summary>
        /// 
        /// </summary>
        internal Int32 HandlerId;
    }

}