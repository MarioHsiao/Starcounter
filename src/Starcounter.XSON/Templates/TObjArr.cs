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
		internal bool hasCustomBoundAccessors = false;
		internal Action<Json, IEnumerable> BoundSetter;
		internal Func<Json, IEnumerable> BoundGetter;
		internal Action<Json, Json> UnboundSetter;
		internal Func<Json, Json> UnboundGetter;
        private Func<TObjArr, TValue> getElementType = null;
		private TValue[] single = new TValue[0];
      
		/// <summary>
		/// 
		/// </summary>
		public TObjArr() {
			Getter = BoundOrUnboundGet;
			Setter = BoundOrUnboundSet;
		}

		public void SetCustomBoundAccessors(Func<Json, IEnumerable> boundGetter, Action<Json, IEnumerable> boundSetter)
		{
			BoundGetter = boundGetter;
			BoundSetter = boundSetter;
			hasCustomBoundAccessors = true;
		}

        private void SetParentAndUseCustomSetter(Json parent, Json value) {
            UpdateParentAndIndex(parent, value);
            customSetter(parent, value);
        }

        public void SetCustomGetElementType(Func<TObjArr, TValue> getElementType) {
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
			bool overwrite = (overwriteExisting || !hasCustomAccessors);
	
			if (overwrite || UnboundGetter == null) {
				UnboundGetter = getter;
#if DEBUG
				DebugUnboundGetter = "<custom>";
#endif
			}

			if (overwrite || UnboundSetter == null) {
                customSetter = setter;
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
				p.DebugUnboundGetter = DebugUnboundGetter;
				p.DebugUnboundSetter = DebugUnboundSetter;
#endif
			}
		}

		internal override void SetDefaultValue(Json parent, bool markAsReplaced = false) {
			UnboundSetter(parent, new Arr<Json>(parent, this));

            if (markAsReplaced && parent != null && parent.HasBeenSent) {
                parent.MarkAsDirty(TemplateIndex);
                parent.CallHasChanged(this);
            }
        }

		internal override void InvalidateBoundGetterAndSetter() {
			BoundGetter = null;
			BoundSetter = null;
			base.InvalidateBoundGetterAndSetter();
		}

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        internal override bool HasBinding() {
            return (BoundGetter != null);
        }

        internal override bool GenerateBoundGetterAndSetter(Json json) {
			TemplateDelegateGenerator.GenerateBoundDelegates(this, json);
			return (BoundGetter != null);
		}

		internal override void GenerateUnboundGetterAndSetter() {
			if (UnboundGetter == null)
				TemplateDelegateGenerator.GenerateUnboundDelegates(this, false);
		}

        /// <summary>
        /// If the property is bound, it reads the bound value and stores it
        /// using the unbound delegate and marks the property as cached. 
        /// All reads after this will read the from the unbound delegate,
        /// until the cache is resetted when checkpointing.
        /// </summary>
        /// <param name="json"></param>
        internal void SetCachedReads(Json json) {
            // We don't have to check if th property is already cached.
            // That is done when checking if binding should be used.
            if (json.IsTrackingChanges && UseBinding(json) && json.Session.enableCachedReads) {
                Json value = UnboundGetter(json);
                if (value != null && json.checkBoundProperties) {
                    value.CheckBoundArray(BoundGetter(json));
                }
                json.MarkAsCached(this.TemplateIndex);
            }
        }

        internal override void Checkpoint(Json parent) {
			Json arr = UnboundGetter(parent);

			for (int i = 0; i < ((IList)arr).Count; i++) {
				var row = (Json)arr._GetAt(i);
				row.CheckpointChangeLog();
				arr.CheckpointAt(i);
			}
			arr.arrayAddsAndDeletes = null;
			arr.dirty = false;
			base.Checkpoint(parent);
		}

		internal override void CheckAndSetBoundValue(Json parent, bool addToChangeLog) {
			throw new NotImplementedException();
		}

		internal override Json GetValue(Json parent) {
			var arr = UnboundGetter(parent);

            if (parent.checkBoundProperties && UseBinding(parent)) {
				arr.CheckBoundArray(BoundGetter(parent));
                if (arr.Session.enableCachedReads)
                    parent.MarkAsCached(this.TemplateIndex);
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
			newArr.pendingEnumeration = true;
			newArr.data = value;
			newArr.Array_InitializeAfterImplicitConversion(parent, this);

			if (UseBinding(parent))
				BoundSetter(parent, value);
			UnboundSetter(parent, newArr);

			if (parent.HasBeenSent)
				parent.MarkAsDirty(TemplateIndex);

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
            if (UnboundGetter == null)
                return parent;

			Json arr = UnboundGetter(parent);

            if (parent.checkBoundProperties && UseBinding(parent)) {
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

			if (value.pendingEnumeration) {
				value.Array_InitializeAfterImplicitConversion(parent, this);
			}

			if (parent.HasBeenSent)
				parent.MarkAsDirty(TemplateIndex);

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
        public TValue ElementType {
            get {
                if (single.Length != 0)
                    return single[0];

                if (getElementType == null) 
                    return null;

                // Quick temporary hack for removing synchronization issue for one specific case.
                // Needs to be solved properly. #2597
                lock (elementLockObject) {
                    if (single.Length == 0)
                        ElementType = getElementType(this);
                }
                return single[0];
            }
            set {
                if (value != null) {
                    single = new TValue[1];
                    single[0] = value;
                } else {
                    single = new TValue[0];
                }
            }
        }

        public override object CreateInstance(Json parent = null) {
            return new Arr<Json>(parent, this);
		}

        internal override TemplateTypeEnum TemplateTypeId {
            get { return TemplateTypeEnum.Array; }
        }
    }
}
