

using System;
namespace Starcounter.Internal {
   public struct FtjWriter {

      private TupleWriter tw;
      private byte[] buffer;

      public FtjWriter( int capacity ) {
         buffer = new byte[capacity]; // TODO!
         tw = new TupleWriter(); // Dummy tuple writer
      }

      public void Write(string str) {
      }

      public void HaveWritten(uint len) {
      }

      public int Seal() {
         return 0;
      }

      public void Write(int str) {
      }

      public FtjWriter (FtjWriter parent, int valueCount, int initialOffsetElementSize) {
         buffer = null;
         unsafe {
            fixed (byte* pbuf = buffer) {
               tw = new TupleWriter(pbuf, (uint)valueCount, (uint)initialOffsetElementSize);
            }
         }
      }

      public void Write<T>(Listing appList) where T : App {
      }

      public byte[] GetBytes() {
         return null;
      }
   }
}
