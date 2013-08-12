// ***********************************************************************
// <copyright file="SessionBlob.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Diagnostics;
using System.Text;

namespace Starcounter.Internal
{
    /// <summary>
    /// Struct SessionBlobHeader
    /// </summary>
   public unsafe struct SessionBlobHeader {
       /// <summary>
       /// The total BLOB size
       /// </summary>
      public Int32 TotalBlobSize; // Total blob size
      /// <summary>
      /// The generation
      /// </summary>
      public UInt32 Generation; // Will maybe be used when hot swap code is supported in Starcounter
      /// <summary>
      /// The encoding
      /// </summary>
      public byte Encoding; // Tuples can be encoded using binary or printable (ASCII/UTF-8) bytes. Base32, Base64 or Base256
      /// <summary>
      /// The tuple overflow limit
      /// </summary>
      public IntPtr TupleOverflowLimit; // Used to avoid overflowing when writing buffers
      /// <summary>
      /// The other session BLOB count
      /// </summary>
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
       /// <summary>
       /// The header
       /// </summary>
      public SessionBlobHeader Header;
      /// <summary>
      /// The root tuple
      /// </summary>
      public SessionTuple RootTuple;
      /// <summary>
      /// The header size
      /// </summary>
      public static int HeaderSize = sizeof(SessionBlobHeader); // 4 + 4 + 1 + sizeof(byte*); // Generation + Size + Encoding
   }

   /// <summary>
   /// A tuple holds a view model object (aka app), a view model array (aka app array) or other such
   /// view model structures comprising a set of values.
   /// </summary>
   public struct SessionTuple
   {
       /// <summary>
       /// The offset size
       /// </summary>
      public byte OffsetSize; // The first byte tells size of offsetelements
      /// <summary>
      /// The offsets
      /// </summary>
      public byte Offsets; // Variable size array of value offseta
   }

   /// <summary>
   /// Used to refer to a session blob. This object can be disposed without affecting the blob.
   /// A new proxy can be reattached to the blob as the proxy has no state other that its cache.
   /// </summary>
   public struct SessionBlobProxy
   {
       /// <summary>
       /// The BLOB handle
       /// </summary>
      internal IntPtr BlobHandle;
      /// <summary>
      /// The cached_ BLOB
      /// </summary>
      unsafe internal SessionBlob* Cached_Blob;
      /// <summary>
      /// The pin nesting
      /// </summary>
      private int pinNesting;

      /// <summary>
      /// Inits the specified handle.
      /// </summary>
      /// <param name="handle">The handle.</param>
      public void Init(IntPtr handle)
      {
         BlobHandle = handle;
         pinNesting = 0;
      }

      /// <summary>
      /// Returns a <see cref="System.String" /> that represents this instance.
      /// </summary>
      /// <param name="handle">The handle.</param>
      /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
      public static string ToString(IntPtr handle)
      {
         SessionBlobProxy bp = new SessionBlobProxy();
         bp.Init(handle);
         return bp.ToString();
      }


      /// <summary>
      /// Pins this instance.
      /// </summary>
      /// <returns>SessionBlob.</returns>
      public unsafe SessionBlob* Pin()
      {
         SessionBlobProxy.PinBlob(BlobHandle, out Cached_Blob);
         pinNesting++;
         return Cached_Blob;
      }

      /// <summary>
      /// Unpins this instance.
      /// </summary>
      public void Unpin()
      {
         pinNesting--;
         if (pinNesting == 0)
         {
            // TODO! Call Unpin
         }
      }


      /// <summary>
      /// Returns a <see cref="System.String" /> that represents this instance.
      /// </summary>
      /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
      public override unsafe string ToString()
      {
         string str = SessionBlobProxy.ToString(Pin());
         Unpin();
         return str;
      }


      /// <summary>
      /// The UTF8 encode
      /// </summary>
      internal static Encoder Utf8Encode = new UTF8Encoding(false, true).GetEncoder();
      /// <summary>
      /// The UTF8 decode
      /// </summary>
      internal static Decoder Utf8Decode = new UTF8Encoding(false, true).GetDecoder();

      /// <summary>
      /// Pins the BLOB.
      /// </summary>
      /// <param name="handle">The handle.</param>
      /// <param name="bufferStart">The buffer start.</param>
      public static unsafe void PinBlob(IntPtr handle, out SessionBlob* bufferStart)
      {
         bufferStart = (SessionBlob*)handle;
      }

      /// <summary>
      /// Allocates a new Session blob used to keep session state in the form of FriendlyTuples.
      /// </summary>
      /// <param name="bufferStart">Will point at first tuple</param>
      /// <param name="bufferEnd">Will point at the last byte of the blob</param>
      /// <param name="tupleStart">The tuple start.</param>
      /// <returns>IntPtr.</returns>
      public static unsafe IntPtr CreateBlob(out byte* bufferStart, out byte* bufferEnd, out byte* tupleStart)
      {
         // Default size is 2Kb
         Int32 size = 2048;
         Debug.WriteLine("Allocating {0} of session state memory", size);
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

      /// <summary>
      /// Frees the BLOB.
      /// </summary>
      /// <param name="Handle">The handle.</param>
      public static unsafe void FreeBlob(IntPtr Handle)
      {
         System.Runtime.InteropServices.Marshal.FreeHGlobal((IntPtr)Handle);
      }

      /// <summary>
      /// Returns a <see cref="System.String" /> that represents this instance.
      /// </summary>
      /// <param name="blob">The BLOB.</param>
      /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
      public static unsafe string ToString(SessionBlob* blob)
      {
         byte* atStart = ((byte*)blob) + SessionBlob.HeaderSize;
         var tr = new TupleReader(atStart,1);
         return tr.DebugString;
      }

   }
}

