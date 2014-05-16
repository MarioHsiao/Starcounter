using System.Text;

namespace Starcounter.Internal {
	/// <summary>
	/// Internal struct that helps writing bytearrays, strings and other values
	/// to an unsafe buffer in utf8 format. 
	/// </summary>
	/// <remarks>
	/// This struct will NOT do any checking if the value fits in the buffer. It is up
	/// to the caller to check for buffer overrun.
	/// </remarks> 
	public unsafe struct Utf8Writer {
		private static Encoder utf8Encoder = new UTF8Encoding(false, true).GetEncoder();

		private int totalWritten;
		private byte* pbuf;

		public Utf8Writer(byte* pbuf) {
			this.pbuf = pbuf;
			this.totalWritten = 0;
		}

		public int Written {
			get { return totalWritten; }
		}

		public void Write(char value) {
			*pbuf++ = (byte)value;
			totalWritten++;
		}

		public void Write(byte[] value) {
			fixed (byte* psrc = value) {
				Memcpy16fwd(pbuf, psrc, (uint)value.Length);
			}
			totalWritten += value.Length;
			pbuf += value.Length;
		}

		public void Write(long value) {
			uint written = Utf8Helper.WriteIntAsUtf8(pbuf, value);
			totalWritten += (int)written;
			pbuf += written;
		}

		public void Write(string value) {
			int written;
			fixed (char* pval = value) {
				written = utf8Encoder.GetBytes(pval, value.Length, pbuf, value.Length * 3, true);
			}
			totalWritten += written;
			pbuf += written;
		}

		public int GetByteCount(string value) {
			fixed (char* pval = value) {
				return  utf8Encoder.GetByteCount(pval, value.Length, false);
			}
		}

        public void Skip(int count) {
            pbuf += count;
            totalWritten += count;
        }

		// Copied from FasterThanJson.MemcpyUtil
		private unsafe static void Memcpy16fwd(byte* dest, byte* src, uint len) {
			if (len >= 16) {
				do {
					*(long*)dest = *(long*)src;
					*(long*)(dest + 8) = *(long*)(src + 8);
					dest += 16;
					src += 16;
				}
				while ((len -= 16) >= 16);
			}
			if (len > 0) {
				if ((len & 8) != 0) {
					*(long*)dest = *(long*)src;
					dest += 8;
					src += 8;
				}
				if ((len & 4) != 0) {
					*(int*)dest = *(int*)src;
					dest += 4;
					src += 4;
				}
				if ((len & 2) != 0) {
					*(short*)dest = *(short*)src;
					dest += 2;
					src += 2;
				}
				if ((len & 1) != 0) {
					byte* expr_75 = dest;
					dest = expr_75 + 1;
					byte* expr_7C = src;
					src = expr_7C + 1;
					*expr_75 = *expr_7C;
				}
			}
		}
	}
}
