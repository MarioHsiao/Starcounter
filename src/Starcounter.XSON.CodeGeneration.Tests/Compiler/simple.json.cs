using System;
using Starcounter;

namespace MySampleNamespace {
    public partial class Simple : Json {
        public string Name { get; set; }

		[Apapa.json.Items]
		public partial class InheritedChild : MyOtherNs.MySubNS.SubClass<MyNS.Order> {
		}
    }
}
