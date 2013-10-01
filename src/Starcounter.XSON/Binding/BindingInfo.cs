using System;
using System.Collections.Generic;
using System.Reflection;

namespace Starcounter.Internal.XSON {
	internal struct BindingInfo {
		internal MemberInfo Member;
		internal List<MemberInfo> Path;
	}
}
