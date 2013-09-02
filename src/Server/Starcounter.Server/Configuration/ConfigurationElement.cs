// ***********************************************************************
// <copyright file="ConfigurationElement.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.ComponentModel;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;

namespace Starcounter.Advanced.Configuration {
    /// <summary>
    /// Base of all configuration elements.
    /// </summary>
    [XmlType(Namespace = ConfigurationElement.Namespace)]
    [Serializable]
    public abstract class ConfigurationElement : IConfigurationElement, INotifyPropertyChanged {
        private string _role;

        /// <summary>
        /// Namespace of elements defined in the current assembly.
        /// </summary>
        public const string Namespace = "http://schemas.starcounter.com/configuration";

        internal IConfigurationElement Parent {
            get;
            private set;
        }

        /// <inheritdoc />
        IConfigurationElement IConfigurationElement.Parent {
            get {
                return Parent;
            }
        }

        /// <inheritdoc />
        string IConfigurationElement.Role {
            get {
                return _role;
            }
        }

        #region IConfigurationElement Members

        /// <inheritdoc />
        void IConfigurationElement.SetParent(IConfigurationElement parent, string role) {
            Parent = parent;
            _role = role;
            foreach (PropertyInfo property in this.GetType().GetProperties()) {
                if (property.GetGetMethod().IsStatic) {
                    return;
                }
                object value = property.GetValue(this, null);
                IConfigurationElement valueAsConfigurationElement = value as IConfigurationElement;
                if (valueAsConfigurationElement != null) {
                    valueAsConfigurationElement.SetParent(this, property.Name);
                }
            }
        }

        #endregion

        /// <summary>
        /// Sets the role of the current element in its parent.
        /// </summary>
        /// <param name="role">Role.</param>
        protected virtual void SetRole(string role) {
            _role = role;
        }

        /// <summary>
        /// Gets key uniquely identifying the current element.
        /// </summary>
        /// <returns>A key uniquely identifying the current element,
        /// composed recursively from its parents.</returns>
        public virtual string GetKeyPath() {
            IConfigurationElement parent = ((IConfigurationElement)this).Parent;
            if (parent == null) {
                return "/";
            } else {
                return parent.GetKeyPath() + "/" + ((IConfigurationElement)this).Role;
            }
        }

        /// <inheritdoc />
        public object Clone() {
            return ((IConfigurationElement)this).Clone(null);
        }

        IConfigurationElement IConfigurationElement.Clone(IConfigurationElement newParent) {
            ConfigurationElement clone = (ConfigurationElement)this.MemberwiseClone();
            clone.Parent = newParent;
            this.CopyTo(clone);
            return clone;
        }

        /// <inheritdoc />
        protected virtual void CopyTo(ConfigurationElement clone) {
            foreach (PropertyInfo property in this.GetType().GetProperties()) {
                if (!property.CanWrite || property.GetSetMethod().IsStatic) {
                    continue;
                }
                ICloneable value = property.GetValue(this, null) as ICloneable;
                if (value != null) {
                    IConfigurationElement configurationElement = value as IConfigurationElement;
                    object clonedValue = configurationElement != null
                                         ? configurationElement.Clone(clone)
                                         : value.Clone();
                    property.SetValue(clone, clonedValue, null);
                }
            }
        }

        /// <inheritdoc />
        public override string ToString() {
            return this.GetType().Name + ":" + this.GetKeyPath();
        }

        #region INotifyPropertyChanged Members

        /// <summary>
        /// Occurs when [property changed].
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Called when [property changed].
        /// </summary>
        /// <param name="fieldName">Name of the field.</param>
        protected virtual void OnPropertyChanged(string fieldName) {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(fieldName));
            }
        }
        #endregion
    }
}