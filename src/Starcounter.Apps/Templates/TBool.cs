// ***********************************************************************
// <copyright file="TBool.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;

namespace Starcounter.Templates {

    /// <summary>
    /// Defines a boolean property in an App object.
    /// </summary>
    public class TBool : TValue<bool>
    {
        /// <summary>
        /// The _ default value
        /// </summary>
        private bool _DefaultValue = false;

        internal override void ProcessInput(Obj obj, byte[] rawValue)
        {
            // TODO:
            // Proper implementation.
            if (rawValue != null && rawValue.Length == 4)
                obj.ProcessInput<bool>(this, true);
            else
                obj.ProcessInput<bool>(this, false);
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
