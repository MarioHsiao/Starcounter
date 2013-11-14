using System;
using System.Collections;
using Starcounter.XSON;

namespace Starcounter.Templates {
	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="OT"></typeparam>
	public class TArray<OT> : TObjArr
		where OT : Json, new() {

		public new readonly Action<Json, Arr<OT>> Setter;
		public new readonly Func<Json, Arr<OT>> Getter;
		internal new Action<Json, Arr<OT>> UnboundSetter;
		internal new Func<Json, Arr<OT>> UnboundGetter;

		public TArray() {
			Getter = BoundOrUnboundGet;
			Setter = BoundOrUnboundSet;
		}

		public override Type MetadataType {
			get { return typeof(ArrMetadata<OT, Json>); }
		}

		internal override void GenerateUnboundGetterAndSetter() {
			TemplateDelegateGenerator.GenerateUnboundDelegates<OT>(this, false);
			base.GenerateUnboundGetterAndSetter();
		}

		private Arr<OT> BoundOrUnboundGet(Json parent) {
			Arr<OT> arr = UnboundGetter(parent);

			if (UseBinding(parent)) {
				var data = BoundGetter(parent);
				arr.CheckBoundArray(data);
			}
			return arr;
		}

		private void BoundOrUnboundSet(Json parent, Arr<OT> value) {
			Arr<OT> oldArr = UnboundGetter(parent);
			if (oldArr != null) {
				oldArr.SetParent(null);
				oldArr._cacheIndexInArr = -1;
			}

			if (UseBinding(parent) && BoundSetter != null) {
				BoundSetter(parent, (IEnumerable)value.Data);
			}
			UnboundSetter(parent, value);

			if (value._PendingEnumeration) {
				value.Array_InitializeAfterImplicitConversion(parent, this);
			}

			if (parent.HasBeenSent)
				parent.MarkAsReplaced(TemplateIndex);

			parent._CallHasChanged(this);
		}

		internal override Json GetValue(Json parent) {
			var arr = UnboundGetter(parent);

			if (UseBinding(parent)) {
				arr.CheckBoundArray(BoundGetter(parent));
			}

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
}
