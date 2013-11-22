using Starcounter;
using Starcounter.Advanced;

namespace Starcounter.Website {
	class Program {
		static void Main() {
			Handle.GET("/", () => {
				return (string)X.GET("/index");
			});
		}
	}
}