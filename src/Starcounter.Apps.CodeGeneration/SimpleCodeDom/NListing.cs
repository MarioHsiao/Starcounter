// ***********************************************************************
// <copyright file="NListing.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Templates;
using System;
using System.Collections.Generic;
using System.Text;
namespace Starcounter.Internal.Application.CodeGeneration {

    /// <summary>
    /// The source code representation of each Listing&lt;T1&gt;, ArrProperty&lt;T1,T2&gt; 
    /// or ArrMetadata&lt;T1,T2&gt; class where 
    /// T1 is the link to the App class and T2 is the link to the AppTemplate class being used in the list.
    /// This means that there is one instance of this class for each T1,T2 combination used.
    /// </summary>
    public class NListingXXXClass : NValueClass {

        /// <summary>
        /// Initializes a new instance of the <see cref="NListingXXXClass" /> class.
        /// </summary>
        /// <param name="typename">The typename.</param>
        /// <param name="appType">Type of the app.</param>
        /// <param name="templateType">Type of the template.</param>
        /// <param name="template"></param>
        public NListingXXXClass(string typename, NClass appType, NClass templateType, Template template ) {
            //this.NTemplateClass.Template = template;            
            TypeName = typename;
            NApp = appType;
            NAppTemplate = templateType;
        }

        /// <summary>
        /// The type of the App
        /// </summary>
        public NClass NApp;

        /// <summary>
        /// The typeof the AppTemplate
        /// </summary>
        public NClass NAppTemplate;

     //   public NPredefinedClass NFixedSet;

        /// <summary>
        /// The type name
        /// </summary>
        public string TypeName;

        /// <summary>
        /// Gets the inherits.
        /// </summary>
        /// <value>The inherits.</value>
        /// <exception cref="System.NotImplementedException"></exception>
        public override string Inherits {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// Gets the name of the class.
        /// </summary>
        /// <value>The name of the class.</value>
        public override string ClassName {
            get {
                var sb = new StringBuilder();
                sb.Append(TypeName);
                sb.Append('<');
                sb.Append(NApp.FullClassName);
                if (NAppTemplate != null) {
                    sb.Append(", ");
                    sb.Append(NAppTemplate.FullClassName);
                }
                sb.Append('>');
                return sb.ToString();
            }
        }


    }
}
