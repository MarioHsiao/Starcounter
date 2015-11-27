using Starcounter;
using System;
using Starcounter.Metadata;

class Program {
    static Int32 Main() {
		
		Handle.GET("/haha", () => {
			return "haha";
		});
		
		String respBody = Self.GET<String>("/haha");
		
		if (respBody != "haha")
			throw new Exception("Wrong response received!");
			
		return 0;
    }
}
