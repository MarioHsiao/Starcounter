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
using System.Text;

namespace Starcounter.Templates {
    /// <summary>
    /// 
    /// </summary>
    public class TObjArr : TContainer {
        // When a custom setter is used we need to add logic for setting parent.
        // The original setter is saved here, and the setter with added code (which uses)
        // this one is set as UnboundSetter (and maybe Setter)
        private Action<Json, Json> customSetter;

		public Action<Json, Json> Setter;
		public Func<Json, Json> Getter;
		internal Action<Json, IEnumerable> BoundSetter;
		internal Func<Json, IEnumerable> BoundGetter;
		internal Action<Json, Json> UnboundSetter;
		internal Func<Json, Json> UnboundGetter;
        private Func<TObjArr, TObject> getElementType = null;
		private TObject[] single = new TObject[0];
      
		/// <summary>
		/// 
		/// </summary>
		public TObjArr() {
			Getter = BoundOrUnboundGet;
			Setter = BoundOrUnboundSet;
		}

        private void SetParentAndUseCustomSetter(Json parent, Json value) {
            UpdateParentAndIndex(parent, value);
            customSetter(parent, value);
        }

        public void SetCustomGetElementType(Func<TObjArr, TObject> getElementType) {
            ElementType = null;
            this.getElementType = getElementType;
        }
        
		/// <summary>
		/// Sets the getter and setter delegates for unbound values to the submitted delegates.
		/// </summary>
		/// <param name="getter"></param>
		/// <param name="setter"></param>
		/// <param name="overwriteExisting">
		/// If false the new delegates are only set if the current delegates are null.
		/// </param>
		public void SetCustomAccessors(Func<Json, Json> getter, 
									   Action<Json, Json> setter,
									   bool overwriteExisting = true) {
            customSetter = setter;
			bool overwrite = (overwriteExisting || !hasCustomAccessors);

			if (BindingStrategy == BindingStrategy.Unbound) {
				if (overwrite || Getter == null)
					Getter = getter;
				if (overwrite || Setter == null)
					Setter = SetParentAndUseCustomSetter;
			}

			if (overwrite || UnboundGetter == null) {
				UnboundGetter = getter;
#if DEBUG
				DebugUnboundGetter = "<custom>";
#endif
			}

			if (overwrite || UnboundSetter == null) {
				UnboundSetter = SetParentAndUseCustomSetter;
#if DEBUG
				DebugUnboundSetter = "<custom>";
#endif
			}

			hasCustomAccessors = true;
		}

		internal override void CopyValueDelegates(Template toTemplate) {
			var p = toTemplate as TObjArr;
			if (p != null) {
                p.customSetter = customSetter;
				p.UnboundGetter = UnboundGetter;
				p.UnboundSetter = UnboundSetter;
				p.hasCustomAccessors = hasCustomAccessors;
#if DEBUG
				DebugUnboundGetter = DebugUnboundGetter;
				DebugUnboundSetter = DebugUnboundSetter;
#endif
			}
		}

		internal override void SetDefaultValue(Json parent) {
			UnboundSetter(parent, new Arr<Json>(parent, this));
		}

		internal override void InvalidateBoundGetterAndSetter() {
			BoundGetter = null;
			BoundSetter = null;
			base.InvalidateBoundGetterAndSetter();
		}

		internal override bool GenerateBoundGetterAndSetter(Json json) {
			TemplateDelegateGenerator.GenerateBoundDelegates(this, json);
			return (BoundGetter != null);
		}

		internal override void GenerateUnboundGetterAndSetter() {
			if (UnboundGetter == null)
				TemplateDelegateGenerator.GenerateUnboundDelegates(this, false);
		}

		internal override void Checkpoint(Json parent) {
			Json arr = UnboundGetter(parent);

			for (int i = 0; i < ((IList)arr).Count; i++) {
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
			var arr = UnboundGetter(parent);

			if (parent._dirtyCheckEnabled && UseBinding(parent)) {
				arr.CheckBoundArray(BoundGetter(parent));	
			}

			return arr;
		}

		internal void SetValue(Json parent, IEnumerable value) {
			Json newArr = (Json)CreateInstance(parent);
			Json current = UnboundGetter(parent);

			if (current != null) {
				current.InternalClear();
				current.SetParent(null);
			}
			newArr._PendingEnumeration = true;
			newArr._data = value;
			newArr.Array_InitializeAfterImplicitConversion(parent, this);

			if (UseBinding(parent))
				BoundSetter(parent, value);
			UnboundSetter(parent, newArr);

			if (parent.HasBeenSent)
				parent.MarkAsReplaced(TemplateIndex);

			parent.CallHasChanged(this);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="parent"></param>
		/// <returns></returns>
		internal override object GetUnboundValueAsObject(Json parent) {
			return UnboundGetter(parent);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="parent"></param>
		/// <returns></returns>
		internal override object GetValueAsObject(Json parent) {
			return Getter(parent);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="value"></param>
		internal override void SetValueAsObject(Json parent, object value) {
			Setter(parent, (Json)value);
		}

		private Json BoundOrUnboundGet(Json parent) {
			Json arr = UnboundGetter(parent);

			if (parent._dirtyCheckEnabled && UseBinding(parent)) {
				var data = BoundGetter(parent);
				arr.CheckBoundArray(data);
			}
			return arr;
		}

		private void BoundOrUnboundSet(Json parent, Json value) {
			if (UseBinding(parent)) {
				BoundSetter(parent, (IEnumerable)value.Data);
			}
			UnboundSetter(parent, value);

			if (value._PendingEnumeration) {
				value.Array_InitializeAfterImplicitConversion(parent, this);
			}

			if (parent.HasBeenSent)
				parent.MarkAsReplaced(TemplateIndex);

			parent.CallHasChanged(this);
		}

        public override Type MetadataType {
            get { return typeof(ArrMetadata<Json, Json>); }
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
                return (IEnumerable<Template>)single;
            }
        }

        protected override IReadOnlyList<Internal.IReadOnlyTree> _Children {
            get { return single; }
        }

        private object elementLockObject = new object();

        /// <summary>
        /// Gets or sets the type (the template) that should be the template for all elements
        /// in this array.
        /// Instructs the array what object template should be used for each element
        /// in this object array.
        /// </summary>
        /// <value>The obj template adhering to each element in this array</value>
        public TObject ElementType {
            get {
                if (single.Length != 0)
                    return single[0];

                if (getElementType == null) 
                    return null;

                // Quick temporary hack for removing synchronization issue fopr one specific case.
                // Needs to be solved properly. #2597
                lock (elementLockObject) {
                    if (single.Length == 0)
                        ElementType = getElementType(this);
                }
                return single[0];
            }
            set {
                if (value != null) {
                    // TODO:
                    // Check why this is needed (or if it is needed).
                    //if (InstanceDataTypeName != null) {
                    //    value.InstanceDataTypeName = InstanceDataTypeName;
                    //}

                    single = new TObject[1];
                    single[0] = (TObject)value;
                } else {
                    single = new TObject[0];
                }
            }
        }

        public virtual Json CreateInstance(Json parent) {
            return new Arr<Json>(parent, this);
		}

        /// <summary>
        /// 
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public override string ToJson(Json json) {
            byte[] buffer = new byte[JsonSerializer.EstimateSizeBytes(json)];
            int count = ToJsonUtf8(json, buffer, 0);
            return Encoding.UTF8.GetString(buffer, 0, count);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public override byte[] ToJsonUtf8(Json json) {
            byte[] buffer = new byte[JsonSerializer.EstimateSizeBytes(json)];
            int count = ToJsonUtf8(json, buffer, 0);

            // Checking if we have to shrink the buffer.
            if (count != buffer.Length) {
                byte[] sizedBuffer = new byte[count];
                Buffer.BlockCopy(buffer, 0, sizedBuffer, 0, count);
                return sizedBuffer;
            }
            return buffer;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="json"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public override int ToJsonUtf8(Json json, byte[] buffer, int offset) {
            return JsonSerializer.Serialize(json, buffer, offset);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="json"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public override int ToFasterThanJson(Json json, byte[] buffer, int offset) {
            return FTJSerializer.Serialize(json, buffer, offset);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="json"></param>
        /// <param name="jsonStr"></param>
        public override void PopulateFromJson(Json json, string jsonStr) {
            byte[] buffer = Encoding.UTF8.GetBytes(jsonStr);
            PopulateFromJson(json, buffer, buffer.Length);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="json"></param>
        /// <param name="srcPtr"></param>
        /// <param name="srcSize"></param>
        /// <returns></returns>
        public override int PopulateFromJson(Json json, IntPtr srcPtr, int srcSize) {
            return JsonSerializer.Populate(json, srcPtr, srcSize);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="json"></param>
        /// <param name="src"></param>
        /// <param name="srcSize"></param>
        /// <returns></returns>
        public override int PopulateFromJson(Json json, byte[] src, int srcSize) {
            unsafe {
                fixed (byte* p = src) {
                    return PopulateFromJson(json, (IntPtr)p, srcSize);
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="json"></param>
        /// <param name="srcPtr"></param>
        /// <param name="srcSize"></param>
        /// <returns></returns>
        public override int PopulateFromFasterThanJson(Json json, IntPtr srcPtr, int srcSize) {
            return FTJSerializer.Populate(json, srcPtr, srcSize);
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
