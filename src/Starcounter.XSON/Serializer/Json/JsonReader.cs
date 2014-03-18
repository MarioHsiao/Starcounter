using System;
using System.Runtime.InteropServices;

namespace Starcounter.Advanced.XSON {
    internal unsafe class JsonReader {
        private byte* pBuffer;
        private int offset;
        private int bufferSize;
        private byte* currentPropertyPtr;
        private bool isArray;

        internal JsonReader(IntPtr buffer, int bufferSize) {
            this.pBuffer = (byte*)buffer;
            this.bufferSize = bufferSize;
            this.offset = 0;
            this.currentPropertyPtr = null;
            FindFirstItem();
        }

        internal int Used {
            get {
                // The offset is zero-bound so we add one to get the correct number of bytes read.
                return offset + 1;
            }
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

        internal IntPtr CurrentPtr {
            get { return (IntPtr)pBuffer; }
        }

        internal JsonReader CreateSubReader() {
            return new JsonReader((IntPtr)pBuffer, bufferSize - offset);
        }

        internal void Skip(int value) {
            if (value > (bufferSize - offset))
                JsonHelper.ThrowUnexpectedEndOfContentException();

            pBuffer += value;
            offset += value;
        }

        internal int SkipValue() {
            bool needsDecoding = false;
            int size;
            
            if (*pBuffer == '"') {
                size = JsonHelper.SizeToDelimiterOrEndString(pBuffer + 1, bufferSize - offset - 1, out needsDecoding) + 2;
            } else {
               size = JsonHelper.SizeToDelimiterOrEnd(pBuffer, bufferSize - offset);
            }

            pBuffer += size;
            offset += size;
            return size;
        }

        internal void ReadRaw(byte[] target, out int valueSize) {
            byte* pstart = pBuffer;
            int size = SkipValue();

            if (size == -1 || target.Length < size)
                JsonHelper.ThrowUnexpectedEndOfContentException();

            valueSize = size;
            Marshal.Copy((IntPtr)pstart, target, 0, size);
        }

        internal string ReadString() {
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

        internal bool ReadBool() {
            int valueSize;
            bool value;

            if (!JsonHelper.ParseBoolean((IntPtr)pBuffer, bufferSize - offset, out value, out valueSize))
                JsonHelper.ThrowWrongValueTypeException(null, CurrentPropertyName, "Boolean", ReadString());

            offset += valueSize;
            if (bufferSize <= offset)
                JsonHelper.ThrowUnexpectedEndOfContentException();
            pBuffer += valueSize;

            return value;
        }

        internal decimal ReadDecimal() {
            int valueSize;
            decimal value;

            if (!JsonHelper.ParseDecimal((IntPtr)pBuffer, bufferSize - offset, out value, out valueSize))
                JsonHelper.ThrowWrongValueTypeException(null, CurrentPropertyName, "Decimal", ReadString());

            offset += valueSize;
            if (bufferSize <= offset)
                JsonHelper.ThrowUnexpectedEndOfContentException();
            pBuffer += valueSize;

            return value;
        }

        internal double ReadDouble() {
            int valueSize;
            double value;

            if (!JsonHelper.ParseDouble((IntPtr)pBuffer, bufferSize - offset, out value, out valueSize))
                JsonHelper.ThrowWrongValueTypeException(null, CurrentPropertyName, "Double", ReadString());

            offset += valueSize;
            if (bufferSize <= offset)
                JsonHelper.ThrowUnexpectedEndOfContentException();
            pBuffer += valueSize;

            return value;
        }

        internal long ReadLong() {
            int valueSize;
            long value;

            if (!JsonHelper.ParseInt((IntPtr)pBuffer, bufferSize - offset, out value, out valueSize))
                JsonHelper.ThrowWrongValueTypeException(null, CurrentPropertyName, "Int64", ReadString());

            offset += valueSize;
            if (bufferSize <= offset)
                JsonHelper.ThrowUnexpectedEndOfContentException();
            pBuffer += valueSize;

            return value;
        }

        internal void PopulateObject(Json obj) {
            int valueSize;
            
            valueSize = obj.PopulateFromJson((IntPtr)pBuffer, bufferSize - offset);
            if (valueSize == -1)
                JsonHelper.ThrowUnexpectedEndOfContentException();

            offset += valueSize;
            if (bufferSize <= offset)
                JsonHelper.ThrowUnexpectedEndOfContentException();
            pBuffer += valueSize;
        }

        private bool FindFirstItem() {
            byte current;

            isArray = false;
            while (true) {
                current = *pBuffer;

                if (current == '{') {
                    return true;
                }

                if (current == '[') {
                    isArray = true;
                    return true;
                }

                if (current == '\n' || current == '\r' || current == '\t' || current == ' ') {
                    offset++;
                    if (bufferSize <= offset)
                        JsonHelper.ThrowInvalidJsonException("Beginning of object not found ('{').");
                    pBuffer++;
                    continue;
                } else {
                    JsonHelper.ThrowInvalidJsonException("Unexpected character found, expected '{' but found '" + (char)current + "'.");
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

        internal bool GotoProperty() {
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

        internal void GotoValue() {
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

        internal bool GotoNextObject() {
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
