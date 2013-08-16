using Starcounter.Advanced;
using Starcounter.Advanced.XSON;
using Starcounter.Internal.XSON;

namespace Starcounter.Templates {
    partial class TObj {
		private bool bindChildren;

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
        /// <param name="child"></param>
        private void CheckBindingForChild(Template child) {
            TValue value;
            string propertyName;

            value = child as TValue;
            if (value != null) {
                if (value.Bound == Bound.No) {
                    propertyName = value.PropertyName;
                    if (!string.IsNullOrEmpty(propertyName)
                        && !(propertyName[0] == '_')) {
                        value.Bind = propertyName;
                    }
                }
                else if (value.Bind == null) {
                    value.Bound = Bound.No;
                }
            }
        }

        /// <summary>
        /// If set to true all children will be automatically bound to the dataobject, 
        /// otherwise the children needs to set binding themselves.
        /// </summary>
        /// <remarks>
        /// If set to true and a child which should not be bound is added the name of the
        /// child should start with a '_' (underscore).
        /// </remarks>
        public bool BindChildren {
            get { return bindChildren; }
            set {
                bindChildren = value;
                if (Properties.Count > 0) {
                    if (value == true) {
                        foreach (var child in Properties) {
                            CheckBindingForChild(child);
                        }
                    }
                }
            }
        }
    }
}
