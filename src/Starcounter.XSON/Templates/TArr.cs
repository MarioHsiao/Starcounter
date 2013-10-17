// ***********************************************************************
// <copyright file="TArr.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections;
using System.Collections.Generic;
using Starcounter.Advanced;
using Starcounter.Advanced.XSON;
using Starcounter.Internal.XSON;
using System.Reflection;

namespace Starcounter.Templates {

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="OT"></typeparam>
    public class TArray<OT> : TObjArr
        where OT : Json, new()
    {
        public override Type MetadataType {
            get { return typeof(ArrMetadata<OT,Json>); }
        }

        /// <summary>
        /// Creates the instance.
        /// </summary>
        /// <param name="parent">The parent.</param>
        /// <returns>System.Object.</returns>
        public override object CreateInstance(Json parent) {
            return new Arr<OT>((Json)parent, this);
        }

        /// <summary>
        /// The .NET type of the instance represented by this template.
        /// </summary>
        /// <value>The type of the instance.</value>
        public override System.Type InstanceType {
            get { return typeof(Arr<OT>); }
        }




    }

    /// <summary>
    /// Class TArr
    /// </summary>
    public class TObjArr : TContainer
#if IAPP
//        , ITObjArr
#endif
    {

        public override Type MetadataType {
            get { return typeof(ArrMetadata<Json, Json>); }
        }


        public override void ProcessInput(Json obj, byte[] rawValue) {
            throw new System.NotImplementedException();
        }

		public override bool IsArray {
			get {
				return true;
			}
		}

        /// <summary>
        /// Gets the children.
        /// </summary>
        /// <value>The children.</value>
        public override IEnumerable<Template> Children {
            get {
                return (IEnumerable<Template>)_Single;
            }
        }

        protected override IReadOnlyList<Internal.IReadOnlyTree> _Children {
            get { return _Single; }
        }


        /// <summary>
        /// 
        /// </summary>
        internal TObject[] _Single = new TObject[0];


       // internal DataValueBinding<IEnumerable> dataBinding;
        private string instanceDataTypeName;
    
        /// <summary>
        /// Gets or sets the type (the template) that should be the template for all elements
        /// in this array.
        /// Instructs the array what object template should be used for each element
        /// in this object array.
        /// </summary>
        /// <value>The obj template adhering to each element in this array</value>
        public TObject ElementType {
            get {
                if (_Single.Length == 0) {
                    return null;
                }
                return (TObject)_Single[0];
            }
            set {
                if (InstanceDataTypeName != null) {
                    value.InstanceDataTypeName = InstanceDataTypeName;
                }
                _Single = new TObject[1];
                _Single[0] = (TObject)value;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public string InstanceDataTypeName {
            get { return instanceDataTypeName; }
            set {
                var tj = ElementType;
                if (tj != null)
                    tj.InstanceDataTypeName = value;
                instanceDataTypeName = value;
            }
        }

        /// <summary>
        /// Contains the default value for the property represented by this
        /// Template for each new App object.
        /// </summary>
        /// <value>The default value as object.</value>
        /// <exception cref="System.NotImplementedException"></exception>
        public override object DefaultValueAsObject {
            get {
                throw new System.NotImplementedException();
            }
            set {
                throw new System.NotImplementedException();
            }
        }

        internal override bool UseBinding(IBindable data) {
			if (data == null)
				return false;
            return DataBindingFactory.VerifyOrCreateBinding(this, data);
        }

        internal override object GetBoundValueAsObject(Json obj) {
			return obj.GetBound(this);
        }

        internal override void SetBoundValueAsObject(Json obj, object value) {
			// TODO:
			// Check this. What do we set as bound value on an array? The IEnumerable?
			// or is the value we get the array itself?
			obj.SetBound(this, value as IEnumerable);
        }

        public override object CreateInstance(Json parent) {
            return new Json((Json)parent, this);
		}

		public override string ToJson(Json json) {
			throw new NotImplementedException();
		}

		public override byte[] ToJsonUtf8(Json json) {
			throw new NotImplementedException();
		}

		public override int ToJsonUtf8(Json json, out byte[] buffer) {
			throw new NotImplementedException();
		}

		public override void PopulateFromJson(Json json, string jsonStr) {
			throw new NotImplementedException();
		}

		public override int PopulateFromJson(Json json, IntPtr srcPtr, int srcSize) {
			throw new NotImplementedException();
		}

		public override int PopulateFromJson(Json json, byte[] src, int srcSize) {
			throw new NotImplementedException();
		}

		public override int ToFasterThanJson(Json json, out byte[] buffer) {
			throw new NotImplementedException();
		}

		public override int PopulateFromFasterThanJson(Json json, IntPtr srcPtr, int srcSize) {
			throw new NotImplementedException();
		}

        /// <summary>
        /// Autogenerates a template for a given data object given its (one dimensional) primitive fields and properties.
        /// This allows you to assign a SQL result to an expando like Json object without having defined
        /// any schema for the Json array.
        /// </summary>
        /// <param name="entity">An instance to create the template from</param>
        internal void CreateElementTypeFromDataObject(object entity) {
            ElementType = new TObject();
            var type = entity.GetType();
            var props = type.GetProperties(BindingFlags.Public|BindingFlags.Instance);
            foreach (var prop in props) {
                if (prop.CanRead) {
                    var pt = prop.PropertyType;
                    if (Template.IsSupportedType(pt)) {
                        ElementType.Add(pt, prop.Name);
                    }
                }
            }
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields) {
                var pt = field.FieldType;
                if (Template.IsSupportedType(pt)) {
                    ElementType.Add(pt, field.Name);
                }
            }

        }
    }
}
