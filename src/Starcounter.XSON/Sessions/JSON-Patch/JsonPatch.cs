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
        private static byte[] testRemoteVersionPatchStart = Encoding.UTF8.GetBytes(@"{""op"":""test"",""path"":""/");
        private static byte[] replaceLocalVersionPatchStart = Encoding.UTF8.GetBytes(@"{""op"":""replace"",""path"":""/");
        private static byte[] patchStartToOp = Encoding.UTF8.GetBytes(@"{""op"":""");
        private static byte[] patchEndToPath = Encoding.UTF8.GetBytes(@""",""path"":""");
        private static byte[] patchEndToFrom = Encoding.UTF8.GetBytes(@""",""from"":""");
        private static byte[] patchEndToValue = Encoding.UTF8.GetBytes(@""",""value"":");
        
        private static byte[][] patchOpUtf8Arr;
        private static byte[] emptyPatchArr = { (byte)'[', (byte)']' };

        private Action<Json, JsonPatchOperation, JsonPointer, IntPtr, int> patchHandler = DefaultPatchHandler.Handle;

        private enum JsonPatchMember {
            Invalid,
            Op,
            Path,
            Value
        }

        static JsonPatch() {
            patchOpUtf8Arr = new byte[6][];
            patchOpUtf8Arr[(int)JsonPatchOperation.Undefined] = Encoding.UTF8.GetBytes("undefined");
            patchOpUtf8Arr[(int)JsonPatchOperation.Remove] = Encoding.UTF8.GetBytes("remove");
            patchOpUtf8Arr[(int)JsonPatchOperation.Replace] = Encoding.UTF8.GetBytes("replace");
            patchOpUtf8Arr[(int)JsonPatchOperation.Add] = Encoding.UTF8.GetBytes("add");
            patchOpUtf8Arr[(int)JsonPatchOperation.Move] = Encoding.UTF8.GetBytes("move");
            patchOpUtf8Arr[(int)JsonPatchOperation.Test] = Encoding.UTF8.GetBytes("test");    
        }

        public JsonPatch() {
        }

        public void SetPatchHandler(Action<Json, JsonPatchOperation, JsonPointer, IntPtr, int> handler) {
            patchHandler = handler;
        }

        /// <summary>
        /// Generates a JSON-Patch array for all changes logged in the changelog.
        /// </summary>
        /// <param name="flushLog">If true, the change log will be reset</param>
        /// <returns>The JSON-Patch string (see RFC6902)</returns>
        public string Generate(Json json, bool flushLog, bool includeNamespace) {
            byte[] patchArr;
            int size = Generate(json, flushLog, includeNamespace, out patchArr);
            return Encoding.UTF8.GetString(patchArr, 0, size);
        }

        /// <summary>
        /// Generates a JSON-Patch array for all changes logged in the changelog
        /// </summary>
        /// <param name="flushLog">If true, the change log will be reset</param>
        /// <returns>The JSON-Patch string (see RFC6902)</returns>
        public int Generate(Json json, bool flushLog, bool includeNamespace, out byte[] patches) {
            int patchSize;
            ChangeLog changeLog = json.ChangeLog;
            
            if (changeLog == null) {
                throw new Exception("Cannot generate patches on json that has no changelog attached.");
            }

            var session = json.Session;
            if (session != null)
                session.enableNamespaces = true;

            try {
                patchSize = Generate(changeLog, includeNamespace, flushLog, out patches);
            } finally {
                if (session != null)
                    session.enableNamespaces = false;
            }
            
            return patchSize;
        }

        /// <summary>
        /// Creates an array of jsonpatches as a bytearray.
        /// </summary>
        /// <param name="changeLog"></param>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private int Generate(ChangeLog changeLog, bool includeNamespace, bool flushLog, out byte[] patches) {
            byte[] buffer;
            int size;
            Utf8Writer writer;
            Change[] changes;
            bool versioning = (changeLog.Version != null);
            
            changes = changeLog.Generate(flushLog);
            
            size = 2; // [ ]

            if (versioning) {
                // If versioning is enabled two patches are fixed: test clientversion and replace serverversion.
                size += testRemoteVersionPatchStart.Length;
                size += changeLog.Version.RemoteVersionPropertyName.Length;
                size += patchEndToValue.Length;
                size += GetSizeOfIntAsUtf8(changeLog.Version.RemoteVersion);
                size += 2; // +2 for "},"

                size += replaceLocalVersionPatchStart.Length;
                size += changeLog.Version.LocalVersionPropertyName.Length;
                size += patchEndToValue.Length;
                size += GetSizeOfIntAsUtf8(changeLog.Version.LocalVersion);
                size += 2; // +2 for "},"
            }

            int patchSize;
            for (int i = 0; i < changes.Length; i++) {
                patchSize = EstimateSizeOfPatch(changes[i], includeNamespace);
                if (patchSize == -1) { // This change is no longer valid.
                    changes[i] = Change.Invalid;
                    continue;
                }
                    
                size += patchSize;
            }

            size += changes.Length - 1; // Adding one ',' per change over zero.
            if (size < 2) size = 2;

            buffer = new byte[size];
            
            unsafe {
                fixed (byte* pbuf = buffer) {
                    writer = new Utf8Writer(pbuf);
                    writer.Write('[');

                    if (versioning) {
                        writer.Write(replaceLocalVersionPatchStart);
                        writer.Write(changeLog.Version.LocalVersionPropertyName);
                        writer.Write(patchEndToValue);
                        writer.Write(changeLog.Version.LocalVersion);
                        writer.Write('}');
                        writer.Write(',');

                        writer.Write(testRemoteVersionPatchStart);
                        writer.Write(changeLog.Version.RemoteVersionPropertyName);
                        writer.Write(patchEndToValue);
                        writer.Write(changeLog.Version.RemoteVersion);
                        writer.Write('}');

                        if (changes.Length > 0) {
                            writer.Write(',');
                        }
                    }

                    for (int i = 0; i < changes.Length; i++) {
                        var change = changes[i];
                        if (change.ChangeType == Change.INVALID)
                            continue;

                        WritePatch(change, ref writer, includeNamespace);

                        if (change.Property != null) {
                            Json parent = change.Parent;
                            parent.Scope<Json, TValue>((p, t) => {
                                t.Checkpoint(p);
                            }, parent, change.Property);
                        }

                        if ((i + 1) < changes.Length)
                            writer.Write(',');
                    }
                    writer.Write(']');

                    // Setting the actual size, since the first is just estimated.
                    size = writer.Written;
                }
            }

            if (size > buffer.Length) {
                var errMsg = "JsonPatch: Written size is larger than estimated size!";
                errMsg += " (written: " + size + ", estimated: " + buffer.Length + ")\r\n";
                errMsg += "Buffer (w/o data out of bounds): " + System.Text.Encoding.UTF8.GetString(buffer);
                throw new Exception(errMsg);
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
        private bool WritePatch(Change change, ref Utf8Writer writer, bool includeNamespace) {
            int size;
            int writerStart = writer.Written;
//            int pathStart;
//            int pathEnd;
            TypedJsonSerializer serializer;

            writer.Write(patchStartToOp);
            writer.Write(patchOpUtf8Arr[change.ChangeType]);
            writer.Write(patchEndToPath);

//            if (change.Property != null) {
//                pathStart = writer.Written;
                if (!WritePath(ref writer, change, includeNamespace)) {
                    writer.Skip(writer.Written - writerStart);
                    return false;
                }

                if (change.ChangeType == Change.MOVE) {
                    // TODO: 
                    // implement move.
                    throw new NotImplementedException("Move operation is not yet implemented.");

                    //// Copy the contens of the path, changing the index 
                    //pathEnd = writer.Written;
                    //writer.Write(patchEndToFrom);

                    ////TODO: 
                    //// Copy...
                    //unsafe {
                    //    byte* pBuf = writer.Buffer;
                        
                    //}
                }
//            }

            if (change.ChangeType != (int)JsonPatchOperation.Remove) {
                writer.Write(patchEndToValue);

                unsafe {
                    if (change.Property == null) {
                        change.Parent.calledFromStepSibling = change.SuppressNamespace;
                        try {
                            serializer = change.Parent.JsonSerializer;
                            size = serializer.Serialize(change.Parent, (IntPtr)writer.Buffer, int.MaxValue);
                        } finally {
                            change.Parent.calledFromStepSibling = false;
                        }
                    } else if (change.Index != -1) {
                        serializer = change.Item.JsonSerializer;
                        size = serializer.Serialize(change.Item, (IntPtr)writer.Buffer, int.MaxValue);
                    } else {
                        size = change.Parent.JsonSerializer.Serialize(change.Parent, change.Property, (IntPtr)writer.Buffer, int.MaxValue);
                    }
                    writer.Skip(size);
                }
            } else {
                writer.Write('"');
            }

            writer.Write('}');
            return true;
        }

        internal static int EstimateSizeOfPatch(Change change, bool includeNamespace) {
            int size;
            int pathSize;
            TypedJsonSerializer serializer;

            // {"op":"???","path":"???","value":???}
            // size = 7 + op + 10 + path + 10 + value + 1 => 28 + var

            // {"op":"???","path":"???"}
            // size = 7 + op + 10 + path + 2 => 19 + var

            if (change.ChangeType == Change.MOVE) {
                // TODO: 
                // implement move.
                throw new NotImplementedException("Move operation is not yet implemented.");
            }

            size = 19;
            size += patchOpUtf8Arr[change.ChangeType].Length;

            pathSize = EstimateSizeOfPath(change.Parent, includeNamespace, false);
            if (pathSize == -1) {
                // No valid path found.
                return -1;
            }

            size += pathSize;
            if (change.Property != null)
                size += change.Property.TemplateName.Length + 1;
            else
                size--; // Path for root is empty, ""

            if (change.Index != -1)
                size += GetSizeOfIntAsUtf8(change.Index) + 1;

            if (change.ChangeType != (int)JsonPatchOperation.Remove) {
                size += 9;

                if (change.Property == null) {
                    change.Parent.calledFromStepSibling = change.SuppressNamespace;
                    try {
                        serializer = change.Parent.JsonSerializer;
                        size += serializer.EstimateSizeBytes(change.Parent);
                    } finally {
                        change.Parent.calledFromStepSibling = false;
                    }
                } else if (change.Index != -1) {
                    serializer = change.Item.JsonSerializer;
                    size += serializer.EstimateSizeBytes(change.Item);
                } else {
                    size += change.Parent.JsonSerializer.EstimateSizeBytes(change.Parent, change.Property);
                }
            }
            return size;
        }

        private static int EstimateSizeOfPath(Json json, bool includeNamespace, bool calledFromStepSibling) {
            int size;
            int totalSize;
            Json parent;
            Template template;
            Session session;

            // TODO:
            // Evaluate all possible paths and create patches for all valid ones. 

            if (json == null)
                return -1;

            session = json.Session;
            if (session == null) // No session, this can't be the correct path.
                return -1;

            if (json == session.PublicViewModel) // Valid path.
                return 1;

            size = -1;
            totalSize = 0;
            if (!calledFromStepSibling && json.StepSiblings != null) {
                foreach (Json stepSibling in json.StepSiblings) {
                    if (stepSibling == json)
                        continue;
                    size = EstimateSizeOfPath(stepSibling, includeNamespace, true);
                    if (size != -1) 
                        break;
                }
            }

            parent = null;
            if (size == -1) {
                parent = json.Parent;
                size = EstimateSizeOfPath(parent, includeNamespace, false);
            }

            if (size == -1)
                return -1;

            totalSize += size;

            if (parent != null) {
                totalSize++;
                if (parent.IsArray) {
                    if (json._cacheIndexInArr == -1)
                        json.UpdateCachedIndex();
                    totalSize += GetSizeOfIntAsUtf8(json._cacheIndexInArr);
                } else {
                    // We use the cacheIndexInArr to keep track of obj that is set
                    // in the parent as an untyped object since the template here is not
                    // the template in the parent (which we want).
                    if (json._cacheIndexInArr != -1) {
                        template = ((TObject)parent.Template).Properties[json._cacheIndexInArr];
                    } else {
                        template = json.Template;
                    }
                    totalSize += template.TemplateName.Length;
                }
            }

            if (includeNamespace && !calledFromStepSibling && json._wrapInAppName) {
                totalSize += json._appName.Length + 1;
            } 

            return totalSize;
        }

        private bool WritePath(ref Utf8Writer writer, Json json, bool includeNamespace, bool calledFromStepSibling) {
            bool pathWritten;
            Json parent;
            Template template;
            Session session;

            // TODO:
            // Evaluate all possible paths and create patches for all valid ones. 

            if (json == null)
                return false;

            session = json.Session;
            if (session == null)
                return false;

            if (json == session.PublicViewModel) // Valid path.
                return true;

            pathWritten = false;
            if (!calledFromStepSibling && json.StepSiblings != null) {
                foreach (Json stepSibling in json.StepSiblings) {
                    if (stepSibling == json)
                        continue;

                    pathWritten = WritePath(ref writer, stepSibling, includeNamespace, true);
                    if (pathWritten)
                        break;
                }
            }

            parent = null;
            if (!pathWritten) {
                parent = json.Parent;
                pathWritten = WritePath(ref writer, parent, includeNamespace, false);
            }

            if (!pathWritten)
                return false;

            if (parent != null) {
                if (parent.IsArray) {
                    if (json._cacheIndexInArr == -1)
                        json.UpdateCachedIndex();
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
                    writer.Write('/');
                    writer.Write(template.TemplateName);
                }
            }

            if (includeNamespace && !calledFromStepSibling && json._wrapInAppName) {
                writer.Write('/');
                writer.Write(json._appName);
            }
            return true;
        }

        private bool WritePath(ref Utf8Writer writer, Change change, bool includeNamespace) {
            if (WritePath(ref writer, change.Parent, includeNamespace, false)) {
                if (change.Property != null) {
                    writer.Write('/');
                    writer.Write(change.Property.TemplateName);
                }

                if (change.Index != -1) {
                    writer.Write('/');
                    writer.Write(change.Index);
                }
                return true;
            }
            return false;
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
        ///// <param name="root">The root json</param>
        ///// <param name="patchArray">The bytearray containing all patches.</param>
        ///// <returns>The number of patches evaluated.</returns>
        public int Apply(Json root, byte[] patchArray, bool strictPatchRejection = true) {
            int patchCount;

            unsafe {
                fixed (byte* pBody = patchArray) {
                    patchCount = Apply(root, (IntPtr)pBody, patchArray.Length, strictPatchRejection);
                }
            }
            return patchCount;
        }


        public int Apply(Json root, string patch, bool strictPatchRejection = true) {

            byte[] patchArray = System.Text.Encoding.UTF8.GetBytes(patch);
            return this.Apply(root, patchArray, strictPatchRejection);
        }
        /// <summary>
        /// Evaluates the patches and calls the appropriate inputhandler.
        /// </summary>
        /// <param name="root">the root app for this request.</param>
        /// <param name="patchArrayPtr">The pointer to the content for the patches.</param>
        /// <param name="patchArraySize">The size of the content.</param>
        /// <returns>The number of patches evaluated, or -1 if versioncheck is enabled and patches were queued.</returns>
        public int Apply(Json root, IntPtr patchArrayPtr, int patchArraySize, bool strictPatchRejection = true) {
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
            long remoteVersion = -1;
            ChangeLog changeLog = null;
            ViewModelVersion version = null;

            if (root != null) {
                changeLog = root.ChangeLog;
                if (changeLog != null) {
                    version = changeLog.Version;
                    if (version != null) {
                        version.RemoteLocalVersion = version.LocalVersion;
                    }
                }
            }
            
            member = JsonPatchMember.Invalid;
            valuePtr = IntPtr.Zero;
            valueSize = 0;
            pointer = null;

            try {
                unsafe {
                    reader = new JsonReader(patchArrayPtr, patchArraySize);
                    JsonToken token = JsonToken.Null;
                    token = reader.ReadNext();

                    while (token != JsonToken.End) {
                        switch (token) {
                            case JsonToken.StartArray:
                                if (member == JsonPatchMember.Value) {
                                    // In case the last read property is the value, we assume that
                                    // this array is part of the value of the patch, and not
                                    // the start of a new batch of patches.
                                    token = JsonToken.Value;
                                    continue;
                                }
                                reader.Skip(1);
                                break;
                            case JsonToken.EndArray:
                                reader.Skip(1);
                                break;
                            case JsonToken.StartObject:
                                if (member == JsonPatchMember.Value) {
                                    // In case the last read property is the value, we assume that
                                    // this object is part of the value of the patch, and not
                                    // the start of a new patch.
                                    token = JsonToken.Value;
                                    continue;
                                }

                                member = JsonPatchMember.Invalid;
                                pointer = null;
                                valuePtr = IntPtr.Zero;
                                valueSize = -1;
                                patchStart = reader.Position;
                                reader.Skip(1);
                                break;
                            case JsonToken.PropertyName:
                                reader.ReadRaw(tmpBuf, out usedTmpBufSize);
                                GetPatchMember(tmpBuf, 1, out member);
                                break;
                            case JsonToken.Value:
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
                                        valueSize = reader.Position;
                                        reader.SkipCurrent();
                                        valueSize = reader.Position - valueSize;
                                        break;
                                    default:
                                        ThrowPatchException(patchStart, patchArrayPtr, patchArraySize, "Unknown property in patch.");
                                        break;
                                }
                                break;
                            case JsonToken.EndObject:
                                reader.Skip(1);
                                // Handle complete patch
                                if (patchOp == JsonPatchOperation.Undefined)
                                    ThrowPatchException(patchStart, patchArrayPtr, patchArraySize, "Unsupported patch operation in patch.");

                                if (pointer == null)
                                    ThrowPatchException(patchStart, patchArrayPtr, patchArraySize, "No path found in patch.");

                                if ((patchOp != JsonPatchOperation.Remove) && (valuePtr == IntPtr.Zero))
                                    ThrowPatchException(patchStart, patchArrayPtr, patchArraySize, "No value found in patch.");

                                patchCount++;
                                if ((version != null) && patchCount <= 2) {
                                    if (patchCount == 1) {
                                        if ((patchOp != JsonPatchOperation.Replace) || !VerifyVersioningPatchPath(pointer, version.RemoteVersionPropertyName)) {
                                            // First patch need to be replace for client version.
                                            ThrowPatchException(patchStart, valuePtr, valueSize, "First patch when versioncheck is enabled have to be replace for remote version.");
                                        }
                                        remoteVersion = GetLongValue(valuePtr, valueSize, version.RemoteVersionPropertyName);
                                        if (remoteVersion != (version.RemoteVersion + 1)) {
                                            if (remoteVersion <= version.RemoteVersion) {
                                                ThrowPatchException(patchStart, patchArrayPtr, patchArraySize, "Remote version mismatched.");
                                            } else {
                                                byte[] tmp = new byte[patchArraySize];
                                                Marshal.Copy(patchArrayPtr, tmp, 0, patchArraySize);
                                                version.EnqueuePatch(tmp, (int)(remoteVersion - version.RemoteVersion - 2));
                                                patchCount = -1;
                                                token = JsonToken.End;
                                                break;
                                            }
                                        }
                                        version.RemoteVersion = remoteVersion;
                                    } else {
                                        // Second patch -> determine if transformations are needed and what version.
                                        if ((patchOp != JsonPatchOperation.Test) || !VerifyVersioningPatchPath(pointer, version.LocalVersionPropertyName)) {
                                            ThrowPatchException(patchStart, valuePtr, valueSize, "Second patch when versioncheck is enabled have to be test of local version.");
                                        }

                                        version.RemoteLocalVersion = GetLongValue(valuePtr, valueSize, version.LocalVersionPropertyName);
                                        changeLog.CleanupOldVersionLogs();
                                    }
                                } else {
                                    if (patchHandler != null) {
                                        try {
                                            patchHandler(root, patchOp, pointer, valuePtr, valueSize);
                                        } catch (JsonPatchException jpe) {
                                            if (strictPatchRejection || jpe.Severity > 0)
                                                throw;
                                            rejectedPatches++;
                                        } catch (FormatException) {
                                            if (strictPatchRejection)
                                                throw;
                                            rejectedPatches++;
                                        }
                                    }
                                }
                                member = JsonPatchMember.Invalid;
                                break;
                        }
                        token = reader.ReadNext();
                    }

                    if (version != null && patchCount != -1) {
                        byte[] enqueuedPatch = version.GetNextEnqueuedPatch();
                        if (enqueuedPatch != null) {
                            patchCount += Apply(root, enqueuedPatch, strictPatchRejection);
                        }
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

        private bool VerifyVersioningPatchPath(JsonPointer pointer, string versionPropertyName) {
            byte[] ptrArr = pointer.GetRawBuffer();

            if (ptrArr.Length != (versionPropertyName.Length + 1))
                return false;

            for (int i = 1; i < ptrArr.Length; i++) {
                if (ptrArr[i] != (byte)versionPropertyName[i - 1])
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
            throw new JsonPatchException(1, msg, GetPatchAsString(patchStart, data, dataSize));
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
                    if (IsPatchOperation(patchOpUtf8Arr[(int)JsonPatchOperation.Add], contentArr, offset, valueSize))
                        op = JsonPatchOperation.Add;
                    break;
                case (byte)'t':
                    if (IsPatchOperation(patchOpUtf8Arr[(int)JsonPatchOperation.Test], contentArr, offset, valueSize))
                        op = JsonPatchOperation.Test;
                    break;
                case (byte)'r':
                    if (valueSize > 2) {
                        if (contentArr[offset + 2] == 'p') {
                            if (IsPatchOperation(patchOpUtf8Arr[(int)JsonPatchOperation.Replace], contentArr, offset, valueSize))
                                op = JsonPatchOperation.Replace;
                        } else {
                            if (IsPatchOperation(patchOpUtf8Arr[(int)JsonPatchOperation.Remove], contentArr, offset, valueSize))
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
