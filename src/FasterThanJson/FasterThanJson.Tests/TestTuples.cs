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

         t.WriteString("Joachim");
         t.WriteString("Wester");

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
         int assumedOffsetElementSize = 2;

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
                  UInt64 phone = nested.ReadULong();
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

      public unsafe void CreateTuple(byte* start, int assumedOffsetElementSize, byte* overflowLimit) {
          var root = new TupleWriterBase64(start, 1, assumedOffsetElementSize); // Allocated on the stack. Will be fast.
          var first = new TupleWriterBase64(root.AtEnd, 4, assumedOffsetElementSize); // Allocated on the stack. Will be fast.

          first.WriteString("Joachim");
          first.WriteString("Wester");
          var nested = new TupleWriterBase64(first.AtEnd, 2, assumedOffsetElementSize); // Allocated on the stack. Will be fast.

          nested.WriteULong(1234);
          nested.WriteString("070-2424472");

          first.HaveWritten(nested.SealTuple());
          first.WriteString("Stockholm");

          root.HaveWritten(first.SealTuple());
          root.SealTuple();
      }

      [Test]
      public static unsafe void TestBinaryTuple() {
          // similar to offsetkey with one node
          fixed (byte* start = new byte[1024]) {
              var top = new TupleWriterBase64(start, 3, 2);
              top.WriteULong(1234);
              var s = new TupleWriterBase64(top.AtEnd, 2, 2);
              s.WriteULong(41083);
              s.WriteString("Static data");
              top.HaveWritten(s.SealTuple());
              var d = new TupleWriterBase64(s.AtEnd, 3, 2);
              d.WriteULong(2);
              d.WriteByteArray(new byte[] { 123, 0, 255, 2, 32, 255, 0, 0, 1, 14, 123, 231, 0, 255 });
              var nested = new TupleWriterBase64(d.AtEnd, 2, 1);
              nested.WriteString("dynamic " + 4);
              nested.WriteByteArray(new byte[] {3, 2, 255, 255, 0, 0, 0, 53, 123});
              d.HaveWritten(nested.SealTuple());
              top.HaveWritten(d.SealTuple());
              top.SealTuple();

              var topReader = new TupleReaderBase64(start, 3);
              Assert.AreEqual(1234, topReader.ReadULong());
              var sReader = new TupleReaderBase64(topReader.AtEnd, 2);
              Assert.AreEqual(41083, sReader.ReadULong());
              Assert.AreEqual("Static data", sReader.ReadString());
              topReader.Skip();
              var dReader = new TupleReaderBase64(topReader.AtEnd, 3);
              Assert.AreEqual(2, dReader.ReadULong());
              Assert.AreEqual(new byte[] { 123, 0, 255, 2, 32, 255, 0, 0, 1, 14, 123, 231, 0, 255 }, dReader.ReadByteArray());
              var nestedReader = new TupleReaderBase64(dReader.AtEnd, 2);
              Assert.AreEqual("dynamic " + 4, nestedReader.ReadString());
              Assert.AreEqual(new byte[] { 3, 2, 255, 255, 0, 0, 0, 53, 123 }, nestedReader.ReadByteArray());

              topReader = new TupleReaderBase64(start, 3);
              topReader.Skip();
              topReader.Skip();
              dReader = new TupleReaderBase64(topReader.AtEnd, 3);
              Assert.AreEqual(2, dReader.ReadULong());
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
              top.WriteULong(1234);
              var s = new TupleWriterBase64(top.AtEnd, 2, 2);
              s.WriteULong(41083);
              s.WriteString("Static data");
              top.HaveWritten(s.SealTuple());
              var d = new TupleWriterBase64(top.AtEnd, 3, 2);
              d.WriteULong(2);
              d.WriteByteArray(new byte[] { 123, 0, 255, 2, 32, 255, 0, 0, 1, 14, 123, 231, 0, 255 });
              var nested = new TupleWriterBase64(d.AtEnd, 3, 1);
              nested.WriteString("dynamic " + 4);
              nested.WriteByteArray(new byte[] { 3, 2, 255, 255, 0, 0, 0, 53, 123 });
              nested.WriteLong(-1235);
              d.HaveWritten(nested.SealTuple());
              top.HaveWritten(d.SealTuple());
              top.SealTuple();

              var topReader = new TupleReaderBase64(start, 3);
              Assert.AreEqual(1234, topReader.ReadULong());
              var sReader = new TupleReaderBase64(topReader.AtEnd, 2);
              Assert.AreEqual(41083, sReader.ReadULong());
              Assert.AreEqual("Static data", sReader.ReadString());
              topReader.Skip();
              var dReader = new TupleReaderBase64(topReader.AtEnd, 3);
              Assert.AreEqual(2, dReader.ReadULong());
              Assert.AreEqual(new byte[] { 123, 0, 255, 2, 32, 255, 0, 0, 1, 14, 123, 231, 0, 255 }, dReader.ReadByteArray());
              var nestedReader = new TupleReaderBase64(dReader.AtEnd, 3);
              Assert.AreEqual("dynamic " + 4, nestedReader.ReadString());
              Assert.AreEqual(new byte[] { 3, 2, 255, 255, 0, 0, 0, 53, 123 }, nestedReader.ReadByteArray());
              Assert.AreEqual(-1235, nestedReader.ReadLong());

              topReader = new TupleReaderBase64(start, 3);
              topReader.Skip();
              topReader.Skip();
              dReader = new TupleReaderBase64(topReader.AtEnd, 3);
              Assert.AreEqual(2, dReader.ReadULong());
              Assert.AreEqual(new byte[] { 123, 0, 255, 2, 32, 255, 0, 0, 1, 14, 123, 231, 0, 255 }, dReader.ReadByteArray());
              nestedReader = new TupleReaderBase64(dReader.AtEnd, 3);
              Assert.AreEqual("dynamic " + 4, nestedReader.ReadString());
              Assert.AreEqual(new byte[] { 3, 2, 255, 255, 0, 0, 0, 53, 123 }, nestedReader.ReadByteArray());
              Assert.AreEqual(-1235, nestedReader.ReadLong());
          }
      }

      [Test]
      public static unsafe void TestNullValues() {
          fixed (byte* start = new byte[17]) {
              TupleWriterBase64 tupleWriter = new TupleWriterBase64(start, 8, 1);
              tupleWriter.WriteByteArray(null);
              tupleWriter.WriteString("");
              tupleWriter.WriteString((String)null);
              tupleWriter.WriteLongNullable(null);
              tupleWriter.WriteULongNullable(null);
              tupleWriter.WriteString("".ToCharArray());
              tupleWriter.WriteByteArray(null);
              tupleWriter.WriteString((char[])null);
              TupleReaderBase64 tupleReader = new TupleReaderBase64(start, 8);
              byte[] nullByteArray = tupleReader.ReadByteArray();
              Assert.AreEqual(null, nullByteArray);
              Assert.AreEqual("", tupleReader.ReadString());
              String nullString = tupleReader.ReadString();
              Assert.AreEqual(null, nullString);
              Assert.AreEqual(null, tupleReader.ReadLongNullable());
              Assert.AreEqual(null, tupleReader.ReadULongNullable());
              char[] value = new char[2];
              var len = tupleReader.ReadString(value);
              Assert.AreEqual("".ToCharArray().Length, len);
              byte[] byteVal = new byte[2];
              Assert.AreEqual(-1, tupleReader.ReadByteArray(byteVal));
              Assert.AreEqual(-1, tupleReader.ReadString(value));
          }
      }

      [Test]
      public static unsafe void TestSignedInt() {
          fixed (byte* start = new byte[25]) {
              TupleWriterBase64 tupleWriter = new TupleWriterBase64(start, 2, 1);
              tupleWriter.WriteLong(Int64.MaxValue);
              tupleWriter.WriteLong(Int64.MinValue);
              TupleReaderBase64 tupleReader = new TupleReaderBase64(start, 2);
              Assert.AreEqual(Int64.MaxValue, tupleReader.ReadLong());
              Assert.AreEqual(Int64.MinValue, tupleReader.ReadLong());
          }
      }

      [Test]
      public static unsafe void TestSignedIntNullable() {
          fixed (byte* start = new byte[31]) {
              TupleWriterBase64 tupleWriter = new TupleWriterBase64(start, 5, 1);
              tupleWriter.WriteLongNullable(Int64.MaxValue);
              tupleWriter.WriteLongNullable(Int64.MinValue);
              tupleWriter.WriteLongNullable(0);
              tupleWriter.WriteLongNullable(-1);
              tupleWriter.WriteLongNullable(1);
              TupleReaderBase64 tupleReader = new TupleReaderBase64(start, 5);
              Assert.AreEqual(Int64.MaxValue, tupleReader.ReadLongNullable());
              Assert.AreEqual(Int64.MinValue, tupleReader.ReadLongNullable());
              Assert.AreEqual(0, tupleReader.ReadLongNullable());
              Assert.AreEqual(-1, tupleReader.ReadLongNullable());
              Assert.AreEqual(1, tupleReader.ReadLongNullable());
          }
      }

      [Test]
      public static unsafe void TestDecimal() {
          fixed(byte* start = new byte[62]) {
              TupleWriterBase64 tupleWriter = new TupleWriterBase64(start, 6, 1);
              tupleWriter.WriteDecimalLossless(-2m); // 2 chars
              tupleWriter.WriteDecimalLossless((decimal)UInt64.MaxValue + UInt32.MaxValue); // 18 chars
              tupleWriter.WriteDecimalLossless(-(decimal)UInt64.MaxValue - UInt32.MaxValue); // 18 chars
              tupleWriter.WriteDecimalLossless(((decimal)UInt64.MaxValue + UInt32.MaxValue)/100000000000000000); // 18 chars
              tupleWriter.WriteDecimalLossless(0m); // 2 chars
              tupleWriter.WriteDecimalLossless(-0.00000023432523m); // 4 chars
              TupleReaderBase64 tupleReader = new TupleReaderBase64(start, 6);
              Assert.AreEqual(-2m, tupleReader.ReadDecimalLossless());
              Assert.AreEqual((decimal)UInt64.MaxValue + UInt32.MaxValue, tupleReader.ReadDecimalLossless());
              Assert.AreEqual(-(decimal)UInt64.MaxValue - UInt32.MaxValue, tupleReader.ReadDecimalLossless());
              Assert.AreEqual(((decimal)UInt64.MaxValue + UInt32.MaxValue) / 100000000000000000, tupleReader.ReadDecimalLossless());
              Assert.AreEqual(0m, tupleReader.ReadDecimalLossless());
              Assert.AreEqual(-0.00000023432523m, tupleReader.ReadDecimalLossless());
          }
      }
   }

   class Test {
       public string FirstName { get; set; }
   }
}

