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

		internal override void GenerateUnboundGetterAndSetter(Json json) {
			TemplateDelegateGenerator.GenerateUnboundDelegates<OT>(this, json, false);
		}

		private Arr<OT> BoundOrUnboundGet(Json parent) {
			Arr<OT> arr = UnboundGetter(parent);

			if (UseBinding(parent)) {
				var data = BoundGetter(parent);
				arr.CheckBoundArray(data);
			}
			return arr;
		}

		private void BoundOrUnboundSet(Json json, IEnumerable value) {
			Json arr = UnboundGetter(json);
			if (UseBinding(json)) {
				arr.CheckBoundArray(value);
			} else
				throw new NotSupportedException("TODO!");
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
