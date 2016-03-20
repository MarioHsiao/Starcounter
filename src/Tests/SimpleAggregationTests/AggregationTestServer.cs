using System;
using System.IO;
using System.Text;
using System.Threading;
using Starcounter;
using System.Diagnostics;
using System.Collections.Generic;
using Starcounter.Internal;
using Starcounter.Internal.Web;
using Starcounter.Advanced;
using Codeplex.Data;
using System.Net;

namespace AggregationTestServerNamespace {

class AggregationTestServer {

	static void Main(String[] args)
	{
		Starcounter.Extensions.TestStatistics.EnableTestStatistics();
	
		Handle.GET("/test", () => {
			return 200;
		});
	}
};
}


