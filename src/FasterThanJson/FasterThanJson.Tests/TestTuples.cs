using System;
using NUnit.Framework;
using Starcounter;
using Starcounter.Internal;
using System.Diagnostics;

namespace Starcounter.Internal
{
   [TestFixture]
   public class Hello
   {

      [Test]
      public void World()
      {
         Console.WriteLine("Hello World!");
         Assert.IsTrue( 1+1 == 2 );
      }
   }



   [TestFixture]
   public class TestTuples
   {

      [Test]
      public unsafe void ModifyTuple() {
         const int assumedOffsetElementSize = 2;
         byte* buff;
         byte* blobEnd;
         byte* start;
         IntPtr blob = SessionBlobProxy.CreateBlob(out buff, out blobEnd, out start);
         var root = new TupleWriter(start, 1, assumedOffsetElementSize); // Allocated on the stack. Will be fast.
         
         var t = new TupleWriter(root.AtEnd, 3, assumedOffsetElementSize); // Allocated on the stack. Will be fast.

         t.Write("Joachim");
         t.Write("Wester");

         root.HaveWritten(t.SealTuple());
         root.SealTuple();

        // var tp = new TupleProxy {BlobHandle = blob};
//var tp2 = new TupleProxy() { CachedParent = tp };

      }

      [Test]
      public unsafe void BenchmarkTuples()
      {
         byte* blob;
         byte* blobEnd;
         byte* start;

         IntPtr Blob = SessionBlobProxy.CreateBlob(out blob, out blobEnd, out start);
         uint assumedOffsetElementSize = 2;

         /****** Create tuple ******/

         {

            int cnt = 10000;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            for (int t = 0; t < cnt; t++)
            {
               CreateTuple(start,assumedOffsetElementSize,blobEnd);
            }
            sw.Stop();
            Console.WriteLine(String.Format("Created {0} nested tuples in {1} seconds.", cnt,
                                            (double)sw.ElapsedMilliseconds / 1000));
         }


         /****** Retrieve tuple ******/


         SessionBlobProxy bp = new SessionBlobProxy();
         bp.Init(Blob);
         string str = bp.ToString();

         Console.WriteLine(str);
#if NOPACK
#else
#if BASE32
 //        Assert.AreEqual("2AA1GW]Joachim1FM185 94VAXHOLMWester", str);
#endif
#if BASE64
//         Assert.AreEqual("1X17NTJoachim16D185 94VAXHOLMWester", str);
#endif
#if BASE256
//         Assert.AreEqual("{1}!{1}{7}{23}{29}Joachim{1}{6}{13}185 94VAXHOLMWester", str);
#endif
#endif
         /****** Decode tuple ******/

         var rootParent = new TupleReader(start, 1); // Allocated on the stack. Will be fast.
         var first = new TupleReader(rootParent.AtEnd, 4); // Allocated on the stack. Will be fast.
         string firstName = first.ReadString();
         string lastName = first.ReadString();
         var nested = new TupleReader(first.AtEnd, 2); // Allocated on the stack. Will be fast.
         //nested.Skip();
                  UInt32 phone = nested.ReadUInt();
         //        UInt32 phone = 1234;
         string mobile = nested.ReadString();
         first.Skip();
         string city = first.ReadString();

         Assert.AreEqual("Joachim", firstName);
         Assert.AreEqual("Wester", lastName);
         Assert.AreEqual( 1234, phone);
         Assert.AreEqual("070-2424472", mobile);
         Assert.AreEqual("Stockholm", city);

      }

      public unsafe void CreateTuple(byte* start, uint assumedOffsetElementSize, byte* overflowLimit) {
          var root = new TupleWriter(start, 1, assumedOffsetElementSize); // Allocated on the stack. Will be fast.
          var first = new TupleWriter(root.AtEnd, 4, assumedOffsetElementSize); // Allocated on the stack. Will be fast.

          first.Write("Joachim");
          first.Write("Wester");
          var nested = new TupleWriter(first.AtEnd, 2, assumedOffsetElementSize); // Allocated on the stack. Will be fast.

          nested.Write(1234);
          nested.Write("070-2424472");

          first.HaveWritten(nested.SealTuple());
          first.Write("Stockholm");

          root.HaveWritten(first.SealTuple());
          root.SealTuple();
      }

#if false // Excluded due to use of Newtonsoft.Json
       [Test]
      public static void BenchmarkJson() {
          Test test = new Test() { FirstName = "Hello" };
          Console.WriteLine(JsonConvert.SerializeObject(test));
      }
#endif
   }

   class Test {
       public string FirstName { get; set; }
   }
}

