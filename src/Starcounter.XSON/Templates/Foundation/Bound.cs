using System;

namespace Starcounter.Templates {
    public enum Bound {
		UseParent,
        Yes,
        No,
        Auto
    }

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public class BindChildrenAttribute : Attribute {
		public BindChildrenAttribute(Bound bound) {
			Bound = bound;
		}

		public Bound Bound { get; set; }
	}
}
