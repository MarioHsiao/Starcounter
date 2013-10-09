using System.Collections.Generic;

namespace Starcounter.Internal.XSON.Tests {
	public class Recursive : Entity {
		private List<Recursive> list;

		public string Name { get; set; }

		public List<Recursive> Recursives {
			get {
				if (list == null)
					list = new List<Recursive>();
				return list;
			}
		}
	}
}
