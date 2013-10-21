using System;

namespace Starcounter.Templates {
    public enum BindingStrategy {
		UseParent,
        Bound,
        Unbound,
        Auto
    }

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class BindChildrenAttribute : Attribute {
		public BindChildrenAttribute(BindingStrategy strategy) {
			Strategy = strategy;
		}

		public BindingStrategy Strategy { get; set; }
	}
}
