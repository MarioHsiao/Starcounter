using System;

namespace Starcounter.Templates {
	/// <summary>
	/// 
	/// </summary>
	/// <typeparam name="OT"></typeparam>
	public class TArray<OT> : TObjArr
		where OT : Json, new() {
		public override Type MetadataType {
			get { return typeof(ArrMetadata<OT, Json>); }
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
