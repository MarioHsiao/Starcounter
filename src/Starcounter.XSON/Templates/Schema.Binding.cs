using System;
using Starcounter.Advanced;
using Starcounter.Advanced.XSON;
using Starcounter.Internal.XSON;

namespace Starcounter.Templates {
    partial class TObject {
		private Bound bindChildren;
        public bool HasAtLeastOneBoundProperty = true; // TODO!

		/// <summary>
		/// 
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
        internal override bool UseBinding(IBindable data) {
			if (data == null)
				return false;
            return DataBindingFactory.VerifyOrCreateBinding(this, data.GetType());
        }

		/// <summary>
		/// 
		/// </summary>
		/// <remarks>
		/// 
		/// </remarks>
		public Bound BindChildren {
			get { return bindChildren; }
			set {
				if (value == Templates.Bound.UseParent)
					throw new Exception("Cannot specify Bound.UseParent on this property.");
				bindChildren = value;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
        internal override object GetBoundValueAsObject(Json obj) {
			return obj.GetBound(this);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="obj"></param>
		/// <param name="value"></param>
        internal override void SetBoundValueAsObject(Json obj, object value) {
			obj.SetBound(this, (IBindable)value);
		}
    }
}
