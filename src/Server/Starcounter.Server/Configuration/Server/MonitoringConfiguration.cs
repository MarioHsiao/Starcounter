// ***********************************************************************
// <copyright file="MonitoringConfiguration.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Starcounter.Advanced.Configuration {
    /// <summary>
    /// Configures how the Starcounter server can start a database and if/how
    /// it should monitor its livetime.
    /// </summary>
    [XmlType("Monitoring", Namespace = ConfigurationElement.Namespace)]
    [Serializable]
    public class MonitoringConfiguration : ConfigurationElement {
        /// <summary>
        /// Name of the engine that should run the current engine.
        /// </summary>
        [DefaultValue(null)]
        public string Engine {
            get;
            set;
        }

        /// <summary>
        /// Maximum number of automatic restarts.
        /// </summary>
        /// <remarks><para>This counter is reset when the server is restarted, or when
        /// the database has been running successfully during the period defined by
        /// the <see cref="ResetFailureCountPeriod"/> property.</para>
        /// <para>After the maximum number of automatic restarts is reached, the
        /// database will simply not be restarted in case of failure.</para>
        /// </remarks>
        /// <seealso cref="ResetFailureCountPeriod"/>
        [DefaultValue(3)]
        public int? MaxRestartNumber {
            get;
            set;
        }

        /// <summary>
        /// Time span of successfull database execution, in minutes, after which
        /// the count of failures is reset.
        ///  </summary>
        /// <seealso cref="MaxRestartNumber"/>
        [DefaultValue(30)]
        public double? ResetFailureCountPeriod {
            get;
            set;
        }

        /// <summary>
        /// Time span, in seconds, measured from the moment that the communication
        /// with an database is broken, after which the OS process supporting an database
        /// should be killed in case the OS process did not spontaneously terminate.
        /// </summary>
        [DefaultValue(60)]
        public double? GracePeriodAfterConnectionLost {
            get;
            set;
        }

        /// <summary>
        /// Determines when the database should be started by the server.
        /// </summary>
        [DefaultValue(Configuration.StartupType.Automatic)]
        public StartupType? StartupType {
            get;
            set;
        }

        /// <summary>
        /// Determines when the database should be restarted in case of
        /// failure.
        /// </summary>
        [DefaultValue(Configuration.MonitoringType.RestartOnUnexpectedStop)]
        public MonitoringType? MonitoringType {
            get;
            set;
        }

        /// <summary>
        /// Returns a clone of the current <see cref="MonitoringConfiguration"/>.
        /// </summary>
        /// <returns>A clone of the current <see cref="MonitoringConfiguration"/>.</returns>
        public new MonitoringConfiguration Clone() {
            return (MonitoringConfiguration)base.Clone();
        }
    }

    /// <summary>
    /// Determines when servers should start databases.
    /// </summary>
    [XmlType("StartupType", Namespace = ConfigurationElement.Namespace)]
    public enum StartupType {
        /// <summary>
        /// The database should be started automatically upon server start up.
        /// </summary>
        Automatic,

        /// <summary>
        /// The database should not be started automatically, but may
        /// be started manually.
        /// </summary>
        Manual,

        /// <summary>
        /// The server may not start the database.
        /// </summary>
        Disable
    }

    /// <summary>
    /// Determines when the database should be restarted in case of failure.
    /// </summary>
    [XmlType("MonitoringType", Namespace = ConfigurationElement.Namespace)]
    public enum MonitoringType {
        /// <summary>
        /// Restart only when the database stopped unexpectedly, i.e.
        /// that restart unless a normal stopping request was issued
        /// using management APIes.
        /// </summary>
        RestartOnUnexpectedStop,

        /// <summary>
        /// Never restarts the database.
        /// </summary>
        Disabled
    }
}