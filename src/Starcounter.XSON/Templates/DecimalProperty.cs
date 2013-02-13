// ***********************************************************************
// <copyright file="DecimalProperty.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Text;
using Starcounter.Templates.Interfaces;
#if CLIENT
namespace Starcounter.Client.Template {
#else
namespace Starcounter.Templates {
#endif

    /// <summary>
    /// Class DecimalProperty
    /// </summary>
    public class DecimalProperty : Property<decimal>
    {
        /// <summary>
        /// The _ default value
        /// </summary>
        decimal _DefaultValue = 0;

        /// <summary>
        /// Processes the input.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="rawValue">The raw value.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        public override void ProcessInput(App app, byte[] rawValue)
        {
            // TODO:
            // Superslow way of parsing the decimal value. Needs to be rewritten.
            decimal value;
            decimal.TryParse(Encoding.UTF8.GetString(rawValue), out value);
            ProcessInput(app, value);
        }

        /// <summary>
        /// Gets or sets the default value.
        /// </summary>
        /// <value>The default value.</value>
        public decimal DefaultValue {
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
                DefaultValue = (decimal)value;
            }
        }
        /// <summary>
        /// The .NET type of the instance represented by this template.
        /// </summary>
        /// <value>The type of the instance.</value>
        public override Type InstanceType {
            get { return typeof(decimal); }
        }
    }
}
