using System;
using NUnit.Framework;
using Starcounter;
using Starcounter.Internal;
using System.Diagnostics;

namespace FasterThanJson.Tests
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
         var root = new TupleWriterBase64(start, 1, assumedOffsetElementSize); // Allocated on the stack. Will be fast.
         
         var t = new TupleWriterBase64(root.AtEnd, 3, assumedOffsetElementSize); // Allocated on the stack. Will be fast.

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

         var rootParent = new TupleReaderBase64(start, 1); // Allocated on the stack. Will be fast.
         var first = new TupleReaderBase64(rootParent.AtEnd, 4); // Allocated on the stack. Will be fast.
         string firstName = first.ReadString();
         string lastName = first.ReadString();
         var nested = new TupleReaderBase64(first.AtEnd, 2); // Allocated on the stack. Will be fast.
         //nested.Skip();
                  UInt64 phone = nested.ReadUInt();
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
          var root = new TupleWriterBase64(start, 1, assumedOffsetElementSize); // Allocated on the stack. Will be fast.
          var first = new TupleWriterBase64(root.AtEnd, 4, assumedOffsetElementSize); // Allocated on the stack. Will be fast.

          first.Write("Joachim");
          first.Write("Wester");
          var nested = new TupleWriterBase64(first.AtEnd, 2, assumedOffsetElementSize); // Allocated on the stack. Will be fast.

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

      [Test]
      public static unsafe void TestBinaryTuple() {
          // similar to offsetkey with one node
          fixed (byte* start = new byte[1024]) {
              var top = new TupleWriterBase64(start, 3, 2);
              top.Write(1234);
              var s = new TupleWriterBase64(top.AtEnd, 2, 2);
              s.Write(41083);
              s.Write("Static data");
              top.HaveWritten(s.SealTuple());
              var d = new TupleWriterBase64(s.AtEnd, 3, 2);
              d.Write(2);
              d.Write(new byte[] { 123, 0, 255, 2, 32, 255, 0, 0, 1, 14, 123, 231, 0, 255 });
              var nested = new TupleWriterBase64(d.AtEnd, 2, 1);
              nested.Write("dynamic " + 4);
              nested.Write(new byte[] {3, 2, 255, 255, 0, 0, 0, 53, 123});
              d.HaveWritten(nested.SealTuple());
              top.HaveWritten(d.SealTuple());
              top.SealTuple();

              var topReader = new TupleReaderBase64(start, 3);
              Assert.AreEqual(1234, topReader.ReadUInt());
              var sReader = new TupleReaderBase64(topReader.AtEnd, 2);
              Assert.AreEqual(41083, sReader.ReadUInt());
              Assert.AreEqual("Static data", sReader.ReadString());
              topReader.Skip();
              var dReader = new TupleReaderBase64(topReader.AtEnd, 3);
              Assert.AreEqual(2, dReader.ReadUInt());
              Assert.AreEqual(new byte[] { 123, 0, 255, 2, 32, 255, 0, 0, 1, 14, 123, 231, 0, 255 }, dReader.ReadByteArray());
              var nestedReader = new TupleReaderBase64(dReader.AtEnd, 2);
              Assert.AreEqual("dynamic " + 4, nestedReader.ReadString());
              Assert.AreEqual(new byte[] { 3, 2, 255, 255, 0, 0, 0, 53, 123 }, nestedReader.ReadByteArray());

              topReader = new TupleReaderBase64(start, 3);
              topReader.Skip();
              topReader.Skip();
              dReader = new TupleReaderBase64(topReader.AtEnd, 3);
              Assert.AreEqual(2, dReader.ReadUInt());
              Assert.AreEqual(new byte[] { 123, 0, 255, 2, 32, 255, 0, 0, 1, 14, 123, 231, 0, 255 }, dReader.ReadByteArray());
              nestedReader = new TupleReaderBase64(dReader.AtEnd, 2);
              Assert.AreEqual("dynamic " + 4, nestedReader.ReadString());
              Assert.AreEqual(new byte[] { 3, 2, 255, 255, 0, 0, 0, 53, 123 }, nestedReader.ReadByteArray());

          }
      }

      [Test]
      public static unsafe void TestBinaryTupleArray() {
          // similar to offsetkey with one node
          fixed (byte* start = new byte[1024]) {
              var top = new TupleWriterBase64(start, 3, 2);
              top.Write(1234);
              var s = new TupleWriterBase64(top.AtEnd, 2, 2);
              s.Write(41083);
              s.Write("Static data");
              top.HaveWritten(s.SealTuple());
              var d = new TupleWriterBase64(top.AtEnd, 3, 2);
              d.Write(2);
              d.Write(new byte[] { 123, 0, 255, 2, 32, 255, 0, 0, 1, 14, 123, 231, 0, 255 });
              var nested = new TupleWriterBase64(d.AtEnd, 2, 1);
              nested.Write("dynamic " + 4);
              nested.Write(new byte[] { 3, 2, 255, 255, 0, 0, 0, 53, 123 });
              d.HaveWritten(nested.SealTuple());
              top.HaveWritten(d.SealTuple());
              top.SealTuple();

              var topReader = new TupleReaderBase64(start, 3);
              Assert.AreEqual(1234, topReader.ReadUInt());
              var sReader = new TupleReaderBase64(topReader.AtEnd, 2);
              Assert.AreEqual(41083, sReader.ReadUInt());
              Assert.AreEqual("Static data", sReader.ReadString());
              topReader.Skip();
              var dReader = new TupleReaderBase64(topReader.AtEnd, 3);
              Assert.AreEqual(2, dReader.ReadUInt());
              Assert.AreEqual(new byte[] { 123, 0, 255, 2, 32, 255, 0, 0, 1, 14, 123, 231, 0, 255 }, dReader.ReadByteArray());
              var nestedReader = new TupleReaderBase64(dReader.AtEnd, 2);
              Assert.AreEqual("dynamic " + 4, nestedReader.ReadString());
              Assert.AreEqual(new byte[] { 3, 2, 255, 255, 0, 0, 0, 53, 123 }, nestedReader.ReadByteArray());

              topReader = new TupleReaderBase64(start, 3);
              topReader.Skip();
              topReader.Skip();
              dReader = new TupleReaderBase64(topReader.AtEnd, 3);
              Assert.AreEqual(2, dReader.ReadUInt());
              Assert.AreEqual(new byte[] { 123, 0, 255, 2, 32, 255, 0, 0, 1, 14, 123, 231, 0, 255 }, dReader.ReadByteArray());
              nestedReader = new TupleReaderBase64(dReader.AtEnd, 2);
              Assert.AreEqual("dynamic " + 4, nestedReader.ReadString());
              Assert.AreEqual(new byte[] { 3, 2, 255, 255, 0, 0, 0, 53, 123 }, nestedReader.ReadByteArray());

          }
      }

      [Test]
      public static unsafe void TestNullValues() {
          fixed (byte* start = new byte[10]) {
              TupleWriterBase64 tupleWriter = new TupleWriterBase64(start, 2, 1);
              tupleWriter.Write((byte[])null);
#if false // Does not work yet
              tupleWriter.Write((String)null);
#endif
              TupleReaderBase64 tupleReader = new TupleReaderBase64(start, 2);
              byte[] nullByteArray = tupleReader.ReadByteArray();
              Assert.AreEqual(null, nullByteArray);
#if false // Does not work yet
              String nullString = tupleReader.ReadString();
              Assert.AreEqual(null, nullString);
#endif
          }
      }
   }

   class Test {
       public string FirstName { get; set; }
   }
}

