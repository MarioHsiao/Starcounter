// ***********************************************************************
// <copyright file="TraceSourceConfiguration.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Internal;
using System;
using System.Xml.Serialization;

namespace Starcounter.Advanced.Configuration {
    /// <summary>
    /// Configures the severity threshold of a trace source.
    /// </summary>
    [XmlType("TraceSource", Namespace = ConfigurationElement.Namespace)]
    [Serializable]
    public sealed class TraceSourceConfiguration : ConfigurationElement {
        /// <summary>
        /// Minimal severity that needs to be logged for the given source.
        /// </summary>
        public Severity Severity {
            get;
            set;
        }
    }
}