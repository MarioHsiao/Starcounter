using System.Text;
using Constants = Starcounter.Internal.StarcounterConstants.NetworkConstants;

namespace Starcounter.Internal {
	/// <summary>
	/// Contains static members for all headers used in request/response
	/// creation as bytearrays in Utf8 format.
	/// </summary>
	internal static class HttpHeadersUtf8 {
		internal readonly static byte[] Http11;
		internal readonly static byte[] ServerSc;
		internal readonly static byte[] CacheControlNoCache;
		internal readonly static byte[] CacheControlStart;
		internal readonly static byte[] ContentTypeStart;
		internal readonly static byte[] ContentEncodingStart;
		internal readonly static byte[] ContentLengthStart;
		internal readonly static byte[] SetCookieStart;
		internal readonly static byte[] SetCookieLocationMiddle;
		internal readonly static byte[] setCookiePathEnd;
		internal readonly static byte[] CRLF;
		internal readonly static byte[] CRLFCRLF;
		internal readonly static int TotalByteSize;

		static HttpHeadersUtf8() {
			Http11 = Encoding.UTF8.GetBytes("HTTP/1.1 ");
			ServerSc = Encoding.UTF8.GetBytes("Server: SC" + Constants.CRLF);
			CacheControlNoCache = Encoding.UTF8.GetBytes("Cache-Control: no-cache" + Constants.CRLF);
			CacheControlStart = Encoding.UTF8.GetBytes("Cache-Control: ");
			ContentTypeStart = Encoding.UTF8.GetBytes("Content-Type: ");
			ContentEncodingStart = Encoding.UTF8.GetBytes("Content-Encoding: ");
			ContentLengthStart = Encoding.UTF8.GetBytes("Content-Length: ");
			SetCookieStart = Encoding.UTF8.GetBytes("Set-Cookie: ");
			SetCookieLocationMiddle = Encoding.UTF8.GetBytes(";Location=");
			setCookiePathEnd = Encoding.UTF8.GetBytes("; path=/");
			CRLF = Encoding.UTF8.GetBytes(Constants.CRLF);
			CRLFCRLF = Encoding.UTF8.GetBytes(Constants.CRLFCRLF);

			TotalByteSize = Http11.Length
							+ ServerSc.Length
							+ CacheControlNoCache.Length
							+ CacheControlStart.Length
							+ ContentTypeStart.Length 
							+ ContentEncodingStart.Length 
							+ ContentLengthStart.Length 
							+ SetCookieStart.Length
							+ SetCookieLocationMiddle.Length 
							+ setCookiePathEnd.Length 
							+ CRLF.Length 
							+ CRLFCRLF.Length;

            // Adding new lines, etc.
            TotalByteSize += 64;
		}
	}
}
