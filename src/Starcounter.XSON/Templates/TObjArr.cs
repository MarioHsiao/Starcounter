// ***********************************************************************
// <copyright file="TArr.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Starcounter.XSON;

namespace Starcounter.Templates {
    /// <summary>
    /// 
    /// </summary>
    public class TObjArr : TContainer {
#if DEBUG
		internal string DebugBoundSetter;
		internal string DebugBoundGetter;
		internal string DebugUnboundSetter;
		internal string DebugUnboundGetter;
#endif

		public readonly Action<Json, Json> Setter;
		public readonly Func<Json, Json> Getter;
		internal Action<Json, IEnumerable> BoundSetter;
		internal Func<Json, IEnumerable> BoundGetter;
		internal Action<Json, Json> UnboundSetter;
		internal Func<Json, Json> UnboundGetter;
		
		/// <summary>
		/// 
		/// </summary>
		internal TObject[] _Single = new TObject[0];
		private string instanceDataTypeName;

		/// <summary>
		/// 
		/// </summary>
		public TObjArr() {
			Getter = BoundOrUnboundGet;
			Setter = BoundOrUnboundSet;
		}

		internal override void InvalidateBoundGetterAndSetter() {
			BoundGetter = null;
			BoundSetter = null;
			dataTypeForBinding = null;
		}

		internal override bool GenerateBoundGetterAndSetter(Json json) {
			TemplateDelegateGenerator.GenerateBoundDelegates(this, json);
			return (BoundGetter != null);
		}

		internal override void GenerateUnboundGetterAndSetter() {
			TemplateDelegateGenerator.GenerateUnboundDelegates(this, false);
		}

		internal override void Checkpoint(Json parent) {
			Json arr = UnboundGetter(parent);

			for (int i = 0; i < arr.Count; i++) {
				var row = (Json)arr._GetAt(i);
				row.CheckpointChangeLog();
				arr.CheckpointAt(i);
			}
			arr.ArrayAddsAndDeletes = null;
			arr._Dirty = false;
			base.Checkpoint(parent);
		}

		internal override void CheckAndSetBoundValue(Json parent, bool addToChangeLog) {
			throw new NotImplementedException();
		}

		internal override Json GetValue(Json parent) {
			return UnboundGetter(parent);
		}

		private Json BoundOrUnboundGet(Json json) {
			Json arr = UnboundGetter(json);

			if (UseBinding(json)) {
				var data = BoundGetter(json);
				arr.CheckBoundArray(data);
			}
			return arr;
		}

		private void BoundOrUnboundSet(Json parent, IEnumerable value) {
			Json arr = UnboundGetter(parent);
			if (UseBinding(parent)) {
				arr.CheckBoundArray(value);
			} else
				throw new NotSupportedException("TODO!");
		}

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
        /// 
        /// </summary>
        /// <value></value>
        public override IEnumerable<Template> Children {
            get {
                return (IEnumerable<Template>)_Single;
            }
        }

        protected override IReadOnlyList<Internal.IReadOnlyTree> _Children {
            get { return _Single; }
        }

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
