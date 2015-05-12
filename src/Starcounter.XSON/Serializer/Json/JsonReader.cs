using System;
using System.Runtime.InteropServices;

namespace Starcounter.Advanced.XSON {
    public unsafe class JsonReader {
        private byte* pBuffer;
        private int offset;
        private int bufferSize;
        private byte* currentPropertyPtr;
        private bool isArray;

        public JsonReader(IntPtr buffer, int bufferSize) {
            this.pBuffer = (byte*)buffer;
            this.bufferSize = bufferSize;
            this.offset = 0;
            this.currentPropertyPtr = null;
            FindFirstItem();
        }

        public int Used {
            get {
                return offset;
            }
        }

        public int Size {
            get { return this.bufferSize; }
        }

        private string CurrentPropertyName {
            get {
                if (currentPropertyPtr == null)
                    return null;

                string name;
                int valueSize;
                JsonHelper.ParseString((IntPtr)currentPropertyPtr, 256, out name, out valueSize);
                return name;
            }
        }

        public IntPtr CurrentPtr {
            get { return (IntPtr)pBuffer; }
        }

        public JsonReader CreateSubReader() {
            return new JsonReader((IntPtr)pBuffer, bufferSize - offset);
        }

        public void Skip(int value) {
            if (value > (bufferSize - offset))
                JsonHelper.ThrowUnexpectedEndOfContentException();

            pBuffer += value;
            offset += value;
        }

        public int SkipValue() {
            bool needsDecoding = false;
            int size;
            
            if (*pBuffer == '"') {
                size = JsonHelper.SizeToDelimiterOrEndString(pBuffer + 1, bufferSize - offset - 1, out needsDecoding) + 2;
                pBuffer += size;
                offset += size;
            } else if (*pBuffer == '{') {
                size = SkipObject();
            } else if (*pBuffer == '[') {
                size = SkipArray();
            } else {
                size = JsonHelper.SizeToDelimiterOrEnd(pBuffer, bufferSize - offset);
                pBuffer += size;
                offset += size;
            }
            return size;
        }

        private int SkipObject() {
            int before = Used;
            while (GotoProperty()) {
                GotoValue();
                SkipValue();
            }
            Skip(1); // Skip '}'
            return (Used - before);
        }

        private int SkipArray() {
            int before = Used;
            while (GotoNextObject())
                SkipObject();
            Skip(1); // Skip ']'
            return (Used - before);
        }

        public void ReadRaw(byte[] target, out int valueSize) {
            byte* pstart = pBuffer;
            int size = SkipValue();

            if (size == -1 || target.Length < size)
                JsonHelper.ThrowUnexpectedEndOfContentException();

            valueSize = size;
            Marshal.Copy((IntPtr)pstart, target, 0, size);
        }

        public string ReadString() {
            int valueSize;
            string value;

            if (!JsonHelper.ParseString((IntPtr)pBuffer, bufferSize - offset, out value, out valueSize))
                JsonHelper.ThrowWrongValueTypeException(null, CurrentPropertyName, "String", ReadString());

            offset += valueSize;
            if (bufferSize < offset)
                JsonHelper.ThrowUnexpectedEndOfContentException();
            pBuffer += valueSize;

            return value;
        }

        public bool ReadBool() {
            int valueSize;
            bool value;

            if (!JsonHelper.ParseBoolean((IntPtr)pBuffer, bufferSize - offset, out value, out valueSize))
                JsonHelper.ThrowWrongValueTypeException(null, CurrentPropertyName, "Boolean", ReadString());

            offset += valueSize;
            if (bufferSize < offset)
                JsonHelper.ThrowUnexpectedEndOfContentException();
            pBuffer += valueSize;

            return value;
        }

        public decimal ReadDecimal() {
            int valueSize;
            decimal value;

            if (!JsonHelper.ParseDecimal((IntPtr)pBuffer, bufferSize - offset, out value, out valueSize))
                JsonHelper.ThrowWrongValueTypeException(null, CurrentPropertyName, "Decimal", ReadString());

            offset += valueSize;
            if (bufferSize < offset)
                JsonHelper.ThrowUnexpectedEndOfContentException();
            pBuffer += valueSize;

            return value;
        }

        public double ReadDouble() {
            int valueSize;
            double value;

            if (!JsonHelper.ParseDouble((IntPtr)pBuffer, bufferSize - offset, out value, out valueSize))
                JsonHelper.ThrowWrongValueTypeException(null, CurrentPropertyName, "Double", ReadString());

            offset += valueSize;
            if (bufferSize < offset)
                JsonHelper.ThrowUnexpectedEndOfContentException();
            pBuffer += valueSize;

            return value;
        }

        public long ReadLong() {
            int valueSize;
            long value;

            if (!JsonHelper.ParseInt((IntPtr)pBuffer, bufferSize - offset, out value, out valueSize))
                JsonHelper.ThrowWrongValueTypeException(null, CurrentPropertyName, "Int64", ReadString());

            offset += valueSize;
            if (bufferSize < offset)
                JsonHelper.ThrowUnexpectedEndOfContentException();
            pBuffer += valueSize;

            return value;
        }

        public void PopulateObject(Json obj) {
            int valueSize;
            
            valueSize = obj.PopulateFromJson((IntPtr)pBuffer, bufferSize - offset);
            if (valueSize == -1)
                JsonHelper.ThrowUnexpectedEndOfContentException();

            offset += valueSize;
            if (bufferSize < offset)
                JsonHelper.ThrowUnexpectedEndOfContentException();
            pBuffer += valueSize;
        }

        private bool FindFirstItem() {
            byte current;

            isArray = false;
            while (true) {
                current = *pBuffer;

                if (current == '\n' || current == '\r' || current == '\t' || current == ' ') {
                    offset++;
                    if (bufferSize <= offset)
                        JsonHelper.ThrowInvalidJsonException("Beginning of object not found ('{').");
                    pBuffer++;
                    continue;
                } else {
                    if (current == '[') {
                        isArray = true;
                    }
                    return true;
                }
            }
        }

        //private bool FindObject() {
        //    byte current;

        //    while (true) {
        //        current = *pBuffer;

        //        if (current == '{') {
        //            return true;
        //        }

        //        if (current == '\n' || current == '\r' || current == '\t' || current == ' ') {
        //            offset++;
        //            if (bufferSize <= offset)
        //                JsonHelper.ThrowInvalidJsonException("Beginning of object not found ('{').");
        //            pBuffer++;
        //            continue;
        //        } else {
        //            JsonHelper.ThrowInvalidJsonException("Unexpected character found, expected '{' but found '" + (char)current + "'.");
        //        }
        //    }
        //}

        public bool GotoProperty() {
            byte current;

            while (true) {
                if (bufferSize <= offset) {
                    JsonHelper.ThrowUnexpectedEndOfContentException();
                }
                current = *pBuffer;
                
                if (current == '}') {
                    return false;
                }

                if (current == ',' || current == ' '
                    || current == '\n' || current == '\r'
                    || current == '\t' || current == '{') {
                        offset++;
                        pBuffer++;
                        continue;
                }

                // Start of property name
                break;
            }

            currentPropertyPtr = pBuffer;
            return true;
        }

        public void GotoValue() {
            while (*pBuffer != ':') {
                offset++;
                if (bufferSize <= offset)
                    JsonHelper.ThrowUnexpectedEndOfContentException();
                pBuffer++;
            }
            offset++;
            if (bufferSize <= offset)
                JsonHelper.ThrowUnexpectedEndOfContentException();
            pBuffer++; // Skip ':' or ','

            while (*pBuffer == ' ' || *pBuffer == '\n' || *pBuffer == '\r') {
                offset++;
                if (bufferSize <= offset)
                    JsonHelper.ThrowUnexpectedEndOfContentException();
                pBuffer++;
            }
        }

        public bool GotoNextObject() {
            while (true) {
                if (*pBuffer == ']' || (!isArray && *pBuffer == '}')) {
                    return false;
                } else if (*pBuffer == '{') {
                    return true;
                }
                offset++;
                if (bufferSize <= offset)
                    JsonHelper.ThrowUnexpectedEndOfContentException();
                pBuffer++;
            }
        }
    }
}
