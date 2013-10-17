using System;
using Starcounter;

namespace MySampleNamespace {
    public partial class Simple : Json, IEquatable<Order> {
        public string Name { get; set; }

		[Apapa.json.Items]
		public partial class InheritedChild : MyOtherNs.MySubNS.SubClass, IBound<OrderItem> {
		}
    }
}
