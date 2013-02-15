// ***********************************************************************
// <copyright file="TBool.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using Starcounter.Templates.Interfaces;
#if CLIENT
namespace Starcounter.Client.Template {
#else
namespace Starcounter.Templates {
#endif
    /// <summary>
    /// Defines a boolean property in an App object.
    /// </summary>
    public class TBool : TValue<bool>
    {
        /// <summary>
        /// The _ default value
        /// </summary>
        private bool _DefaultValue = false;

        /// <summary>
        /// Processes the input.
        /// </summary>
        /// <param name="app">The app.</param>
        /// <param name="rawValue">The raw value.</param>
        public override void ProcessInput(Puppet app, byte[] rawValue)
        {
            // TODO:
            // Proper implementation.
            if (rawValue != null && rawValue.Length == 4)
                ProcessInput(app, true);
            else
                ProcessInput(app, false);
        }

        /// <summary>
        /// The default value for a boolean property is false. For the
        /// property defined by this template, you can set an alternative
        /// default value (i.e. true).
        /// </summary>
        /// <value><c>true</c> if [default value]; otherwise, <c>false</c>.</value>
        public bool DefaultValue {
            get { return _DefaultValue; }
            set { _DefaultValue = value; }
        }

        /// <summary>
        /// Will return a boxed version of the DefaultValue property.
        /// </summary>
        /// <value>The default value as object.</value>
        public override object DefaultValueAsObject {
            get {
                return DefaultValue;
            }
            set {
                DefaultValue = (bool)value;
            }
        }

        /// <summary>
        /// Will return the Boolean runtime type
        /// </summary>
        /// <value>The type of the instance.</value>
        public override Type InstanceType {
            get { return typeof(bool); }
        }
    }
}
