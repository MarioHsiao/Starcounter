﻿// ***********************************************************************
// <copyright file="DoubleProperty.cs" company="Starcounter AB">
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
    /// Class DoubleProperty
    /// </summary>
    public class DoubleProperty : Property
#if IAPP
        , IDoubleTemplate
#endif
    {
        /// <summary>
        /// The _ default value
        /// </summary>
       private double _DefaultValue = 0;

       /// <summary>
       /// Processes the input.
       /// </summary>
       /// <param name="app">The app.</param>
       /// <param name="rawValue">The raw value.</param>
       /// <exception cref="System.NotImplementedException"></exception>
       public override void ProcessInput(App app, byte[] rawValue)
       {
           throw new NotImplementedException();
       }

       /// <summary>
       /// Gets or sets the default value.
       /// </summary>
       /// <value>The default value.</value>
        public double DefaultValue {
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
                DefaultValue = (double)value;
            }
        }

        /// <summary>
        /// The .NET type of the instance represented by this template.
        /// </summary>
        /// <value>The type of the instance.</value>
        public override Type InstanceType {
            get { return typeof(double); }
        }
    }
}
