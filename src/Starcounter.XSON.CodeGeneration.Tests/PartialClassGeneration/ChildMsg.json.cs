using Starcounter;


namespace StarcounterApplication6 {
	[BindChildren(Bound.Auto)]
	[ChildMsg_json]
	public partial class ChildMsg : BaseMsg {

		[ChildMsg_json.SomeObject]
		public partial class SomeJson : Json {

			[ChildMsg_json.SomeObject.Inner]
			public partial class InnerJson : Json {
			}
		}
	}
}
