// ***********************************************************************
// <copyright file="Template.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Starcounter.Internal.Application.CodeGeneration")]

#if CLIENT
namespace Starcounter.Client.Template {
#else
namespace Starcounter.Templates {
#endif

    /// <summary>
    /// A template describes an App or a property of an App. A tree of
    /// templates defines the schema of an App.
    /// </summary>
    public abstract class Template 
    {
        /// <summary>
        /// Gets the type of the json.
        /// </summary>
        /// <value>The type of the json.</value>
        public virtual string JsonType {
            get {
                return InstanceType.Name;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is visible on client.
        /// </summary>
        /// <value><c>true</c> if this instance is visible on client; otherwise, <c>false</c>.</value>
        public virtual bool IsVisibleOnClient {
            get {
                return true;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance has instance value on client.
        /// </summary>
        /// <value><c>true</c> if this instance has instance value on client; otherwise, <c>false</c>.</value>
        public abstract bool HasInstanceValueOnClient { get; }

        /// <summary>
        /// Gets a value indicating whether this instance has default properties on client.
        /// </summary>
        /// <value><c>true</c> if this instance has default properties on client; otherwise, <c>false</c>.</value>
        public virtual bool HasDefaultPropertiesOnClient {
            get {
                return Editable;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="TValue" /> is editable.
        /// </summary>
        /// <value><c>true</c> if editable; otherwise, <c>false</c>.</value>
        public bool Editable { get; set; }

        /// <summary>
        /// Gets or sets the on update.
        /// </summary>
        /// <value>The on update.</value>
        public string OnUpdate { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Template" /> class.
        /// </summary>
		public Template()
		{
			Editable = false;
		}

//        public abstract object CreateInstance( IParent parent );
        /// <summary>
        /// The .NET type of the instance represented by this template.TApp
        /// </summary>
        /// <value>The type of the instance.</value>
        /// <exception cref="System.Exception">You are not allowed to set the InstanceType of a </exception>
        public virtual Type InstanceType {
            get { return null; }
            set {
                throw new Exception("You are not allowed to set the InstanceType of a " + this.GetType().Name + "." );
            }
        }

        /// <summary>
        /// The _ parent
        /// </summary>
        internal TContainer _Parent;

        /// <summary>
        /// All templates other than the Root template has a parent template. For
        /// properties, the parent template is the TApp. For App elements
        /// in an array (a list), the parent is the TObjArr.
        /// </summary>
        /// <value>The parent.</value>
        public TContainer Parent {
            get {
                return _Parent;
            }
            set {
                if (value is TObj) {
                    var at = (TObj)value;
                    at.Properties.Add(this);
                }
                _Parent = (TContainer)value;
            }
        }

        /// <summary>
        /// Each template with a parent has an internal position amongst its siblings
        /// </summary>
        /// <value>The index.</value>
        public int TemplateIndex { get; internal set; }

        /// <summary>
        /// The _ name
        /// </summary>
        private string _Name;

        /// <summary>
        /// The _ property name
        /// </summary>
        private string _PropertyName;

        /// <summary>
        /// The compiler origin
        /// </summary>
        public CompilerOrigin CompilerOrigin = new CompilerOrigin();

        /// <summary>
        /// Tells if the property or child should be sent to the client.
        /// </summary>
        /// <value><c>true</c> if visible; otherwise, <c>false</c>.</value>
        public bool Visible { get; set; }

        /// <summary>
        /// Tells if the property or child should be enabled on client. Disabled means that
        /// it or its children is not editable or invokable.
        /// </summary>
        /// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
        public bool Enabled { get; set; }

        /// <summary>
        /// If this property returns true, you are not allowed to alter the properties of this template.
        /// </summary>
        /// <value><c>true</c> if sealed; otherwise, <c>false</c>.</value>
        /// <exception cref="System.Exception">You are not allowed to set the IsSealed value</exception>
        public virtual bool Sealed {
            get {
                if (Parent == null || !Parent.Sealed) {
                    return false;
                }
                return true;
            }
            internal set {
                throw new Exception("You are not allowed to set the IsSealed value");
            }
        }

        /// <summary>
        /// The Name of the property in the parent App. The allowed characters are the same as in JSON.
        /// See www.json.org.
        /// </summary>
        /// <value>The name.</value>
        /// <exception cref="System.Exception">Once the Name is set, it cannot be changed</exception>
        public string TemplateName {
            get {
                return _Name;
            }
            set {
                if (_Name != null && TemplateName != value)
                    throw new Exception("Once the TemplateName is set, it cannot be changed");
                _Name = value;
                if (PropertyName == null) {
                    string name = value.Replace("$", "");
                    _PropertyName = name;
                    if (Parent != null) {
                        var parent = (TObj)Parent;
                        var props = (PropertyList)(parent.Properties);
                        props.ChildPropertyNameIsSet(this);
                        props.ChildNameIsSet(this);
                    }
                }   
            }
        }

        /// <summary>
        /// As the Name of the property represented by this Template may contain characters that are not legal
        /// in a .NET property, this property contains a sanitized (or a completely different) name that is used
        /// as the property identifier in the App.
        /// </summary>
        /// <value>The name of the property.</value>
        public string PropertyName {
            get { return _PropertyName; }
        }

        /// <summary>
        /// Contains the default value for the property represented by this
        /// Template for each new App object.
        /// </summary>
        /// <value>The default value as object.</value>
        /// <exception cref="System.NotImplementedException">This template </exception>
        public virtual object DefaultValueAsObject {
            get {
                return null;
            }
            set {
                throw new NotImplementedException("This template " + GetType().FullName + " does not implement DefaultValueAsObject" );
            }
        }

        /// <summary>
        /// Gets the instance.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <returns>System.Object.</returns>
        public object GetInstance(Container parent) {
            return this.CreateInstance(parent);
        }

        /// <summary>
        /// Creates the instance.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <returns>System.Object.</returns>
        public virtual object CreateInstance(Container parent) {
            return DefaultValueAsObject;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public object CreateInstance() {
            return CreateInstance(null);
        }

        /// <summary>
        /// Copies to.
        /// </summary>
        /// <param name="toTemplate">To template.</param>
        public virtual void CopyTo(Template toTemplate)
        {
            toTemplate._Name = _Name;
            toTemplate._PropertyName = _PropertyName;
            toTemplate.Editable = Editable;
            toTemplate.Enabled = Enabled;
            toTemplate.Visible = Visible;
        }
    }

    /// <summary>
    /// Struct CompilerOrigin
    /// </summary>
    public struct CompilerOrigin {
        /// <summary>
        /// The file name
        /// </summary>
        public string FileName;
        /// <summary>
        /// The line no
        /// </summary>
        public int LineNo;
        /// <summary>
        /// The col no
        /// </summary>
        public int ColNo;
    }
}
