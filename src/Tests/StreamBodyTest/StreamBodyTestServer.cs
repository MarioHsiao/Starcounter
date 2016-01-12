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

class StreamBodyTest {
	static Byte[] Stream1 = new Byte[1000];
	static Byte[] Stream10 = new Byte[1000 * 10];
	static Byte[] Stream100 = new Byte[1000 * 100];
	static Byte[] Stream1000 = new Byte[1000 * 1000];
	static Byte[] Stream10000 = new Byte[1000 * 10000];

	static void Main(String[] args)
	{
		for (Int32 i = 0; i < Stream1.Length; i++) Stream1[i] = (Byte)'a';
		for (Int32 i = 0; i < Stream10.Length; i++) Stream10[i] = (Byte)'b';
		for (Int32 i = 0; i < Stream100.Length; i++) Stream100[i] = (Byte)'c';
		for (Int32 i = 0; i < Stream1000.Length; i++) Stream1000[i] = (Byte)'d';
		for (Int32 i = 0; i < Stream10000.Length; i++) Stream10000[i] = (Byte)'e';

		Handle.GET("/streamtest/{?}", (string streamId) => {

			MemoryStream ms = new MemoryStream();

			switch (streamId) {
				case "1":
					ms = new MemoryStream(Stream1);
				break;

				case "2":
					ms = new MemoryStream(Stream10);
				break;

				case "3":
					ms = new MemoryStream(Stream100);
				break;

				case "4":
					ms = new MemoryStream(Stream1000);
				break;

				case "5":
					ms = new MemoryStream(Stream10000);
				break;
				
				default: {
					throw new ArgumentOutOfRangeException("Wrong stream Id number: " + streamId);
                }
			}

			return new Response() {
				StatusCode = 200,
				StreamedBody = ms
			};
		});
	}
};
