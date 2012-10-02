
using System;
using System.Text;

namespace Starcounter.Internal
{
   public unsafe struct SessionBlobHeader {
      public Int32 TotalBlobSize; // Total blob size
      public UInt32 Generation; // Will maybe be used when hot swap code is supported in Starcounter
      public byte Encoding; // Tuples can be encoded using binary or printable (ASCII/UTF-8) bytes. Base32, Base64 or Base256
      public IntPtr TupleOverflowLimit; // Used to avoid overflowing when writing buffers
      public int OtherSessionBlobCount;
   }

   /// <summary>
   /// Allocation binary large object used by the session Storage engine for Session data.
   /// The session storage engine is optimized for many concurrent session by trying to keep the number of 
   /// allocation (session blobs) down to a minimum and the individual size of each blob as small as possible.
   /// If there are many session blobs in a session, a session blob merge can be performed resulting in the whole 
   /// session state being hosted in a single SessionBlob.
   /// </summary>
   public unsafe struct SessionBlob
   {
      public SessionBlobHeader Header;
      public SessionTuple RootTuple;
      public static int HeaderSize = sizeof(SessionBlobHeader); // 4 + 4 + 1 + sizeof(byte*); // Generation + Size + Encoding
   }

   /// <summary>
   /// A tuple holds a view model object (aka app), a view model array (aka app array) or other such
   /// view model structures comprising a set of values.
   /// </summary>
   public struct SessionTuple
   {
      public byte OffsetSize; // The first byte tells size of offsetelements
      public byte Offsets; // Variable size array of value offseta
   }

   /// <summary>
   /// Used to refer to a session blob. This object can be disposed without affecting the blob.
   /// A new proxy can be reattached to the blob as the proxy has no state other that its cache.
   /// </summary>
   public struct SessionBlobProxy
   {
      internal IntPtr BlobHandle;
      unsafe internal SessionBlob* Cached_Blob;
      private int pinNesting;

      public void Init(IntPtr handle)
      {
         BlobHandle = handle;
         pinNesting = 0;
      }

      public static string ToString(IntPtr handle)
      {
         SessionBlobProxy bp = new SessionBlobProxy();
         bp.Init(handle);
         return bp.ToString();
      }


      public unsafe SessionBlob* Pin()
      {
         SessionBlobProxy.PinBlob(BlobHandle, out Cached_Blob);
         pinNesting++;
         return Cached_Blob;
      }

      public void Unpin()
      {
         pinNesting--;
         if (pinNesting == 0)
         {
            // TODO! Call Unpin
         }
      }


      public override unsafe string ToString()
      {
         string str = SessionBlobProxy.ToString(Pin());
         Unpin();
         return str;
      }


      internal static Encoder Utf8Encode = new UTF8Encoding(false, false).GetEncoder();
      internal static Decoder Utf8Decode = new UTF8Encoding(false, false).GetDecoder();

      public static unsafe void PinBlob(IntPtr handle, out SessionBlob* bufferStart)
      {
         bufferStart = (SessionBlob*)handle;
      }

      /// <summary>
      /// Allocates a new Session blob used to keep session state in the form of FriendlyTuples.
      /// </summary>
      /// <param name="bufferStart">Will point at first tuple</param>
      /// <param name="bufferEnd">Will point at the last byte of the blob</param>
	  /// <param name="tupleStart"></param>
      /// <returns></returns>
      public static unsafe IntPtr CreateBlob(out byte* bufferStart, out byte* bufferEnd, out byte* tupleStart)
      {
         // Default size is 2Kb
         Int32 size = 2048;
         Console.WriteLine("Allocating {0} of session state memory", size);
         SessionBlob* blob = (SessionBlob*)System.Runtime.InteropServices.Marshal.AllocHGlobal(size);
#if DEBUG
         for (int t = 0 ; t < size ; t++ )
         {
            ((byte*)blob)[t] = (byte)126;
         }
#endif
         bufferEnd = (byte*)blob + size - 1;
         blob->Header.Generation = 0;
         blob->Header.TotalBlobSize = size;
         blob->Header.OtherSessionBlobCount = 0; // Increase for every item in other-session-pointer-array
         blob->Header.TupleOverflowLimit = (IntPtr)(blob + size - 1- (5*sizeof(UInt64))); // Our external blob list is empty. We can use all space except for 5 entries (40 bytes)
         blob->Header.Encoding = (byte)'T'; // Means the most friendly text format (no control characters)
//         blob->RootTuple.OffsetSize = (byte)'3'; // 6 means 6 byte offset array element size
         //blob->RootTupleSize = 0;
         bufferStart = (byte*)blob;
         //         blob->LastTupleOffset = (UInt32)(bufferStart - (byte*)blob);

         tupleStart = bufferStart + SessionBlob.HeaderSize /*+ SessionBlob.PublicBlobHeaderSize*/;

         return (IntPtr)blob;
      }

      public static unsafe void FreeBlob(IntPtr Handle)
      {
         System.Runtime.InteropServices.Marshal.FreeHGlobal((IntPtr)Handle);
      }

      public static unsafe string ToString(SessionBlob* blob)
      {
         byte* atStart = ((byte*)blob) + SessionBlob.HeaderSize;
         var tr = new TupleReader(atStart,1);
         return tr.DebugString;
      }

   }
}

