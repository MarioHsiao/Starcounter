// ***********************************************************************
// <copyright file="TString.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using Starcounter.Templates.Interfaces;
using System.Collections.Generic;

#if CLIENT
namespace Starcounter.Client.Template {
#else
namespace Starcounter.Templates {
#endif

    /// <summary>
    /// Class TString
    /// </summary>
    public class TString : TValue<string>
    {
        /// <summary>
        /// Processes the input.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="rawValue">The raw value.</param>
        public override void ProcessInput(App app, byte[] rawValue)
        {
            ProcessInput(app, System.Text.Encoding.UTF8.GetString(rawValue));
        }

        /// <summary>
        /// The _ default value
        /// </summary>
        private string _DefaultValue = "";

        /// <summary>
        /// Gets or sets the default value.
        /// </summary>
        /// <value>The default value.</value>
        public string DefaultValue {
            get { return _DefaultValue; }
            set { _DefaultValue = value; }
        }

        /// <summary>
        /// Contains the default value for the property represented by this
        /// Template for each new App object.
        /// </summary>
        /// <value>The default value as object.</value>
        public override object DefaultValueAsObject {
            get {
                return DefaultValue;
            }
            set {
                DefaultValue = (string)value;
            }
        }

        /// <summary>
        /// The .NET type of the instance represented by this template.
        /// </summary>
        /// <value>The type of the instance.</value>
        public override Type InstanceType {
            get { return typeof(string); }
        }
    }
}
