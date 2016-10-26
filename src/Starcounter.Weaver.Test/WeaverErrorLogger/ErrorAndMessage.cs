using System;
using System.IO;

namespace Starcounter.Weaver.Test
{
    internal class ErrorAndMessage
    {
        const string Envelope = "5985EF3E-CD73-413A-B8A9-AAE88B5EE6CD";

        public uint Code { get; set; }
        public string Message { get; set; }

        public void WriteTo(StreamWriter writer)
        {
            writer.WriteLine(Envelope);
            writer.WriteLine($"{Code}:{Message}");
            writer.WriteLine(Envelope);
        }

        public void ReadFrom(string serialized)
        {
            var index = serialized.IndexOf(":");
            Code = uint.Parse(serialized.Substring(0, index));
            Message = serialized.Substring(index + 1);
        }

        public static ErrorAndMessage ReadFrom(StreamReader reader)
        {
            var first = reader.ReadLine();
            if (string.IsNullOrEmpty(first) || first != Envelope)
            {
                return null;
            }
            
            var head = reader.ReadLine();

            var error = new ErrorAndMessage();
            var index = head.IndexOf(":");
            error.Code = uint.Parse(head.Substring(0, index));
            error.Message = head.Substring(index + 1);

            var carry = reader.ReadLine();
            while (carry != Envelope)
            {
                error.Message += Environment.NewLine + carry;
                carry = reader.ReadLine();
            }

            return error;
        }
    }
}
