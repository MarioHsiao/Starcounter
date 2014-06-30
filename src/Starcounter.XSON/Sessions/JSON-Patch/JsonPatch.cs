// ***********************************************************************
// <copyright file="JsonPatch.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Starcounter.Advanced.XSON;
using Starcounter.Internal;
using Starcounter.Internal.XSON;
using Starcounter.Templates;

namespace Starcounter.XSON.JsonPatch {
    /// <summary>
    /// Class for evaluating, handling and creating json-patch to and from typed json objects 
    /// and logged changes done in a typed json object during a request.
    /// 
    /// The json-patch is implemented according to http://tools.ietf.org/html/draft-ietf-appsawg-json-patch-10
    /// </summary>
    public class JsonPatch {
        private const Int32 UNDEFINED = 0;
        private const Int32 REMOVE = 1;
        private const Int32 REPLACE = 2;
        private const Int32 ADD = 3;

        private static byte[][] _patchOpUtf8Arr;
        private static byte[] _emptyPatchArr = { (byte)'[', (byte)']' };

        private enum JsonPatchMember {
            Invalid,
            Op,
            Path,
            Value
        }

        static JsonPatch() {
            _patchOpUtf8Arr = new byte[4][];
            _patchOpUtf8Arr[UNDEFINED] = Encoding.UTF8.GetBytes("undefined");
            _patchOpUtf8Arr[REMOVE] = Encoding.UTF8.GetBytes("remove");
            _patchOpUtf8Arr[REPLACE] = Encoding.UTF8.GetBytes("replace");
            _patchOpUtf8Arr[ADD] = Encoding.UTF8.GetBytes("add");    
        }

        /// <summary>
        /// Generates a JSON-Patch array for all changes made to the session data
        /// </summary>
        /// <param name="flushLog">If true, the change log will be reset</param>
        /// <returns>The JSON-Patch string (see RFC6902)</returns>
        public static string CreateJsonPatch(Session session, bool flushLog) {
            byte[] patchArr;
            int size = CreateJsonPatchBytes(session, flushLog, out patchArr);
            return Encoding.UTF8.GetString(patchArr, 0, size);
        }

        /// <summary>
        /// Generates a JSON-Patch array for all changes made to the session data
        /// </summary>
        /// <param name="flushLog">If true, the change log will be reset</param>
        /// <returns>The JSON-Patch string (see RFC6902)</returns>
        public static int CreateJsonPatchBytes(Session session, bool flushLog, out byte[] patches) {
            int patchSize;
            List<Change> changes;

            session.GenerateChangeLog();
            changes = session.GetChanges();

            if (changes.Count == 0) {
                patches = _emptyPatchArr;
                return _emptyPatchArr.Length;
            }

            patchSize = CreatePatches(changes, out patches);
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
        internal static int CreatePatches(List<Change> changes, out byte[] patches) {
            byte[] buffer;
            int size;
            int[] pathSizes;
            Utf8Writer writer;

            // TODO:
            // We dont want to create a new array here...
            pathSizes = new int[changes.Count];

            size = 2;
            size += changes.Count;
            for (int i = 0; i < changes.Count; i++) 
                size += CalculateSize(changes[i], out pathSizes[i]);
            buffer = new byte[size];

            unsafe {
                fixed (byte* pbuf = buffer) {
                    writer = new Utf8Writer(pbuf);
                    writer.Write('[');

                    for (int i = 0; i < changes.Count; i++) {
                        var change = changes[i];
                        WritePatch(change, ref writer, pathSizes[i]);

                        if (change.Property != null) {
                            change.Obj.AddInScope(() => {
                                change.Property.Checkpoint(change.Obj);
                            });
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
        private static void WritePatch(Change change, ref Utf8Writer writer, int pathSize) {
			Json childJson = null;

            // TODO:
            // dont write static strings as strings. convert them once and copy arrays.
            writer.Write("{\"op\":\"");
            writer.Write(_patchOpUtf8Arr[change.ChangeType]);
            writer.Write("\",\"path\":\"");

            if (change.Property != null) {
                WritePath(ref writer, change, pathSize);
            } else {
                writer.Write('/');
                childJson = change.Obj;
            }
            writer.Write('"');

            if (change.ChangeType != REMOVE) {
                writer.Write(",\"value\":");
                if (childJson == null && change.Property is TContainer) {
                    childJson = (Json)change.Property.GetUnboundValueAsObject(change.Obj);
                    if (change.Index != -1)
                        childJson = (Json)childJson._GetAt(change.Index);

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
                    string value = 
                        change.Obj.AddAndReturnInScope<Change, string>(
                            (Change c) => { return c.Property.ValueToJsonString(c.Obj); },
                            change
                        );
                    writer.Write(value);
                }
            }
            writer.Write('}');
        }

        private static int EstimatePropertyValueSizeInBytes(TValue property, Json parent, int index) {
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
                    childJson = (Json)childJson._GetAt(index);
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

            pathSize = CalculateSizeOfPath(change.Obj, false);
            if (change.Property != null)
                pathSize += change.Property.TemplateName.Length;

            if (change.Index != -1)
                pathSize += GetSizeOfIntAsUtf8(change.Index) + 1;
            size += pathSize;

            if (change.ChangeType != REMOVE) {
                size += 9;
                size += change.Obj.AddAndReturnInScope<Change, int>(
                            (Change c) => {
                                return EstimatePropertyValueSizeInBytes(c.Property, c.Obj, c.Index);
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

                if (!fromStepParent && json._stepSiblings != null && json._stepSiblings.Count > 0) {
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

        private static void WritePath(ref Utf8Writer writer, Change change, int pathSize) {
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
            WritePath_2(ref writer, change.Obj, sizeToWrite, false);
            if (positionAfter != writer.Written) {
                writer.Skip(positionAfter - writer.Written);
            }
        }

        private static void WritePath_2(ref Utf8Writer writer, Json json, int prevSize, bool fromStepParent) {
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
                    if (!fromStepParent && json._stepSiblings != null && json._stepSiblings.Count > 0) {
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

        // TODO:
        // must be a much better way of doing this
        private static int GetSizeOfIntAsUtf8(int value) {
            if (value < 10)
                return 1;
            if (value < 100)
                return 2;
            if (value < 1000)
                return 3;
            if (value < 10000)
                return 4;
            if (value < 100000)
                return 5;
            if (value < 1000000)
                return 6;

            return -1;
        }

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="session">The session the root json is retrieved from.</param>
        ///// <param name="patchArray">The bytearray containing all patches.</param>
        ///// <returns>The number of patches evaluated.</returns>
        public static int EvaluatePatches(Session session, byte[] patchArray) {
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
        /// <returns>The number of patches evaluated.</returns>
        public static int EvaluatePatches(Session session, IntPtr patchArrayPtr, int patchArraySize) {
            int used = 0;
            int patchCount = 0;
            int patchStart = -1;
            int patchVerb = -1;
            int valueSize;
            IntPtr valuePtr;
            JsonPatchMember member;
            JsonPointer pointer;
            JsonReader reader;
            byte[] tmpBuf = new byte[512];
            int usedTmpBufSize;

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
                                    JsonPatch.GetPatchVerb(tmpBuf, 1, out patchVerb);
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

                        if (patchVerb != JsonPatch.REPLACE)
                            ThrowPatchException(patchStart, patchArrayPtr, patchArraySize, "Unsupported patch operation in patch.");

                        if (pointer == null)
                            ThrowPatchException(patchStart, patchArrayPtr, patchArraySize, "No path found in patch.");

                        if (valuePtr == IntPtr.Zero)
                            ThrowPatchException(patchStart, patchArrayPtr, patchArraySize, "No value found in patch.");

                        used += reader.Used;
                        patchCount++;
                        HandleParsedPatch(session, pointer, valuePtr, valueSize);
                    }
                }
            } catch (JsonPatchException jpex) {
                if (patchStart != -1 && string.IsNullOrEmpty(jpex.Patch))
                    jpex.Patch = GetPatchAsString(patchStart, patchArrayPtr, patchArraySize);
                throw;
            }

            return patchCount;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="patchStart"></param>
        /// <param name="patchEnd"></param>
        /// <param name="data"></param>
        /// <param name="msg"></param>
        private static void ThrowPatchException(int patchStart, IntPtr data, int dataSize, string msg) {
            throw new JsonPatchException(msg, GetPatchAsString(patchStart, data, dataSize));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="patchStart"></param>
        /// <param name="data"></param>
        /// <param name="dataSize"></param>
        /// <returns></returns>
        private static string GetPatchAsString(int patchStart, IntPtr data, int dataSize) {
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
        private static int GetPatchLength(int patchStart, IntPtr data, int dataSize) {
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
        private static int GotoMember(byte[] contentArr, int offset, bool first) {
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
        private static Int32 GetPatchVerb(Byte[] contentArr, Int32 offset, out Int32 patchType) {
            byte[] replaceArr = _patchOpUtf8Arr[REPLACE];

            if (contentArr[offset] == '"')
                offset++;

            if (IsPatchVerb(replaceArr, contentArr, offset, contentArr.Length)) {
                patchType = REPLACE;
                offset += replaceArr.Length;
            } else
                patchType = UNDEFINED;
            return offset;
        }

        /// <summary>
        /// Determines whether the value in the buffer equals the submitted field.
        /// </summary>
        /// <param name="verbName">The array of bytes for the field</param>
        /// <param name="buffer">The buffer containing the value to check</param>
        /// <param name="offset">The offset in the buffer</param>
        /// <param name="length">The length of the buffer</param>
        private static bool IsPatchVerb(byte[] verbName, byte[] buffer, int offset, int length) {
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

        private static void HandleParsedPatch(Session session, JsonPointer pointer, IntPtr valuePtr, int valueSize) {
            Debug.WriteLine("Handling patch for: " + pointer.ToString());

            if (session == null) return;

            JsonProperty aat = pointer.Evaluate(session.GetFirstData());

            if (!aat.Property.Editable) {
                throw new JsonPatchException(
                    "Property '" + aat.Property.PropertyName + "' is readonly.",
                    null
                );
            }

            aat.Json.AddInScope(() => {
                if (aat.Property is TBool) {
                    ParseAndProcess((TBool)aat.Property, aat.Json, valuePtr, valueSize);
                } else if (aat.Property is TDecimal) {
                    ParseAndProcess((TDecimal)aat.Property, aat.Json, valuePtr, valueSize);
                } else if (aat.Property is TDouble) {
                    ParseAndProcess((TDouble)aat.Property, aat.Json, valuePtr, valueSize);
                } else if (aat.Property is TLong) {
                    ParseAndProcess((TLong)aat.Property, aat.Json, valuePtr, valueSize);
                } else if (aat.Property is TString) {
                    ParseAndProcess((TString)aat.Property, aat.Json, valuePtr, valueSize);
                } else if (aat.Property is TTrigger) {
                    ParseAndProcess((TTrigger)aat.Property, aat.Json);
                } else {
                    throw new JsonPatchException(
                        "Property " + aat.Property.TemplateName + " is invalid for userinput",
                        null
                    );
                }
            });
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

        private static void ParseAndProcess(TBool property, Json parent, IntPtr valuePtr, int valueSize) {
            bool value;
            int size;

            if (!JsonHelper.ParseBoolean(valuePtr, valueSize, out value, out size)) 
                JsonHelper.ThrowWrongValueTypeException(null, property.PropertyName, property.JsonType, ValueAsString(valuePtr, valueSize));
            property.ProcessInput(parent, value);
        }

        private static void ParseAndProcess(TDecimal property, Json parent, IntPtr valuePtr, int valueSize) {
            decimal value;
            int size;

            if (!JsonHelper.ParseDecimal(valuePtr, valueSize, out value, out size))
                JsonHelper.ThrowWrongValueTypeException(null, property.PropertyName, property.JsonType, ValueAsString(valuePtr, valueSize));
            property.ProcessInput(parent, value);
        }

        private static void ParseAndProcess(TDouble property, Json parent, IntPtr valuePtr, int valueSize) {
            double value;
            int size;

            if (!JsonHelper.ParseDouble(valuePtr, valueSize, out value, out size))
                JsonHelper.ThrowWrongValueTypeException(null, property.PropertyName, property.JsonType, ValueAsString(valuePtr, valueSize));
            property.ProcessInput(parent, value);
        }

        private static void ParseAndProcess(TLong property, Json parent, IntPtr valuePtr, int valueSize) {
            long value;
            int realValueSize;

            if (!JsonHelper.ParseInt(valuePtr, valueSize, out value, out realValueSize))
                JsonHelper.ThrowWrongValueTypeException(null, property.PropertyName, property.JsonType, ValueAsString(valuePtr, valueSize));
            property.ProcessInput(parent, value);
        }

        private static void ParseAndProcess(TString property, Json parent, IntPtr valuePtr, int valueSize) {
            string value;
            int size;

            if (!JsonHelper.ParseString(valuePtr, valueSize, out value, out size))
                JsonHelper.ThrowWrongValueTypeException(null, property.PropertyName, property.JsonType, null);
            property.ProcessInput(parent, value);
        }

        private static void ParseAndProcess(TTrigger property, Json parent) {
            property.ProcessInput(parent);
        }
    }
}
