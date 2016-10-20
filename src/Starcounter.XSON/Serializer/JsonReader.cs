using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Starcounter.Advanced.XSON {
    public enum JsonToken {
        End = 0,
        StartObject = 1,
        EndObject = 2,
        StartArray = 3,
        EndArray = 4,
        PropertyName = 5,
        Value = 6,
        Null = 7
    }

    public unsafe class /*Utf8*/JsonReader {
        private byte* pStream;

        private int streamSize;
        private int position;

        private JsonToken currentToken;
        private byte* propertyNamePtr;
        private bool nextIsPropertyName;
        private int objectCount;
        
        public /*Utf8*/JsonReader(IntPtr stream, int streamSize) {
//            System.Diagnostics.Debugger.Launch();

            this.pStream = (byte*)stream;
            this.streamSize = streamSize;
            this.position = 0;
        }

        public JsonToken ReadNext() {
            byte current;
            JsonToken token = JsonToken.End;

            while (position < streamSize) {
                current = *pStream;
                switch (current) {
                    case (byte)'{': // Object
                        token = JsonToken.StartObject;
                        objectCount++;
                        nextIsPropertyName = true;
                        break;
                    case (byte)'}': // End Object
                        token = JsonToken.EndObject;
                        objectCount--;
                        break;
                    case (byte)'[': // Array
                        token = JsonToken.StartArray;
                        break;
                    case (byte)']': // End Array
                        token = JsonToken.EndArray;
                        break;
                    case (byte)',':
                        if (objectCount > 0)
                            nextIsPropertyName = true;
                        break;
                    case (byte)':':
                        nextIsPropertyName = false;
                        break;
                    case (byte)'\n':
                    case (byte)'\r':
                    case (byte)'\t':
                    case (byte)' ': // Skip, unless in string.
                        break;
                    default: // Any value
                        if (nextIsPropertyName) {
                            nextIsPropertyName = false;
                            propertyNamePtr = pStream;
                            token = JsonToken.PropertyName;
                        } else {
                            token = JsonToken.Value;
                        }
                        break;
                }

                if (token != JsonToken.End)
                    break;

                pStream++;
                position++;
            }

            currentToken = token;
            return token;
        }

        public void Skip(int count) {
            position += count;

            if (position > streamSize) {
                position -= count;
                JsonHelper.ThrowUnexpectedEndOfContentException();
            }

            pStream += count;
        }

        public void SkipCurrent() {
            if (currentToken == JsonToken.StartObject)
                SkipObjectOrArray((byte)'{', (byte)'}');
            else if (currentToken == JsonToken.StartArray)
                SkipObjectOrArray((byte)'[', (byte)']');
            else if (currentToken == JsonToken.PropertyName || currentToken == JsonToken.Value)
                SkipValue();
        }

        private void SkipObjectOrArray(byte startToken, byte endToken) {
            byte current;
            int nrOfTokens = 0;
            bool keepLooking = true;

            while (keepLooking) {
                current = *pStream;
                if (current == startToken) {
                    nrOfTokens++;
                }  else if (current == endToken) {
                    nrOfTokens--;
                    if (nrOfTokens == 0)
                        keepLooking = false;
                }

                position++;
                if (position >= streamSize)
                    JsonHelper.ThrowUnexpectedEndOfContentException();
                pStream++;
            }
        }

        private void SkipValue() {
            bool needsDecoding;
            int valueSize;

            if (*pStream == (byte)'"') {
                valueSize = JsonHelper.SizeToDelimiterOrEndString(pStream + 1, streamSize - position - 1, out needsDecoding) + 2;
            } else {
                valueSize = JsonHelper.SizeToDelimiterOrEnd(pStream, streamSize - position);
            }
            pStream += valueSize;
            position += valueSize;
        }

        public bool ReadBool() {
            int valueSize;
            bool value;

            if (!JsonHelper.ParseBoolean((IntPtr)pStream, streamSize - position, out value, out valueSize))
                JsonHelper.ThrowWrongValueTypeException(null, CurrentPropertyName, "Boolean", ReadString());

            position += valueSize;
            pStream += valueSize;
            return value;
        }

        public decimal ReadDecimal() {
            int valueSize;
            decimal value;

            if (!JsonHelper.ParseDecimal((IntPtr)pStream, streamSize - position, out value, out valueSize))
                JsonHelper.ThrowWrongValueTypeException(null, CurrentPropertyName, "Decimal", ReadString());

            position += valueSize;
            pStream += valueSize;
            return value;
        }

        public double ReadDouble() {
            int valueSize;
            double value;

            if (!JsonHelper.ParseDouble((IntPtr)pStream, streamSize - position, out value, out valueSize))
                JsonHelper.ThrowWrongValueTypeException(null, CurrentPropertyName, "Double", ReadString());

            position += valueSize;
            pStream += valueSize;
            return value;
        }

        public long ReadLong() {
            int valueSize;
            long value;

            if (!JsonHelper.ParseInt((IntPtr)pStream, streamSize - position, out value, out valueSize))
                JsonHelper.ThrowWrongValueTypeException(null, CurrentPropertyName, "Int64", ReadString());

            position += valueSize;
            pStream += valueSize;
            return value;
        }

        public string ReadString() {
            int valueSize;
            string value;

            if (!JsonHelper.ParseString((IntPtr)pStream, streamSize - position, out value, out valueSize))
                JsonHelper.ThrowWrongValueTypeException(null, CurrentPropertyName, "String", ReadString());

            position += valueSize;
            pStream += valueSize;
            return value;
        }

        public void ReadRaw(byte[] target, out int valueSize) {
            byte* pstart = pStream;
            int size = position;
            SkipCurrent();
            size = position - size;

            if (size == -1 || target.Length < size)
                JsonHelper.ThrowUnexpectedEndOfContentException();

            valueSize = size;
            Marshal.Copy((IntPtr)pstart, target, 0, size);
        }

        public int Position {
            get { return position; }
        }

        public IntPtr CurrentPtr {
            get { return (IntPtr)pStream; }
        }

        public int Size {
            get { return streamSize; }
        }

        public JsonReader Clone() {
            var reader = new JsonReader((IntPtr)pStream, streamSize);
            reader.position = position;
            reader.currentToken = currentToken;
            reader.propertyNamePtr = propertyNamePtr;
            reader.nextIsPropertyName = nextIsPropertyName;
            reader.objectCount = objectCount;
            return reader;
        }

        private string CurrentPropertyName {
            get {
                if (this.propertyNamePtr == null)
                    return null;

                string name;
                int valueSize;
                JsonHelper.ParseString((IntPtr)propertyNamePtr, 256, out name, out valueSize);
                return name;
            }
        }
    }
}
