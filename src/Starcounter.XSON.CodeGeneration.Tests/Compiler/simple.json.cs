using System;
using Starcounter;

namespace MySampleNamespace {
    public partial class Simple<T,T2> : Json<T> {
        public string Name { get; set; }

		[Apapa.json.Items]
		public partial class InheritedChild : MyOtherNs.MySubNS.SubClass<MyNS.Order> {
		}
    }
}
