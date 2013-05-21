using System;

namespace Starcounter.XSON.Serializers {
    internal unsafe class ApaJsonReader {
        private byte* pBuffer;
        private int offset;
        private int bufferSize;

        internal ApaJsonReader(IntPtr buffer, int bufferSize) {
            this.pBuffer = (byte*)buffer;
            this.bufferSize = bufferSize;
        }

        internal int Offset {
            get {
                return offset;
            }
        }

        internal string ReadString() {
            int valueSize;
            string value;

            if (!JsonHelper.ParseString((IntPtr)pBuffer, bufferSize - offset, out value, out valueSize))
                throw new Exception("Apapapa");

            pBuffer += valueSize;
            offset += valueSize;

            if (bufferSize < offset)
                throw new Exception("Deserialization failed.");

            return value;
        }

        internal bool ReadBool() {
            int valueSize;
            bool value;

            if (!JsonHelper.ParseBoolean((IntPtr)pBuffer, bufferSize - offset, out value, out valueSize))
                throw new Exception("Apapapa");

            pBuffer += valueSize;
            offset += valueSize;

            if (bufferSize < offset)
                throw new Exception("Deserialization failed.");

            return value;
        }

        internal decimal ReadDecimal() {
            int valueSize;
            decimal value;

            if (!JsonHelper.ParseDecimal((IntPtr)pBuffer, bufferSize - offset, out value, out valueSize))
                throw new Exception("Apapapa");

            pBuffer += valueSize;
            offset += valueSize;

            if (bufferSize < offset)
                throw new Exception("Deserialization failed.");

            return value;
        }

        internal double ReadDouble() {
            int valueSize;
            double value;

            if (!JsonHelper.ParseDouble((IntPtr)pBuffer, bufferSize - offset, out value, out valueSize))
                throw new Exception("Apapapa");

            pBuffer += valueSize;
            offset += valueSize;

            if (bufferSize < offset)
                throw new Exception("Deserialization failed.");

            return value;
        }

        internal long ReadLong() {
            int valueSize;
            long value;

            if (!JsonHelper.ParseInt((IntPtr)pBuffer, bufferSize - offset, out value, out valueSize))
                throw new Exception("Apapapa");

            pBuffer += valueSize;
            offset += valueSize;

            if (bufferSize < offset)
                throw new Exception("Deserialization failed.");

            return value;
        }

        internal void PopulateObject(Obj obj) {
            int valueSize;
            
            valueSize = obj.PopulateFromJson((IntPtr)pBuffer, bufferSize - offset);

            if (valueSize == -1)
                throw new Exception("Apapapa");

            pBuffer += valueSize;
            offset += valueSize;

            if (bufferSize < offset)
                throw new Exception("Deserialization failed.");
        }

        internal bool GotoProperty() {
            while (true) {
                if (*pBuffer == '"')
                    break;
                if (*pBuffer == '}') {
                    offset++;
                    return false;
                }
                pBuffer++;
                offset++;
                if (bufferSize < offset)
                    throw new Exception("Deserialization failed.");
            }
            return true;
        }

        internal void GotoValue() {
            while (*pBuffer != ':') {
                pBuffer++;
                offset++;
                if (bufferSize < offset)
                    throw new Exception("Deserialization failed.");
            }
            pBuffer++; // Skip ':' or ','
            offset++;
            if (bufferSize < offset)
                throw new Exception("Deserialization failed.");

            while (*pBuffer == ' ' || *pBuffer == '\n' || *pBuffer == '\r') {
                pBuffer++;
                offset++;
                if (bufferSize < offset)
                    throw new Exception("Deserialization failed.");
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
                pBuffer++;
                offset++;
                if (bufferSize < offset)
                    throw new Exception("Deserialization failed.");
            }
        }
    }
}
