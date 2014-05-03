using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Starcounter.Advanced.XSON;
using Starcounter.Templates;

namespace Starcounter.Internal.JsonPatch {
    partial class JsonPatch {

        ///// <summary>
        ///// 
        ///// </summary>
        ///// <param name="rootApp">the root app for this request.</param>
        ///// <param name="body">The body of the request.</param>
        ///// <returns>The number of patches evaluated.</returns>
        internal static int EvaluatePatches(Json rootApp, byte[] body) {
            int patchCount;

            unsafe {
                fixed (byte* pBody = body) {
                    patchCount = EvaluatePatches(rootApp, (IntPtr)pBody, body.Length);        
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
        internal static int EvaluatePatches(Json root, IntPtr body, int bodySize) {
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
                    reader = new JsonReader(body, bodySize);

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

                            switch (member){
                                case JsonPatchMember.Op:
                                    reader.ReadRaw(tmpBuf, out usedTmpBufSize);
                                    JsonPatch.GetPatchVerb(tmpBuf, 1, out patchVerb);
                                    break;
                                case JsonPatchMember.Path:
                                    reader.ReadRaw(tmpBuf, out usedTmpBufSize);
                                    byte[] ptr = new byte[usedTmpBufSize-2];
                                    Buffer.BlockCopy(tmpBuf, 1, ptr, 0, usedTmpBufSize - 2);
                                    pointer = new JsonPointer(ptr);
                                    break;
                                case JsonPatchMember.Value:
                                    valuePtr = reader.CurrentPtr;
                                    valueSize = reader.SkipValue();
                                    break;
                                default:
                                    ThrowPatchException(patchStart, body, bodySize, "Unknown property in patch.");
                                    break;
                            }
                        }

                        if (patchVerb != JsonPatch.REPLACE)
                            ThrowPatchException(patchStart, body, bodySize, "Unsupported patch operation in patch.");

                        if (pointer == null)
                            ThrowPatchException(patchStart, body, bodySize, "No path found in patch.");

                        if (valuePtr == IntPtr.Zero)
                            ThrowPatchException(patchStart, body, bodySize, "No value found in patch.");

                        used += reader.Used;
                        patchCount++;
                        HandleParsedPatch(root, pointer, valuePtr, valueSize);
                    }
                }
            } catch (JsonPatchException jpex) {
                if (patchStart != -1 && string.IsNullOrEmpty(jpex.Patch))
                    jpex.Patch = GetPatchAsString(patchStart, body, bodySize);
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
            if (contentArr[offset] == '"')
                offset++;

            if (IsPatchVerb(_replacePatchArr, contentArr, offset, contentArr.Length)) {
                patchType = REPLACE;
                offset += _replacePatchArr.Length;
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

        private static void HandleParsedPatch(Json rootApp, JsonPointer pointer, IntPtr valuePtr, int valueSize) {
            Debug.WriteLine("Handling patch for: " + pointer.ToString());

            if (rootApp != null) {
                AppAndTemplate aat = JsonPatch.Evaluate(rootApp, pointer);

                if (!aat.Template.Editable) {
                    throw new JsonPatchException(
                        "Property '" + aat.Template.PropertyName + "' is readonly.",
                        null
                    );
                }

                aat.App.ExecuteInScope(() => {
                    if (aat.Template is TBool){
                        ParseAndProcess((TBool)aat.Template, aat.App, valuePtr, valueSize);
                    } else if (aat.Template is TDecimal) {
                        ParseAndProcess((TDecimal)aat.Template, aat.App, valuePtr, valueSize);
                    } else if (aat.Template is TDouble) {
                        ParseAndProcess((TDouble)aat.Template, aat.App, valuePtr, valueSize);
                    } else if (aat.Template is TLong) {
                        ParseAndProcess((TLong)aat.Template, aat.App, valuePtr, valueSize);
                    } else if (aat.Template is TString) {
                        ParseAndProcess((TString)aat.Template, aat.App, valuePtr, valueSize);
                    } else if (aat.Template is TTrigger) {
                        ParseAndProcess((TTrigger)aat.Template, aat.App);
                    } else {
                        throw new JsonPatchException(
                            "Property " + aat.Template.TemplateName + " is invalid for userinput", 
                            null
                        );
                    }
                });
            }
        }

        private static void ParseAndProcess(TBool property, Json parent, IntPtr valuePtr, int valueSize) {
            bool value;
            int size;

            if (!JsonHelper.ParseBoolean(valuePtr, valueSize, out value, out size))
                JsonHelper.ThrowWrongValueTypeException(null, property.PropertyName, property.JsonType, null);
            property.ProcessInput(parent, value);
        }

        private static void ParseAndProcess(TDecimal property, Json parent, IntPtr valuePtr, int valueSize) {
            decimal value;
            int size;

            if (!JsonHelper.ParseDecimal(valuePtr, valueSize, out value, out size))
                JsonHelper.ThrowWrongValueTypeException(null, property.PropertyName, property.JsonType, null);
            property.ProcessInput(parent, value);
        }

        private static void ParseAndProcess(TDouble property, Json parent, IntPtr valuePtr, int valueSize) {
            double value;
            int size;

            if (!JsonHelper.ParseDouble(valuePtr, valueSize, out value, out size))
                JsonHelper.ThrowWrongValueTypeException(null, property.PropertyName, property.JsonType, null);
            property.ProcessInput(parent, value);
        }

        private static void ParseAndProcess(TLong property, Json parent, IntPtr valuePtr, int valueSize) {
            long value;
            
            if (!Utf8Helper.IntFastParseFromAscii(valuePtr, valueSize, out value))
                JsonHelper.ThrowWrongValueTypeException(null, property.PropertyName, property.JsonType, null);
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

        /// <summary>
        /// Evaluates the string containing the jsonpointer and retrieves the property it 
        /// points to and the correct jsoninstance for the template.
        /// </summary>
        /// <param name="mainApp">The root app.</param>
        /// <param name="jsonPtr">The string containing the path to the template.</param>
        /// <returns></returns>
        internal static AppAndTemplate Evaluate(Json mainApp, String jsonPtr) {
            return Evaluate(mainApp, new JsonPointer(jsonPtr));
        }

        /// <summary>
        /// Evaluates the jsonpointer and retrieves the property it points to 
        /// and the correct jsoninstance for the template.
        /// </summary>
        /// <param name="mainApp">The root app.</param>
        /// <param name="ptr">The pointer containing the path to the template.</param>
        /// <returns></returns>
        internal static AppAndTemplate Evaluate(Json mainApp, JsonPointer ptr) {
            Boolean currentIsTApp;
            Boolean nextTokenShouldBeIndex;
            Int32 index;
            Object current = null;
            
            nextTokenShouldBeIndex = false;
            currentIsTApp = false;
            while (ptr.MoveNext()) {

                // TODO: 
                // Check if this can be improved. Searching for transaction and execute every
                // step in a new action is not the most efficient way.
                mainApp.ExecuteInScope(() => {
                    if (nextTokenShouldBeIndex) {
                        // Previous object was a Set. This token should be an index
                        // to that Set. If not, it's an invalid patch.
                        nextTokenShouldBeIndex = false;
                        index = ptr.CurrentAsInt;

                        Json list = ((TObjArr)current).Getter(mainApp);
                        current = list._GetAt(index);
                    } else {
                        if (currentIsTApp) {
                            mainApp = ((TObject)current).Getter(mainApp);
//                            mainApp.ResumeTransaction(false);
                            currentIsTApp = false;
                        }

                        if (mainApp.IsArray) {
                            throw new NotImplementedException();
                        }

                        Template t = ((TObject)mainApp.Template).Properties.GetExposedTemplateByName(ptr.Current);
                        if (t == null) {
                            Boolean found = false;
                            if (mainApp._stepSiblings != null && mainApp._stepSiblings.Count > 0) {
                                foreach (Json j in mainApp._stepSiblings) {
                                    if (j._appName == ptr.Current) {
                                        current = j;
                                        found = true;
                                        break;
                                    }
                                }
                            }
                            
                            if (!found) {
                                if (mainApp._appName == ptr.Current) {
                                    current = mainApp;
                                } else {
                                    throw new JsonPatchException(
                                        String.Format("Unknown property '{0}' in path.", ptr.Current),
                                        null
                                    );
                                }
                            }
                        } else {
                            current = t;
                        }
                    }

                    if (current is Json && !(current as Json).IsArray) {
                        mainApp = current as Json;
                    } else if (current is TObject) {
                        currentIsTApp = true;
                    } else if (current is TObjArr) {
                        nextTokenShouldBeIndex = true;
                    } else {
                        // Current token points to a value or an action.
                        // No more tokens should exist. If it does we need to 
                        // return an error.
                        if (ptr.MoveNext()) {
                            throw new JsonPatchException(
                                        String.Format("Invalid path in patch. Property: '{0}' was not expected", ptr.Current),
                                        null
                            );
                        }
                    }
                });
            }

            return new AppAndTemplate(mainApp, current as TValue);
        }
    }
}
