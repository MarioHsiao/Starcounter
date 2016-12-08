﻿using System;
using System.Collections;
using Starcounter.XSON;

namespace Starcounter.Templates {
	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="OT"></typeparam>
	public class TArray<OT> : TObjArr where OT : Json, new() {
        // When a custom setter is used we need to add logic for setting parent.
        // The original setter is saved here, and the setter with added code (which uses)
        // this one is set as UnboundSetter (and maybe Setter)
        private Action<Json, Arr<OT>> customSetter;

		public new Action<Json, Arr<OT>> Setter;
		public new Func<Json, Arr<OT>> Getter;
		internal new Action<Json, Arr<OT>> UnboundSetter;
		internal new Func<Json, Arr<OT>> UnboundGetter;

		public TArray() {
			Getter = BoundOrUnboundGet;
			Setter = BoundOrUnboundSet;
		}

		public override Type MetadataType {
			get { return typeof(ArrMetadata<OT, Json>); }
		}

        private void SetParentAndUseCustomSetter(Json parent, Arr<OT> value) {
            UpdateParentAndIndex(parent, value);
            customSetter(parent, value);
        }

		/// <summary>
		/// Sets the getter and setter delegates for unbound values to the submitted delegates.
		/// </summary>
		/// <param name="getter"></param>
		/// <param name="setter"></param>
		/// <param name="overwriteExisting">
		/// If false the new delegates are only set if the current delegates are null.
		/// </param>
		public void SetCustomAccessors(Func<Json, Arr<OT>> getter, 
									   Action<Json, Arr<OT>> setter, 
									   bool overwriteExisting = true) {
			bool overwrite = (overwriteExisting || !hasCustomAccessors);

			if (overwrite || UnboundGetter == null)
				UnboundGetter = getter;
            if (overwrite || UnboundSetter == null) {
                customSetter = setter;
                UnboundSetter = SetParentAndUseCustomSetter;
            }

			base.SetCustomAccessors(
				(parent) => { return (Json)getter(parent); }, 
				(parent, value) => { setter(parent, (Arr<OT>)value); },
				overwriteExisting
			);

			hasCustomAccessors = true;
		}

		internal override void CopyValueDelegates(Template toTemplate) {
			var p = toTemplate as TArray<OT>;
			if (p != null) {
                p.customSetter = customSetter;
				p.UnboundGetter = UnboundGetter;
				p.UnboundSetter = UnboundSetter;
				p.hasCustomAccessors = hasCustomAccessors;
				base.CopyValueDelegates(toTemplate);

#if DEBUG
				p.DebugUnboundGetter = DebugUnboundGetter;
				p.DebugUnboundSetter = DebugUnboundSetter;
#endif
			}
		}

		internal override void SetDefaultValue(Json parent, bool markAsReplaced = false) {
			UnboundSetter(parent, new Arr<OT>(parent, this));

            if (markAsReplaced && parent != null && parent.HasBeenSent) {
                parent.MarkAsDirty(TemplateIndex);
                parent.CallHasChanged(this);
            }
        }
        
		internal override void GenerateUnboundGetterAndSetter() {
			if (UnboundGetter == null) {
				TemplateDelegateGenerator.GenerateUnboundDelegates<OT>(this, false);
				base.GenerateUnboundGetterAndSetter();
			}
		}

		private Arr<OT> BoundOrUnboundGet(Json parent) {
            if (UnboundGetter == null)
                return (Arr<OT>)parent;

			Arr<OT> arr = UnboundGetter(parent);

            if (parent.checkBoundProperties && UseBinding(parent)) {
				var data = BoundGetter(parent);
				arr.CheckBoundArray(data);
			}
			return arr;
		}

		private void BoundOrUnboundSet(Json parent, Arr<OT> value) {
			if (UseBinding(parent) && BoundSetter != null) {
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

		internal override Json GetValue(Json parent) {
			var arr = UnboundGetter(parent);

            if (parent.checkBoundProperties && UseBinding(parent)) {
				arr.CheckBoundArray(BoundGetter(parent));
                parent.MarkAsCached(this.TemplateIndex);
			}

            return arr;
		}

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
			Setter(parent, (Arr<OT>)value);
		}

		/// <summary>
		/// Creates the instance.
		/// </summary>
		/// <param name="parent">The parent.</param>
		/// <returns>System.Object.</returns>
		public override object CreateInstance(Json parent) {
			return new Arr<OT>(parent, this);
		}

		/// <summary>
		/// The .NET type of the instance represented by this template.
		/// </summary>
		/// <value>The type of the instance.</value>
		internal override System.Type DefaultInstanceType {
			get { return typeof(Arr<OT>); }
		}
	}
}
