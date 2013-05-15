// ***********************************************************************
// <copyright file="NArr.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Templates;
using System;
using System.Collections.Generic;
using System.Text;
namespace Starcounter.Internal.Application.CodeGeneration {

    /// <summary>
    /// The source code representation of each Arr&lt;T1&gt;, TArr&lt;T1,T2&gt; 
    /// or ArrMetadata&lt;T1,T2&gt; class where 
    /// T1 is the link to the App class and T2 is the link to the TApp class being used in the list.
    /// This means that there is one instance of this class for each T1,T2 combination used.
    /// </summary>
    public class NArrXXXClass : NValueClass {

        /// <summary>
        /// 
        /// </summary>
        /// <param name="gen"></param>
        /// <param name="typename"></param>
        /// <param name="appType"></param>
        /// <param name="templateType"></param>
        /// <param name="template"></param>
        public NArrXXXClass(DomGenerator gen, string typename, NClass appType, NClass templateType, Template template ) 
        :base( gen)
        {
            //this.NTemplateClass.Template = template;            
            TypeName = typename;
            NApp = appType;
            NTApp = templateType;
        }

        /// <summary>
        /// The type of the App
        /// </summary>
        public NClass NApp;

        /// <summary>
        /// The typeof the TApp
        /// </summary>
        public NClass NTApp;

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
                if (NTApp != null) {
                    sb.Append(", ");
                    sb.Append(NTApp.FullClassName);
                }
                sb.Append('>');
                return sb.ToString();
            }
        }


    }
}
