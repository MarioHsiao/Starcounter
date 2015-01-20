// ***********************************************************************
// <copyright file="JsonPatch.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Advanced.XSON;
using Starcounter.Internal;
using Starcounter.Internal.XSON;
using Starcounter.Templates;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Starcounter.XSON {
    /// <summary>
    /// Class for evaluating, handling and creating json-patch to and from typed json objects 
    /// and logged changes done in a typed json object during a request.
    /// 
    /// The json-patch is implemented according to http://tools.ietf.org/html/rfc6902
    /// </summary>
    public class JsonPatch {
        private static string serverVersionPropertyName = "_ver#s";
        private static string clientVersionPropertyName = "_ver#c$";
        private static byte[] serverVersionPath = Encoding.UTF8.GetBytes("/" + serverVersionPropertyName);
        private static byte[] clientVersionPath = Encoding.UTF8.GetBytes("/" + clientVersionPropertyName);
        private static byte[] testClientVersionPatch = Encoding.UTF8.GetBytes(@"{""op"":""test"",""path"":""/" + clientVersionPropertyName + @""",""value"":");
        private static byte[] replaceServerVersionPatch = Encoding.UTF8.GetBytes(@"{""op"":""replace"",""path"":""/" + serverVersionPropertyName + @""",""value"":");

        private static byte[][] _patchOpUtf8Arr;
        private static byte[] _emptyPatchArr = { (byte)'[', (byte)']' };

        private Action<Session, JsonPatchOperation, JsonPointer, IntPtr, int> patchHandler = DefaultPatchHandler.Handle;
        
        private enum JsonPatchMember {
            Invalid,
            Op,
            Path,
            Value
        }

        static JsonPatch() {
            _patchOpUtf8Arr = new byte[5][];
            _patchOpUtf8Arr[(int)JsonPatchOperation.Undefined] = Encoding.UTF8.GetBytes("undefined");
            _patchOpUtf8Arr[(int)JsonPatchOperation.Remove] = Encoding.UTF8.GetBytes("remove");
            _patchOpUtf8Arr[(int)JsonPatchOperation.Replace] = Encoding.UTF8.GetBytes("replace");
            _patchOpUtf8Arr[(int)JsonPatchOperation.Add] = Encoding.UTF8.GetBytes("add");
            _patchOpUtf8Arr[(int)JsonPatchOperation.Test] = Encoding.UTF8.GetBytes("test");    
        }

        public JsonPatch() {
        }

        public void SetPatchHandler(Action<Session, JsonPatchOperation, JsonPointer, IntPtr, int> handler) {
            patchHandler = handler;
        }

        public static string ServerVersionPropertyName {
            get { return serverVersionPropertyName; }
            set { serverVersionPropertyName = value; }
        }

        public static string ClientVersionPropertyName {
            get { return clientVersionPropertyName; }
            set { clientVersionPropertyName = value; }
        }

        /// <summary>
        /// Generates a JSON-Patch array for all changes made to the session data
        /// </summary>
        /// <param name="flushLog">If true, the change log will be reset</param>
        /// <returns>The JSON-Patch string (see RFC6902)</returns>
        public string CreateJsonPatch(Session session, bool flushLog) {
            byte[] patchArr;
            int size = CreateJsonPatchBytes(session, flushLog, out patchArr);
            return Encoding.UTF8.GetString(patchArr, 0, size);
        }

        /// <summary>
        /// Generates a JSON-Patch array for all changes made to the session data
        /// </summary>
        /// <param name="flushLog">If true, the change log will be reset</param>
        /// <returns>The JSON-Patch string (see RFC6902)</returns>
        public int CreateJsonPatchBytes(Session session, bool flushLog, out byte[] patches) {
            int patchSize;
            List<Change> changes;

            session.GenerateChangeLog();
            changes = session.GetChanges();

            //if (changes.Count == 0) {
            //    patches = _emptyPatchArr;
            //    return _emptyPatchArr.Length;
            //}

            patchSize = CreatePatches(session, changes, out patches);
            if (flushLog)
                session.Clear();
        
            return patchSize;
        }

        /// <summary>
        /// Creates an array of jsonpatches as a bytearray.
        /// </summary>
        /// <param name="changeLog"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        internal int CreatePatches(Session session, List<Change> changes, out byte[] patches) {
            byte[] buffer;
            int size;
            int[] pathSizes;
            Utf8Writer writer;
            bool versioning = session.CheckOption(SessionOptions.PatchVersioning);

            // TODO:
            // We dont want to create a new array here...
            pathSizes = new int[changes.Count];

            size = 2;
            size += pathSizes.Length;

            for (int i = 0; i < changes.Count; i++) {
                size += CalculateSize(changes[i], out pathSizes[i]);
            }

            if (versioning) {
                // If versioning is enabled two patches are fixed: test clientversion and replace serverversion.
                size += testClientVersionPatch.Length + GetSizeOfIntAsUtf8(session.ClientVersion) + 2;
                size += replaceServerVersionPatch.Length + GetSizeOfIntAsUtf8(session.ServerVersion) + 2;
                
            }    
            buffer = new byte[size];
            
            unsafe {
                fixed (byte* pbuf = buffer) {
                    writer = new Utf8Writer(pbuf);
                    writer.Write('[');

                    if (versioning) {
                        writer.Write(replaceServerVersionPatch);
                        writer.Write(session.ServerVersion);
                        writer.Write('}');
                        writer.Write(',');

                        writer.Write(testClientVersionPatch);
                        writer.Write(session.ClientVersion);
                        writer.Write('}');

                        if (changes.Count > 0) {
                            writer.Write(',');
                        }
                    }

                    for (int i = 0; i < changes.Count; i++) {
                        var change = changes[i];
                        WritePatch(change, ref writer, pathSizes[i]);

                        if (change.Property != null) {
                            Json parent = change.Parent;
                            parent.Scope<Json, TValue>((p, t) => {
                                t.Checkpoint(p);
                            }, parent, change.Property);
                        }

                        if ((i + 1) < changes.Count)
                            writer.Write(',');
                    }
                    writer.Write(']');

                    // Setting the actual size, since the first is just estimated.
                    size = writer.Written;
                }
            }

            patches = buffer;
            return size;
        }

        /// <summary>
        /// Builds the json patch.
        /// </summary>
        /// <param name="patchType">Type of the patch.</param>
        /// <param name="nearestApp">The nearest app.</param>
        /// <param name="from">From.</param>
        /// <param name="index">The index.</param>
        /// <returns>String.</returns>
        private void WritePatch(Change change, ref Utf8Writer writer, int pathSize) {
			Json childJson = null;

            // TODO:
            // dont write static strings as strings. convert them once and copy arrays.
            writer.Write("{\"op\":\"");
            writer.Write(_patchOpUtf8Arr[change.ChangeType]);
            writer.Write("\",\"path\":\"");

            if (change.Property != null) {
                WritePath(ref writer, change, pathSize);
            } else {
                childJson = change.Parent;
            }
            writer.Write('"');
            if (change.ChangeType != (int)JsonPatchOperation.Remove) {

                writer.Write(",\"value\":");
                if (childJson == null && change.Property is TContainer) {
                    childJson = (Json)change.Property.GetUnboundValueAsObject(change.Parent);
                    if (change.Index != -1)
                        childJson = change.Item;

                    if (childJson == null) {
                        writer.Write('{');
                        writer.Write('}');
                    }
                }

                if (childJson != null) {
                    writer.Write(childJson.ToJsonUtf8());
                    childJson.SetBoundValuesInTuple();
                } else {
                    // TODO: 
                    // Should write value directly to buffer.
                    Json parent = change.Parent;
                    string value = 
                        parent.Scope<Json, TValue, string>(
                            (p, t) => { return t.ValueToJsonString(p); },
                            parent,
                            change.Property
                        );
                    writer.Write(value);
                }
            }
            writer.Write('}');
        }

        private static int EstimatePropertyValueSizeInBytes(TValue property, Json parent, int index, Json item) {
            int sizeBytes = 0;

            if (property is TLong) {
                sizeBytes += 32;
            } else if (property is TString) {
                string s = ((TString)property).Getter(parent);
                if (s == null)
                    sizeBytes += 2; // 2 for quotation marks around string.
                else
                    sizeBytes += s.Length * 2 + 2; // 2 for quotation marks around string.
            } else if (property is TBool) {
                sizeBytes += 5;
            } else if (property is TDecimal) {
                sizeBytes += 32;
            } else if (property is TDouble) {
                sizeBytes += 32;
            } else if (property is TTrigger) {
                sizeBytes += 4;
            } else if (property is TContainer) {
                var childJson = (Json)property.GetUnboundValueAsObject(parent);
                if (index != -1) {
                    childJson = item;
                }
                if (childJson != null)
                    sizeBytes = ((TContainer)childJson.Template).JsonSerializer.EstimateSizeBytes(childJson);
                else
                    sizeBytes = 2;
            } else if (property == null) {
                sizeBytes = ((TContainer)parent.Template).JsonSerializer.EstimateSizeBytes(parent);
            }

            return sizeBytes;
        }

        internal static int CalculateSize(Change change, out int pathSize) {
            int size;

            // {"op":"???","path":"???","value":???}
            // size = 7 + op + 10 + path + 10 + value + 1 => 28 + var

            // {"op":"???","path":"???"}
            // size = 7 + op + 10 + path + 2 => 19 + var
            
            size = 19;
            size += _patchOpUtf8Arr[change.ChangeType].Length;

            pathSize = CalculateSizeOfPath(change.Parent, false);
            if (change.Property != null)
                pathSize += change.Property.TemplateName.Length;
            else
                pathSize--; // Path for root is empty, ""

            if (change.Index != -1)
                pathSize += GetSizeOfIntAsUtf8(change.Index) + 1;
            size += pathSize;

            if (change.ChangeType != (int)JsonPatchOperation.Remove) {
                size += 9;
                size += change.Parent.Scope<Change, int>(
                            (Change c) => {
                                return EstimatePropertyValueSizeInBytes(c.Property, c.Parent, c.Index, c.Item);
                            },
                            change);
            }
            return size;
        }

        private static int CalculateSizeOfPath(Json json, bool fromStepParent) {
            int size;
            Json parent;
            Template template;

            size = 0;
            if (json._stepParent != null) {
                fromStepParent = true;
                parent = json._stepParent;
                size += json.GetAppName().Length + 1;
            } else {
                parent = json.Parent;

//                if (!fromStepParent && json._stepSiblings != null && json._stepSiblings.Count > 0) {
                if (!fromStepParent && json._appName != null) {
                    size += json._appName.Length + 1;
                }
                fromStepParent = false;

                size += 1;
                if (parent != null) {
                    if (parent.IsArray) {
                        if (json._cacheIndexInArr == -1)
                            json.UpdateCachedIndex();
                        size += GetSizeOfIntAsUtf8(json._cacheIndexInArr);
                    } else {
                        // We use the cacheIndexInArr to keep track of obj that is set
                        // in the parent as an untyped object since the template here is not
                        // the template in the parent (which we want).
                        if (json._cacheIndexInArr != -1) {
                            template = ((TObject)parent.Template).Properties[json._cacheIndexInArr];
                        } else {
                            template = json.Template;
                        }
                        size += template.TemplateName.Length;
                    }
                }   
            }

            if (parent != null)
                size += CalculateSizeOfPath(parent, fromStepParent);

            return size;
        }

        private void WritePath(ref Utf8Writer writer, Change change, int pathSize) {
            int sizeToWrite = change.Property.TemplateName.Length + 1;
            
            if (change.Index != -1) {
                sizeToWrite += GetSizeOfIntAsUtf8(change.Index) + 1;
            }

            writer.Skip(pathSize - sizeToWrite);
            writer.Write('/');
            writer.Write(change.Property.TemplateName);

            if (change.Index != -1) {
                writer.Write('/');
                writer.Write(change.Index);
            }

            int positionAfter = writer.Written;
            WritePath_2(ref writer, change.Parent, sizeToWrite, false);
            if (positionAfter != writer.Written) {
                writer.Skip(positionAfter - writer.Written);
            }
        }

        private void WritePath_2(ref Utf8Writer writer, Json json, int prevSize, bool fromStepParent) {
            int size;
            Json parent;
            Template template;

            if (json._stepParent != null) {
                parent = json._stepParent;
                size = json.GetAppName().Length + 1;
                writer.Skip(-(size + prevSize));
                writer.Write('/');
                writer.Write(json.GetAppName());
                fromStepParent = true;
            } else {
                size = 0;
                parent = json.Parent;
                if (parent != null) {
//                    if (!fromStepParent && json._stepSiblings != null && json._stepSiblings.Count > 0) {
                    if (!fromStepParent &&  json._appName != null) {
                        size = json.GetAppName().Length + 1;
                        writer.Skip(-(size + prevSize));
                        writer.Write('/');
                        writer.Write(json._appName);
                        prevSize = size;
                    }
                    fromStepParent = false;

                    if (parent.IsArray) {
                        if (json._cacheIndexInArr == -1)
                            json.UpdateCachedIndex();

                        size = GetSizeOfIntAsUtf8(json._cacheIndexInArr) + 1;
                        writer.Skip(-(size + prevSize));
                        writer.Write('/');
                        writer.Write(json._cacheIndexInArr);
                    } else {
                        // We use the cacheIndexInArr to keep track of obj that is set
                        // in the parent as an untyped object since the template here is not
                        // the template in the parent (which we want).
                        if (json._cacheIndexInArr != -1) {
                            template = ((TObject)parent.Template).Properties[json._cacheIndexInArr];
                        } else {
                            template = json.Template;
                        }

                        size = template.TemplateName.Length + 1;
                        writer.Skip(-(size + prevSize));
                        writer.Write('/');
                        writer.Write(template.TemplateName);
                    }
                }
            }

            if (parent != null)
                WritePath_2(ref writer, parent, size, fromStepParent);
        }

        private static int GetSizeOfIntAsUtf8(long value) {
            int size = 0;
            do {
                value = value / 10;
                size++;
            } while (value > 0);

            return size;
        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="session">The session the root json is retrieved from.</param>
        ///// <param name="patchArray">The bytearray containing all patches.</param>
        ///// <returns>The number of patches evaluated.</returns>
        public int EvaluatePatches(Session session, byte[] patchArray) {
            int patchCount;

            unsafe {
                fixed (byte* pBody = patchArray) {
                    patchCount = EvaluatePatches(session, (IntPtr)pBody, patchArray.Length);
                }
            }
            return patchCount;
        }

        /// <summary>
        /// Evaluates the patches and calls the appropriate inputhandler.
        /// </summary>
        /// <param name="rootApp">the root app for this request.</param>
        /// <param name="body">The body of the request.</param>
        /// <returns>The number of patches evaluated, or -1 if versioncheck is enabled and patches were queued.</returns>
        public int EvaluatePatches(Session session, IntPtr patchArrayPtr, int patchArraySize) {
            int used = 0;
            int patchCount = 0;
            int patchStart = -1;
            int rejectedPatches = 0;
            JsonPatchOperation patchOp = JsonPatchOperation.Undefined;
            int valueSize;
            IntPtr valuePtr;
            JsonPatchMember member;
            JsonPointer pointer;
            JsonReader reader;
            byte[] tmpBuf = new byte[512];
            int usedTmpBufSize;
            long clientVersion = -1;

            bool versionCheckEnabled = session.CheckOption(SessionOptions.PatchVersioning);
            session.ClientServerVersion = session.ServerVersion;

            try {
                unsafe {
                    reader = new JsonReader(patchArrayPtr, patchArraySize);

                    while (reader.GotoNextObject()) {
                        member = JsonPatchMember.Invalid;
                        pointer = null;
                        valuePtr = IntPtr.Zero;
                        valueSize = -1;

                        patchStart = reader.Used - 1;
                        while (reader.GotoProperty()) {
                            reader.ReadRaw(tmpBuf, out usedTmpBufSize);
                            GetPatchMember(tmpBuf, 1, out member);
                            reader.GotoValue();

                            switch (member) {
                                case JsonPatchMember.Op:
                                    reader.ReadRaw(tmpBuf, out usedTmpBufSize);
                                    patchOp = JsonPatch.GetPatchOperation(tmpBuf, 0, usedTmpBufSize);
                                    break;
                                case JsonPatchMember.Path:
                                    reader.ReadRaw(tmpBuf, out usedTmpBufSize);
                                    byte[] ptr = new byte[usedTmpBufSize - 2];
                                    Buffer.BlockCopy(tmpBuf, 1, ptr, 0, usedTmpBufSize - 2);
                                    pointer = new JsonPointer(ptr);
                                    break;
                                case JsonPatchMember.Value:
                                    valuePtr = reader.CurrentPtr;
                                    valueSize = reader.SkipValue();
                                    break;
                                default:
                                    ThrowPatchException(patchStart, patchArrayPtr, patchArraySize, "Unknown property in patch.");
                                    break;
                            }
                        }

                        if (patchOp == JsonPatchOperation.Undefined)
                            ThrowPatchException(patchStart, patchArrayPtr, patchArraySize, "Unsupported patch operation in patch.");

                        if (pointer == null)
                            ThrowPatchException(patchStart, patchArrayPtr, patchArraySize, "No path found in patch.");

                        if ((patchOp != JsonPatchOperation.Remove) && (valuePtr == IntPtr.Zero))
                            ThrowPatchException(patchStart, patchArrayPtr, patchArraySize, "No value found in patch.");

                        used += reader.Used;
                        patchCount++;

                        if (versionCheckEnabled && patchCount <= 2) {
                            if (patchCount == 1) {
                                if ((patchOp != JsonPatchOperation.Replace) || !VerifyPatchPath(pointer, clientVersionPath)) {
                                    // First patch need to be replace for client version.
                                    ThrowPatchException(patchStart, valuePtr, valueSize, "Second patch when versioncheck is enabled have to be replace for client version.");
                                }
                                clientVersion = GetLongValue(valuePtr, valueSize, JsonPatch.ClientVersionPropertyName);
                                if (clientVersion != (session.ClientVersion + 1)) {
                                    if (clientVersion <= session.ClientVersion) {
                                        ThrowPatchException(patchStart, patchArrayPtr, patchArraySize, "Local version of client and clientversion in patch mismatched.");
                                    } else {
                                        byte[] tmp = new byte[patchArraySize];
                                        Marshal.Copy(patchArrayPtr, tmp, 0, patchArraySize);
                                        session.EnqueuePatch(tmp, (int)(clientVersion - session.ClientVersion - 2));

                                        patchCount = -1;
                                        break;
                                    }
                                }
                                session.ClientVersion = clientVersion;
                            } else {
                                // Server: Second patch -> determine if transformations are needed and what version.
                                if ((patchOp != JsonPatchOperation.Test) || !VerifyPatchPath(pointer, serverVersionPath)) {
                                    ThrowPatchException(patchStart, valuePtr, valueSize, "First patch when versioncheck is enabled have to be test for server version.");
                                }

                                session.ClientServerVersion = GetLongValue(valuePtr, valueSize, ServerVersionPropertyName);
                                session.CleanupOldVersionLogs();
                            }
                        } else {
                            if (patchHandler != null) {
                                try {
                                    patchHandler(session, patchOp, pointer, valuePtr, valueSize);
                                } catch (JsonPatchException) {
                                    if (session.CheckOption(SessionOptions.StrictPatchRejection))
                                        throw;
                                    rejectedPatches++;
                                } catch (FormatException) {
                                    if (session.CheckOption(SessionOptions.StrictPatchRejection))
                                        throw;
                                    rejectedPatches++;
                                }
                            }
                        }
                    }
                }

                if (patchCount != -1) {
                    byte[] enqueuedPatch = session.GetNextEnqueuedPatch();
                    if (enqueuedPatch != null) {
                        patchCount += EvaluatePatches(session, enqueuedPatch);
                    }
                }
            } catch (JsonPatchException jpex) {
                if (patchStart != -1 && string.IsNullOrEmpty(jpex.Patch))
                    jpex.Patch = GetPatchAsString(patchStart, patchArrayPtr, patchArraySize);
                throw;
            }

            if (rejectedPatches > 0) {
                // TODO:
                // Callback for rejected patches.
            }

            return patchCount;
        }

        private bool VerifyPatchPath(JsonPointer pointer, byte[] path) {
            byte[] ptrArr = pointer.GetRawBuffer();

            if (ptrArr.Length != path.Length)
                return false;

            for (int i = 0; i < ptrArr.Length; i++) {
                if (ptrArr[i] != path[i])
                    return false;
            }

            return true;
        }

        private long GetLongValue(IntPtr ptr, int size, string propertyName) {
            long value;
            int valueSize;

            if (!JsonHelper.ParseInt(ptr, size, out value, out valueSize))
                JsonHelper.ThrowWrongValueTypeException(null, propertyName, "Int64", ValueAsString(ptr, size));
            return value;
        }

        private static string ValueAsString(IntPtr valuePtr, int valueSize) {
            string value;
            int size;
            JsonHelper.ParseString(valuePtr, valueSize, out value, out size);

            unsafe {
                byte* pval = (byte*)valuePtr;
                if (*pval == (byte)'"')
                    value = '"' + value + '"';
            }

            return value;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="patchStart"></param>
        /// <param name="patchEnd"></param>
        /// <param name="data"></param>
        /// <param name="msg"></param>
        private void ThrowPatchException(int patchStart, IntPtr data, int dataSize, string msg) {
            throw new JsonPatchException(msg, GetPatchAsString(patchStart, data, dataSize));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="patchStart"></param>
        /// <param name="data"></param>
        /// <param name="dataSize"></param>
        /// <returns></returns>
        private string GetPatchAsString(int patchStart, IntPtr data, int dataSize) {
            if (patchStart == -1)
                return "";

            byte[] patchArr = new byte[GetPatchLength(patchStart, data, dataSize)];

            unsafe {
                byte* pdata = (byte*)data;
                pdata += patchStart;
                Marshal.Copy((IntPtr)pdata, patchArr, 0, patchArr.Length);
            }
            return Encoding.UTF8.GetString(patchArr);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="patchStart"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private int GetPatchLength(int patchStart, IntPtr data, int dataSize) {
            int bracketCount = 1;
            int patchEnd = patchStart + 1;

            unsafe {
                byte* pdata = (byte*)data;

                while (patchEnd < dataSize) {
                    if (pdata[patchEnd] == '}') {
                        bracketCount--;
                        if (bracketCount == 0) {
                            patchEnd++;
                            break;
                        }
                    } else if (pdata[patchEnd] == '{')
                        bracketCount++;

                    patchEnd++;
                }
            }
            return (patchEnd - patchStart);
        }

        /// <summary>
        /// Skips all bytes until the name of the current member starts.
        /// </summary>
        /// <param name="contentArr"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        private int GotoMember(byte[] contentArr, int offset, bool first) {
            if (!first) {
                while (offset < contentArr.Length && contentArr[offset] != ',')
                    offset++;
                offset++;
            }
            while (offset < contentArr.Length && contentArr[offset] == ' ')
                offset++;
            if (offset < contentArr.Length && contentArr[offset] == (byte)'"')
                offset++;

            if (offset >= contentArr.Length)
                offset = -1;
            return offset;
        }

        /// <summary>
        /// Skips all bytes until the value of the current member starts.
        /// </summary>
        /// <param name="contentArr"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        private static int GotoValue(byte[] contentArr, int offset) {
            while (offset < contentArr.Length && contentArr[offset] != ':')
                offset++;
            offset++;
            while (offset < contentArr.Length && contentArr[offset] == ' ')
                offset++;
            //if (contentArr[offset] == (byte)'"')
            //    offset++;
            if (offset >= contentArr.Length)
                offset = -1;
            return offset;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="contentArr"></param>
        /// <param name="offset"></param>
        /// <param name="member"></param>
        /// <returns></returns>
        private static int GetPatchMember(byte[] contentArr, int offset, out JsonPatchMember member) {
            member = JsonPatchMember.Invalid;
            switch (contentArr[offset]) {
                case (byte)'o':
                    offset++;
                    if (contentArr[offset] == 'p') {
                        member = JsonPatchMember.Op;
                        offset++;
                    }
                    break;
                case (byte)'p':
                    if (contentArr[offset + 1] == 'a'
                        && contentArr[offset + 2] == 't'
                        && contentArr[offset + 3] == 'h') {
                        offset += 4;
                        member = JsonPatchMember.Path;
                    }
                    break;
                case (byte)'v':
                    if (contentArr[offset + 1] == 'a'
                        && contentArr[offset + 2] == 'l'
                        && contentArr[offset + 3] == 'u'
                        && contentArr[offset + 4] == 'e') {
                        offset += 5;
                        member = JsonPatchMember.Value;
                    }
                    break;
            }
            return offset;
        }

        /// <summary>
        /// Gets the patch verb.
        /// </summary>
        /// <param name="contentArr">The content arr.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="patchType">Type of the patch.</param>
        /// <returns>Int32.</returns>
        /// <exception cref="System.Exception">Unsupported json-patch</exception>
        private static JsonPatchOperation GetPatchOperation(Byte[] contentArr, int offset, int valueSize) {
            byte current = contentArr[offset];
            var op = JsonPatchOperation.Undefined;

            if (current == '"') {
                offset++;
                current = contentArr[offset];
            }

            switch (current){
                case (byte)'a':
                    if (IsPatchOperation(_patchOpUtf8Arr[(int)JsonPatchOperation.Add], contentArr, offset, valueSize))
                        op = JsonPatchOperation.Add;
                    break;
                case (byte)'t':
                    if (IsPatchOperation(_patchOpUtf8Arr[(int)JsonPatchOperation.Test], contentArr, offset, valueSize))
                        op = JsonPatchOperation.Test;
                    break;
                case (byte)'r':
                    if (valueSize > 2) {
                        if (contentArr[offset + 2] == 'p') {
                            if (IsPatchOperation(_patchOpUtf8Arr[(int)JsonPatchOperation.Replace], contentArr, offset, valueSize))
                                op = JsonPatchOperation.Replace;
                        } else {
                            if (IsPatchOperation(_patchOpUtf8Arr[(int)JsonPatchOperation.Remove], contentArr, offset, valueSize))
                                op = JsonPatchOperation.Remove;
                        }
                    }
                    break;
                default:
                    break;
            }
            
            return op;
        }

        /// <summary>
        /// Determines whether the value in the buffer equals the submitted field.
        /// </summary>
        /// <param name="verbName">The array of bytes for the field</param>
        /// <param name="buffer">The buffer containing the value to check</param>
        /// <param name="offset">The offset in the buffer</param>
        /// <param name="length">The length of the buffer</param>
        private static bool IsPatchOperation(byte[] verbName, byte[] buffer, int offset, int length) {
            int i;

            for (i = 0; i < verbName.Length; i++) {
                if (i == length)
                    return false;
                if (buffer[offset] == verbName[i]) {
                    offset++;
                } else {
                    return false;
                }
            }

            return true;
        }
    }
}
