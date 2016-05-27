using System;
using Starcounter;
using ScAdv = Starcounter.Advanced;

namespace MySampleNamespace {
	using ScSchema = Starcounter.Json.Schema;

    public partial class Simple : Json, IEquatable<Order> {
        public string Name { get; set; }

		[Apapa_json.Items]
		public partial class InheritedChild : MyOtherNs.MySubNS.SubClass, IBound<OrderItem> {
		}
    }
}
