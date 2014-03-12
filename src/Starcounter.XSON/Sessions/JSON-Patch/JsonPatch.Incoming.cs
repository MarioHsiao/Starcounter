using System;
using System.Diagnostics;
using System.Text;
using Starcounter.Templates;

namespace Starcounter.Internal.JsonPatch {
    partial class JsonPatch {
        /// <summary>
        /// Evaluates the patches.
        /// </summary>
        /// <param name="rootApp">the root app for this request.</param>
        /// <param name="body">The body of the request.</param>
        /// <returns>The number of patches evaluated.</returns>
        public static int EvaluatePatches(Json rootApp, byte[] body) {
            byte[] contentArr;
            byte current;
            int bracketCount;

            JsonPatchMember member;
            int patchType = UNDEFINED;
            JsonPointer pointer = null;
            byte[] value = null;
            bool valueFound = false;
            int patchStart = -1;
            int patchCount = 0;
            
            bracketCount = 0;
            contentArr = body;

            Int32 offset = 0;
            try {
                while (offset < contentArr.Length) {
                    current = contentArr[offset];
                    if (current == (byte)'{') {
                        patchStart = offset;
                        offset++;
                        if (bracketCount == 0) {
                            // Start of a new patch. Lets read the needed items from it.
                            valueFound = false;
                            bool first = true;
                            for (int mi = 0; mi < 3; mi++) {
                                offset = GotoMember(contentArr, offset, first);
                                if (offset == -1)
                                    ThrowPatchException(patchStart, body, "All needed properties was not found in patch.");

                                first = false;
                                offset = GetPatchMember(contentArr, offset, out member);
                                if (offset == -1)
                                    ThrowPatchException(patchStart, body, "Unexpected end of patch.");

                                offset = GotoValue(contentArr, offset);
                                if (offset == -1)
                                    ThrowPatchException(patchStart, body, "Unexpected end of patch.");

                                switch (member) {
                                    case JsonPatchMember.Op:
                                        offset = GetPatchVerb(contentArr, offset, out patchType);
                                        if (patchType == UNDEFINED)
                                            ThrowPatchException(patchStart, body, "Unsupported operation in patch.");
                                        break;
                                    case JsonPatchMember.Path:
                                        offset = GetPatchPointer(contentArr, offset, out pointer);
                                        if (pointer == null)
                                            ThrowPatchException(patchStart, body, "Cannot find path in patch.");
                                        break;
                                    case JsonPatchMember.Value:
                                        offset = GetPatchValue(contentArr, offset, out value);
                                        valueFound = true;
                                        break;
                                    default:
                                        ThrowPatchException(patchStart, body, "Unknown property in patch.");
                                        break;
                                }
                            }

                            if (!valueFound)
                                ThrowPatchException(patchStart, body, "No value found in patch.");

                            patchCount++;
                            HandleParsedPatch(rootApp, patchType, pointer, value);
                        }
                        bracketCount++;
                    } else if (current == '}') {
                        bracketCount--;
                    }
                    offset++;
                }
            } catch (JsonPatchException ex) {
                if (patchStart != -1)
                    ex.Patch = Encoding.UTF8.GetString(body, patchStart, GetPatchLength(patchStart, body));
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
        private static void ThrowPatchException(int patchStart, byte[] data, string msg) {
            string patch = "";
            
            if (patchStart != -1) {
                patch = Encoding.UTF8.GetString(data, patchStart, GetPatchLength(patchStart, data));
            }
            throw new JsonPatchException(msg, patch);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="patchStart"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private static int GetPatchLength(int patchStart, byte[] data) {
            int bracketCount = 1;
            int patchEnd = patchStart + 1;

            while (patchEnd < data.Length) {
                if (data[patchEnd] == '}') {
                    bracketCount--;
                    if (bracketCount == 0)
                        break;
                } else if (data[patchEnd] == '{')
                    bracketCount++;

                patchEnd++;
            }
            return (patchEnd - patchStart) + 1;
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
        /// Gets the patch value.
        /// </summary>
        /// <param name="contentArr">The content arr.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="value">The value.</param>
        /// <returns>Int32.</returns>
        /// <exception cref="System.Exception">Cannot find value in patch</exception>
        private static Int32 GetPatchValue(Byte[] contentArr, Int32 offset, out Byte[] value) {
            bool quotation;
            int start;
            int length;
		
            quotation = false;
            if (contentArr[offset] == '"') {
                offset++;
                quotation = true;
            }

            start = offset;
            if (quotation) {
                while (offset < contentArr.Length) {
					if (contentArr[offset] == '\\') {
						offset++;
					} else if (contentArr[offset] == '"')
                        break;
                    offset++;
                }
            }
            else {
                while (offset < contentArr.Length) {
                    if (contentArr[offset] == ' '
                        || contentArr[offset] == '}'
                        || contentArr[offset] == ',')
                        break;
                    offset++;
                }
            }

            length = offset - start;
            byte[] ret = new byte[length];
            Buffer.BlockCopy(contentArr, start, ret, 0, length);
            value = ret;
            return offset;
        }

        /// <summary>
        /// Gets the patch pointer.
        /// </summary>
        /// <param name="contentArr">The content arr.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="pointer">The pointer.</param>
        /// <returns>Int32.</returns>
        /// <exception cref="System.Exception">Cannot find json pointer in patch</exception>
        private static Int32 GetPatchPointer(Byte[] contentArr, Int32 offset, out JsonPointer pointer) {
            Byte current;
            Byte[] temp;
            Int32 start;
            Int32 length;

            start = -1;
            length = -1;
            while (offset < contentArr.Length) {
                current = contentArr[offset];
                if (current == '"') {
                    if (start != -1) {
                        length = offset - start;
                        offset++;
                        break;
                    }
                    else {
                        offset++;
                        start = offset;
                    }
                }
                else if (current == ',') {
                    length = offset - start;
                    offset++;
                    break;
                }
                offset++;
            }

            if (start < 0 || length < 0) {
                pointer = null;
            } else {
                // TODO: 
                // Change the pointer so we can use an existing buffer with 
                // pointers to where to read
                temp = new Byte[length];
                Buffer.BlockCopy(contentArr, start, temp, 0, length);
                pointer = new JsonPointer(temp);
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
                }
                else {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Called after a jsonpatch is read. Will evaluate the jsonpointer
        /// and call the appropriate handler in Apps.
        /// </summary>
        /// <param name="rootApp">The root application from which the pointer starts.</param>
        /// <param name="patchType">The type of patch</param>
        /// <param name="pointer">A jsonpointer that points to the value to be patched</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.Exception">TODO:</exception>
        private static void HandleParsedPatch(Json rootApp, Int32 patchType, JsonPointer pointer, Byte[] value) {
            Debug.WriteLine("Handling patch for: " + pointer.ToString());

            if (rootApp != null) {
                AppAndTemplate aat = JsonPatch.Evaluate(rootApp, pointer);

                if (!aat.Template.Editable) {
                    throw new JsonPatchException(
                        String.Format("Property '" + aat.Template.PropertyName + "' is readonly."),
                        null
                    );
                }

                aat.App.ExecuteInScope(() => {
                    ((TValue)aat.Template).ProcessInput(aat.App, value);
                });
            }
        }

        /// <summary>
        /// Evaluates the specified main app.
        /// </summary>
        /// <param name="mainApp">The main app.</param>
        /// <param name="jsonPtr">The json PTR.</param>
        /// <returns>AppAndTemplate.</returns>
        internal static AppAndTemplate Evaluate(Json mainApp, String jsonPtr) {
            return Evaluate(mainApp, new JsonPointer(jsonPtr));
        }

        /// <summary>
        /// Enumerates the json patch and retrieves the value.
        /// </summary>
        /// <param name="mainApp">The main app.</param>
        /// <param name="ptr">The PTR.</param>
        /// <returns>AppAndTemplate.</returns>
        /// <exception cref="System.Exception"></exception>
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
                        Template t = ((TObject)mainApp.Template).Properties.GetTemplateByName(ptr.Current);

                        if (t == null) {
                            throw new JsonPatchException(
                                String.Format("Unknown property '{0}' in path.", ptr.Current),
                                null
                            );
                        }

                        current = t;
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
