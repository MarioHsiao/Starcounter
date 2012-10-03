using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
//using NUnit.Framework;

namespace Starcounter.Internal
{

   /// <summary>
   /// The friendly tuple proxy is a temporary object that is instantianted when needed to interact
   /// with a FriendlyTuple stored in a SessionBlob. The FriendlyTupleProxy should be short lived.
   /// There will be millions of unmanaged FriendlyTuples stored inside the blob and the proxy should be 
   /// created when processing a request and after the request is processed, it should be garbage collected.
   /// The next time the same FriendlyTuple is referenced, a new FriendlyTupleProxy should be created.
   /// </summary>
   /// <remarks>
   /// All core methods to manipulate tuples are static. This is for performance reasons as it allows the
   /// user to manipulate session state (documents and templates) without having to instantiate CLR objects.
   /// The session state can also be manipulated directly using unmanaged code that works directly on memory.
   /// </remarks>
   public unsafe class TupleProxy
   {
      /// <summary>
      /// A handle to the memory blob hosting this document element.
      /// </summary>
      internal IntPtr BlobHandle = IntPtr.Zero;
      internal List<Int32> Address = null;
      internal UInt32 Generation = 0;
      internal Int32 OffsetInBlob = -1;

      [EditorBrowsable(EditorBrowsableState.Never)]
      internal int _Index = -1;

      [EditorBrowsable(EditorBrowsableState.Never)]     
      public override string ToString() {
          return base.ToString();
      }

      [EditorBrowsable(EditorBrowsableState.Never)]
      public override bool Equals(object obj) {
          return base.Equals(obj);
      }

      [EditorBrowsable(EditorBrowsableState.Never)]
      public override int GetHashCode() {
          return base.GetHashCode();
      }

      [EditorBrowsable(EditorBrowsableState.Never)]
      new public object MemberwiseClone() {
          return base.MemberwiseClone();
      }

      [EditorBrowsable(EditorBrowsableState.Never)]
      new public Type GetType() {
          return base.GetType();
      }

      #region Cache
      /// <summary>
      /// -1 means that the size is not cached and the value needs to be read from the blob
      /// </summary>
      internal Int32 Cached_Size = 0;
      internal int Cached_OffsetSize = 0;
      /// <summary>
      /// If Cached_Index == -2, the parent is not cached and must be read from
      /// the session blob. If Create_Index == -1 and Parent == null, it means that
      /// the Template has not yet been added to a parent Template.
      /// </summary>
      internal TupleProxy CachedParent = null;
   //   internal SessionBlobProxy Cached_Blob; //= null;
//      /// <summary>
//      /// The address inside of the host blob where this document element data (payload) resides.
//      /// </summary>
      #endregion

      /// <summary>
      /// Makes this proxy point at the root tuple in the session blob. Use the Traverse
      /// method to traverse down the tree.
      /// </summary>
      /// <param name="blob">The handle to the session blob</param>
      private void ReferToRootTuple2( IntPtr blob )
      {
         throw new Exception("Not implemented");
//         BlobHandle = blob;
//         OffsetInBlob = SessionBlob.PrivateBlobHeaderSize + /*SessionBlob.internalBlobHeaderSize*/ + SessionBlob.RootTupleParentHeaderSize;
      }

      private void What(int steps) {
      }

      private void TraverseRight(int steps) {
      }

      private void TraverseDown(int steps)
      {
         Address.Add(steps);
      }

      internal unsafe SessionBlob* Pin()
      {
         SessionBlob* buffer;
         SessionBlobProxy.PinBlob(this.BlobHandle,out buffer);
#if DEBUG
//         Assert.True( OffsetInBlob != -1 );
#endif
         buffer += OffsetInBlob;
         return buffer;
      }

      internal void Unpin()
      {
      }

      internal unsafe string GetString(int index)
      {
         return "TODO";
      }

/*      internal TupleProxy Parent
      {
         get
         {
            if (_Index == -2)
               throw new Exception("Parent needs to be read from the session blob");
#if DEBUG
//            Assert.False( _Index == -1 && Cached_Parent != null );
#endif
            return CachedParent;
         }
      }
 */

//      internal SessionBlobProxy Blob
//      {
//         get
//         {
//            if (Cached_Blob == null)
//            {
//               Cached_Blob = new SessionBlobProxy();
//               Cached_Blob.Init(this.BlobHandle);
//            }
//            return Cached_Blob;
//        }
//      }
   }
}
