

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Starcounter.Templates.Interfaces;

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
#if IAPP
        : ITemplate, IStatefullTemplate 
#endif
    {
        public virtual string JsonType {
            get {
                return InstanceType.Name;
            }
        }

        public virtual bool IsVisibleOnClient {
            get {
                return true;
            }
        }

        public abstract bool HasInstanceValueOnClient { get; }
        public virtual bool HasDefaultPropertiesOnClient {
            get {
                return Editable;
            }
        }

        public bool Editable { get; set; }
        public bool Bound { get; set; }
        public string OnUpdate { get; set; }
        public string Bind { get; set; }

		public Template()
		{
			Editable = false;
		}

//        public abstract object CreateInstance( IParent parent );
        public virtual Type InstanceType {
            get { return null; }
            set {
                throw new Exception("You are not allowed to set the InstanceType of a " + this.GetType().Name + "." );
            }
        }

        internal ParentTemplate _Parent;

        /// <summary>
        /// All templates other than the Root template has a parent template. For
        /// properties, the parent template is the AppTemplate. For App elements
        /// in an array (a list), the parent is the AppListTemplate.
        /// </summary>
        public IParentTemplate Parent {
            get {
                return _Parent;
            }
            set {
                if (value is AppTemplate) {
                    var at = (AppTemplate)value;
                    at.Properties.Add(this);
                }
                _Parent = (ParentTemplate)value;
            }
        }

        public int Index { get; internal set; }

        private string _Name;
        private string _PropertyName;

        public CompilerOrigin CompilerOrigin = new CompilerOrigin();
 
        /// <summary>
        /// Tells if the property or child should be sent to the client.
        /// </summary>
        public bool Visible { get; set; }

        /// <summary>
        /// Tells if the property or child should be enabled on client. Disabled means that
        /// it or its children is not editable or invokable.
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// If this property returns true, you are not allowed to alter the properties of this template.
        /// </summary>
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
        public string Name {
            get {
                return _Name;
            }
            set {
                if (_Name != null && Name != value)
                    throw new Exception("Once the Name is set, it cannot be changed");
                _Name = value;
                if (PropertyName == null) {
                    string name = value.Replace("$", "");
                    PropertyName = name;
                    if (Parent != null) {
                        var parent = (AppTemplate)Parent;
                        var props = (PropertyList)(parent.Properties);
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
        public string PropertyName {
            get {
                return _PropertyName;
            }
            set {
                if (_PropertyName != null)
                    throw new Exception("Once the PropertyName is set, it cannot be changed");
                _PropertyName = value;
                if (Parent != null ) {
                    var parent = (AppTemplate)Parent;
                    var props = (PropertyList)(parent.Properties);
                    props.ChildPropertyNameIsSet(this);
                }
            }
        }

        /// <summary>
        /// Contains the default value for the property represented by this
        /// Template for each new App object.
        /// </summary>
        public virtual object DefaultValueAsObject {
            get {
                return null;
            }
            set {
                throw new NotImplementedException("This template " + GetType().FullName + " does not implement DefaultValueAsObject" );
            }
        }

        public object GetInstance(AppNode parent) {
            return this.CreateInstance(parent);
        }

        public virtual object CreateInstance(AppNode parent) {
            return DefaultValueAsObject;
        }

        object IStatefullTemplate.CreateInstance(IAppNode parent) {
            return CreateInstance( (AppNode)parent );
        }

        public virtual void CopyTo(Template toTemplate)
        {
            toTemplate._Name = _Name;
            toTemplate._PropertyName = _PropertyName;
            toTemplate.Bind = Bind;
            toTemplate.Bound = Bound;
            toTemplate.Editable = Editable;
            toTemplate.Enabled = Enabled;
            toTemplate.Visible = Visible;
        }
    }

    public struct CompilerOrigin {
        public string FileName;
        public int LineNo;
        public int ColNo;
    }



}
