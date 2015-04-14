﻿// ***********************************************************************
// <copyright file="Template.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Internal;
using System;
using System.Collections.Generic;
using Starcounter.Advanced.XSON;
using Starcounter.Internal.XSON.Modules;

namespace Starcounter.Templates {
    /// <summary>
    /// A template describes an App or a property of an App. A tree of
    /// templates defines the schema of an App.
    /// </summary>
    public abstract partial class Template : IReadOnlyTree {
        private bool _dynamic;
        private string _className;
        private string _name;
        private string _propertyName;
        private bool _sealed;
        internal TContainer _parent;

        private static readonly IReadOnlyList<IReadOnlyTree> _emptyList = new List<IReadOnlyTree>();

        /// <summary>
        /// Dictionary used to hold values gathered when json is parsed from file and during codegeneration.
        /// When using templates in applications this dictionary should never be used or instantiated.
        /// </summary>
        private Dictionary<string, string> codegenMetadata;
        internal CompilerOrigin CompilerOrigin = new CompilerOrigin();

        /// <summary>
        /// Initializes a new instance of the <see cref="Template" /> class.
        /// </summary>
        public Template() {
            Editable = false;
            TemplateIndex = -1;
            _dynamic = false;
        }

        internal Dictionary<string, string> CodegenMetadata {
            get {
                if (codegenMetadata == null)
                    codegenMetadata = new Dictionary<string, string>();
                return codegenMetadata;
            }
        }

        internal string GetCodegenMetadata(string key) {
            string value = null;
            if (codegenMetadata != null)
                codegenMetadata.TryGetValue(key, out value);
            return value;
        }

        /// <summary>
        /// If this template is based on a template in a baseclass, i.e. inherited, this property
        /// is set to the baseclass template.
        /// </summary>
        internal Template BasedOn { get; set; }

        /// <summary>
        /// Returns true if this object support expando like (Javascript like) behaviour that
        /// lets you create properties without a preexisting schema.
        /// </summary>
        public bool IsDynamic {
            get { return _dynamic; }
            set { _dynamic = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual bool IsArray {
            get { return false; }
        }

        /// <summary>
        /// Gets or sets the name of the class.
        /// </summary>
        /// <value>The name of the class.</value>
        public string ClassName {
            get { return _className; }
            set { _className = value; }
        }
        /// <summary>
        /// Gets or sets the namespace.
        /// </summary>
        /// <value></value>
        public string Namespace { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public abstract bool IsPrimitive { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString() {
            return DebugString;
        }

        /// <summary>
        /// 
        /// </summary>
        public abstract Type MetadataType { get; }

        /// <summary>
        /// Gets the type of the json.
        /// </summary>
        /// <value>The type of the json.</value>
        public virtual string JsonType {
            get {
                if (InstanceType != null)
                    return InstanceType.Name;
                return null;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is visible on client.
        /// </summary>
        /// <value><c>true</c> if this instance is visible on client; otherwise, <c>false</c>.</value>
        public virtual bool IsVisibleOnClient { get { return true; } }

        /// <summary>
        /// Gets a value indicating whether this instance has instance value on client.
        /// </summary>
        /// <value><c>true</c> if this instance has instance value on client; otherwise, <c>false</c>.</value>
        public abstract bool HasInstanceValueOnClient { get; }

        /// <summary>
        /// Gets a value indicating whether this instance has default properties on client.
        /// </summary>
        /// <value><c>true</c> if this instance has default properties on client; otherwise, <c>false</c>.</value>
        public virtual bool HasDefaultPropertiesOnClient { get { return Editable; } }

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
        /// The .NET type of the instance represented by this template.TApp
        /// </summary>
        /// <value>The type of the instance.</value>
        /// <exception cref="System.Exception">You are not allowed to set the InstanceType of a </exception>
        public virtual Type InstanceType {
            get { return null; }
            set { throw new Exception("You are not allowed to set the InstanceType of a " + this.GetType().Name + "."); }
        }

        /// <summary>
        /// All templates other than the Root template has a parent template. For
        /// properties, the parent template is the TApp. For App elements
        /// in an array (a list), the parent is the TObjArr.
        /// </summary>
        /// <value>The parent.</value>
        public TContainer Parent {
            get {
                return _parent;
            }
            set {
                if (value is TObject) {
                    var at = (TObject)value;
                    at.Properties.Add(this);
                }
                _parent = (TContainer)value;
            }
        }

        /// <summary>
        /// Each template with a parent has an internal position amongst its siblings
        /// </summary>
        /// <value>The index.</value>
        public int TemplateIndex { get; internal set; }

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
        public bool Sealed {
            get {
                if (Parent != null) {
                    return Parent.Sealed;
                }
                return _sealed;
            }
            internal set {
                if (_sealed == true) {
                    // TODO! SCERR!
                    throw new Exception("Once a TContainer (Obj or Arr schema) is in use (have instances), you cannot modify it");
                }
                _sealed = value;
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
                return _name;
            }
            set {
                if (_name != null && TemplateName != value)
                    throw new Exception("Once the TemplateName is set, it cannot be changed");
                _name = value;
                if (PropertyName == null) {
                    string name = value.Replace("$", "");
                    _propertyName = name;
                    if (Parent != null) {
                        var parent = (TObject)Parent;
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
            get {
                if (_propertyName == null) {
                    var p = Parent;
                    if (p != null && p is TObjArr) {
                        return Parent.PropertyName + "Element";
                    }
                }
                return _propertyName;
            }
        }

        /// <summary>
        /// The property name including parent path
        /// </summary>
        public string DebugString {
            get {
                return HelperFunctions.GetClassDeclarationSyntax(this.GetType()) + " " + DebugPropertyNameWithPathSuffix;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        private string DebugPropertyNameWithPathSuffix {
            get {
                var str = "";
                if (Parent != null) {
                    str += Parent.DebugPropertyNameWithPathSuffix + ".";
                }
                if (Parent is TObjArr) {
                    str += "ElementType.";
                } else {
                    if (PropertyName != null) {
                        str += PropertyName;
                    } else if (ClassName != null) {
                        str += this.ClassName;
                    } else {
                        str += "(anonymous)";
                    }
                }
                return str; // +" #" + this.GetHashCode();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="toTemplate"></param>
        public virtual void CopyTo(Template toTemplate) {
            toTemplate._name = _name;
            toTemplate._propertyName = _propertyName;
            toTemplate.Editable = Editable;
            toTemplate.Enabled = Enabled;
            toTemplate.Visible = Visible;
        }

#if DEBUG
        internal void VerifyProperty(Template prop) {
            if (this != prop.Parent) {
                string parentString;
                if (prop.Parent == null) {
                    parentString = "as a parentless " + prop.GetType();
                } else {
                    parentString = "in " + prop.Parent.DebugString;
                }
                throw new Exception(String.Format(
                    "The property {0} is declared {1} but an attempt was made to use it in {2}",
                    prop.DebugString,
                    parentString,
                    this.DebugString));
            }

        }
#endif

        IReadOnlyTree IReadOnlyTree.Parent { get { return _parent; } }
        IReadOnlyList<IReadOnlyTree> IReadOnlyTree.Children { get { return _Children; } }
        protected virtual IReadOnlyList<IReadOnlyTree> _Children { get { return _emptyList; } }

        protected static bool IsSupportedType(Type pt) {
            Func<TObject, string, TValue> dummy;
            return (TObject.@switch.TryGetValue(pt, out dummy));
        }

        internal static Template CreateFromMarkup(string json) {
            IXsonTemplateMarkupReader reader;
            string format = "json";

            if (Starcounter_XSON.JsonByExample.MarkupReaders.TryGetValue(format, out reader))
                return reader.CompileMarkup<Json, TValue>(json, null);
            
            throw new Exception(String.Format("Cannot create an XSON template. No markup compiler is registred for the format {0}.", format));
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public struct CompilerOrigin {
        public string FileName;
        public int LineNo;
        public int ColNo;
    }
}
