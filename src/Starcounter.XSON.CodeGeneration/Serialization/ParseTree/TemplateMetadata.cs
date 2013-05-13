// ***********************************************************************
// <copyright file="RequestProcessorMetaData.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Text;
using Starcounter.Templates;

namespace Starcounter.Internal.Application.CodeGeneration {
    /// <summary>
    /// 
    /// </summary>
    internal class TemplateMetadata  {
        private byte[] nameArr = null;

        internal const byte END_OF_PROPERTY = (byte)'"';

        /// <summary>
        /// The name of the template.
        /// </summary>
        internal string TemplateName { get { return Template.TemplateName; } }

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

            int size = Encoding.UTF8.GetByteCount(Template.PropertyName);
            nameArr = new byte[size + 1];

            Encoding.UTF8.GetBytes(Template.PropertyName, 0, size, nameArr, 0);
            nameArr[nameArr.Length - 1] = END_OF_PROPERTY;
        }
    }

}