// ***********************************************************************
// <copyright file="ManagedTupleWriter.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
namespace Starcounter.Internal {
    /// <summary>
    /// Struct FtjWriter
    /// </summary>
   public struct FtjWriter {

       /// <summary>
       /// The tw
       /// </summary>
      private TupleWriter tw;
      /// <summary>
      /// The buffer
      /// </summary>
      private byte[] buffer;

      /// <summary>
      /// Initializes a new instance of the <see cref="FtjWriter" /> struct.
      /// </summary>
      /// <param name="capacity">The capacity.</param>
      public FtjWriter( int capacity ) {
         buffer = new byte[capacity]; // TODO!
         tw = new TupleWriter(); // Dummy tuple writer
      }

      /// <summary>
      /// Writes the specified STR.
      /// </summary>
      /// <param name="str">The STR.</param>
      public void Write(string str) {
      }

      /// <summary>
      /// Haves the written.
      /// </summary>
      /// <param name="len">The len.</param>
      public void HaveWritten(uint len) {
      }

      /// <summary>
      /// Seals this instance.
      /// </summary>
      /// <returns>System.Int32.</returns>
      public int Seal() {
         return 0;
      }

      /// <summary>
      /// Writes the specified STR.
      /// </summary>
      /// <param name="str">The STR.</param>
      public void Write(int str) {
      }

      /// <summary>
      /// Initializes a new instance of the <see cref="FtjWriter" /> struct.
      /// </summary>
      /// <param name="parent">The parent.</param>
      /// <param name="valueCount">The value count.</param>
      /// <param name="initialOffsetElementSize">Initial size of the offset element.</param>
      public FtjWriter (FtjWriter parent, int valueCount, int initialOffsetElementSize) {
         buffer = null;
         unsafe {
            fixed (byte* pbuf = buffer) {
               tw = new TupleWriter(pbuf, (uint)valueCount, (uint)initialOffsetElementSize);
            }
         }
      }

      /// <summary>
      /// Writes the specified app list.
      /// </summary>
      /// <typeparam name="T"></typeparam>
      /// <param name="appList">The app list.</param>
      public void Write<T>(Arr appList) where T : Obj {
      }

      /// <summary>
      /// Gets the bytes.
      /// </summary>
      /// <returns>System.Byte[][].</returns>
      public byte[] GetBytes() {
         return null;
      }
   }
}
