using System;

namespace Starcounter.XSON.Serializers {
    internal unsafe class JsonReader {
        private byte* pBuffer;
        private int offset;
        private int bufferSize;
        private string currentPropertyName;

        internal JsonReader(IntPtr buffer, int bufferSize) {
            this.pBuffer = (byte*)buffer;
            this.bufferSize = bufferSize;
            this.offset = 0;
            FindObject();
        }

        internal int Used {
            get {
                // The offset is zero-bound so we add one to get the correct number of bytes read.
                return offset + 1;
            }
        }

        internal string ReadString() {
            int valueSize;
            string value;

            if (!JsonHelper.ParseString((IntPtr)pBuffer, bufferSize - offset, out value, out valueSize))
                JsonHelper.ThrowWrongValueTypeException(null, currentPropertyName, "String", ReadString());

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
                JsonHelper.ThrowWrongValueTypeException(null, currentPropertyName, "Boolean", ReadString());

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
                JsonHelper.ThrowWrongValueTypeException(null, currentPropertyName, "Decimal", ReadString());

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
                JsonHelper.ThrowWrongValueTypeException(null, currentPropertyName, "Double", ReadString());

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
                JsonHelper.ThrowWrongValueTypeException(null, currentPropertyName, "Int64", ReadString());

            offset += valueSize;
            if (bufferSize <= offset)
                JsonHelper.ThrowUnexpectedEndOfContentException();
            pBuffer += valueSize;

            return value;
        }

        internal void PopulateObject(Obj obj) {
            int valueSize;
            
            valueSize = obj.PopulateFromJson((IntPtr)pBuffer, bufferSize - offset);
            if (valueSize == -1)
                JsonHelper.ThrowUnexpectedEndOfContentException();

            offset += valueSize;
            if (bufferSize <= offset)
                JsonHelper.ThrowUnexpectedEndOfContentException();
            pBuffer += valueSize;
        }

        private bool FindObject() {
            byte current;

            while (true) {
                current = *pBuffer;

                if (current == '{')
                    return true;

                if (current == '\n' || current == '\r' || current == '\t') {
                    offset++;
                    if (bufferSize <= offset)
                        JsonHelper.ThrowInvalidJsonException("Beginning of object not found ('{').");
                    pBuffer++;
                    continue;
                } else {
                    JsonHelper.ThrowInvalidJsonException("Unexpected character found, was expecting '{' but found '" + (char)current + "'");
                }
            }
        }

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

            currentPropertyName = ReadString();
            return true;
        }

        internal string CurrentPropertyName {
            get {
                return currentPropertyName;
            }
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

        internal bool GotoNextObjectInArray() {
            while (true) {
                if (*pBuffer == ']') {
                    pBuffer++;
                    offset++;
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
