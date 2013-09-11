using System;
using System.Runtime.InteropServices;
using System.Text;
using Constants = Starcounter.Internal.StarcounterConstants.NetworkConstants;

namespace Starcounter.Internal {
	/// <summary>
	/// Internal struct that helps writing bytearrays, strings and other values
	/// to an unsafe buffer in utf8 format. 
	/// </summary>
	/// <remarks>
	/// This struct will NOT do any checking if the value fits in the buffer. It is up
	/// to the caller to check for buffer overrun.
	/// </remarks> 
	internal unsafe struct ResponseWriter {
		internal static byte[] Http11;
		internal static byte[] ServerSc;
		internal static byte[] NoCache;
		internal static byte[] ContentTypeStart;
		internal static byte[] ContentEncodingStart;
		internal static byte[] ContentLengthStart;
		internal static byte[] SetCookieStart;
		internal static byte[] SetCookieLocationMiddle;
		internal static byte[] setCookiePathEnd;
		internal static byte[] CRLF;
		internal static byte[] CRLFCRLF;
		private static Encoder utf8Encoder;

		private int totalWritten;
		private byte* pbuf;

		static ResponseWriter() {
			Http11 = Encoding.UTF8.GetBytes("HTTP/1.1 ");
			ServerSc = Encoding.UTF8.GetBytes("Server: SC" + Constants.CRLF);
			NoCache = Encoding.UTF8.GetBytes("Cache-Control: no-cache" + Constants.CRLF);
			ContentTypeStart = Encoding.UTF8.GetBytes("Content-Type: ");
			ContentEncodingStart = Encoding.UTF8.GetBytes("Content-Encoding: ");
			ContentLengthStart = Encoding.UTF8.GetBytes("Content-Length: ");
			SetCookieStart = Encoding.UTF8.GetBytes("Set-Cookie: ");
			SetCookieLocationMiddle = Encoding.UTF8.GetBytes(";Location=");
			setCookiePathEnd = Encoding.UTF8.GetBytes("; path=/");
			CRLF = Encoding.UTF8.GetBytes(Constants.CRLF);
			CRLFCRLF = Encoding.UTF8.GetBytes(Constants.CRLFCRLF);

			utf8Encoder = new UTF8Encoding(false, true).GetEncoder();
		}

		internal ResponseWriter(byte* pbuf) {
			this.pbuf = pbuf;
			this.totalWritten = 0;
		}

		internal int Written {
			get { return totalWritten; }
		}

		internal void Write(byte value) {
			*pbuf++ = value;
			totalWritten++;
		}

		internal void Write(byte[] value) {
			Marshal.Copy(value, 0, (IntPtr)pbuf, value.Length);
			totalWritten += value.Length;
			pbuf += value.Length;
		}

		internal void Write(long value) {
			uint written = Utf8Helper.WriteIntAsUtf8(pbuf, value);
			totalWritten += (int)written;
			pbuf += written;
		}

		internal void Write(string value) {
			int written;
			fixed (char* pval = value) {
				written = utf8Encoder.GetBytes(pval, value.Length, pbuf, 8192, true);
			}
			totalWritten += written;
			pbuf += written;
		}
	}
}
