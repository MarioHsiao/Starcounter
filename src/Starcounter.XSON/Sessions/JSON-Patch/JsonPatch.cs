using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Newtonsoft.Json;
using Starcounter.Advanced.XSON;
using Starcounter.Internal.XSON;
using Starcounter.Templates;
using Starcounter.XSON.Interfaces;

namespace Starcounter.XSON {
    public enum JsonPatchStatus {
        Unknown,
        Applied,
        Queued,
        AlreadyApplied,
        Rejected,
        Processing
    }
    
    /// <summary>
    /// Class for evaluating, handling and creating json-patch to and from typed json objects 
    /// and logged changes done in a typed json object during a request.
    /// 
    /// The json-patch is implemented according to http://tools.ietf.org/html/rfc6902
    /// </summary>
    public class JsonPatch {
        private static string testRemoteVersionPatchStart = @"{""op"":""test"",""path"":""/";
        private static string replaceLocalVersionPatchStart = @"{""op"":""replace"",""path"":""/";
        private static string patchStartToOp = @"{""op"":""";
        private static string patchEndToPath = @""",""path"":""";
        private static string patchEndToValue = @""",""value"":";

        private static string[] patchOpArr;
        
        private Action<Json, JsonPatchOperation, JsonPointer, string> patchHandler = DefaultPatchHandler.Handle;
        
        private enum JsonPatchMember {
            Invalid,
            Op,
            Path,
            Value
        }

        static JsonPatch() {
            patchOpArr = new string[6];
            patchOpArr[(int)JsonPatchOperation.Undefined] = JsonPatchOperation.Undefined.ToString().ToLower();
            patchOpArr[(int)JsonPatchOperation.Remove] = JsonPatchOperation.Remove.ToString().ToLower();
            patchOpArr[(int)JsonPatchOperation.Replace] = JsonPatchOperation.Replace.ToString().ToLower();
            patchOpArr[(int)JsonPatchOperation.Add] = JsonPatchOperation.Add.ToString().ToLower();
            patchOpArr[(int)JsonPatchOperation.Move] = JsonPatchOperation.Move.ToString().ToLower();
            patchOpArr[(int)JsonPatchOperation.Test] = JsonPatchOperation.Test.ToString().ToLower();
        }

        public JsonPatch() {
        }

        #region obsoleted API
        [Obsolete("Use the overload that takes a Func with value as string instead of IntPtr and length.", true)]
        public void SetPatchHandler(Action<Json, JsonPatchOperation, JsonPointer, IntPtr, int> handler) {
        }
        
        [Obsolete("Using IntPtr is no longer supported. Use method that takes source as string or byte[] instead")]
        public int Apply(Json root, IntPtr patchArrayPtr, int patchArraySize, bool strictPatchRejection = true) {
            int count;
            byte[] tmp = new byte[patchArraySize];

            Marshal.Copy(patchArrayPtr, tmp, 0, patchArraySize);
            var status = Apply(root, tmp, strictPatchRejection, out count);
            return ConvertStatusToCount(status, count);
        }
        
        #endregion


        public void SetPatchHandler(Action<Json, JsonPatchOperation, JsonPointer, string> handler) {
            patchHandler = handler;
        }
        
        /// <summary>
        /// Generates a JSON-Patch array for all changes logged in the changelog.
        /// </summary>
        /// <param name="flushLog">If true, the change log will be reset</param>
        /// <returns>The JSON-Patch string (see RFC6902)</returns>
        public int Generate(Json json, bool flushLog, bool includeNamespace, out byte[] patches) {
            string str = Generate(json, flushLog, includeNamespace);
            if (str != null) {
                patches = Encoding.UTF8.GetBytes(str);
                return patches.Length;
            }
            patches = null;
            return -1;
        }

        /// <summary>
        /// Generates a JSON-Patch array for all changes logged in the changelog
        /// </summary>
        /// <param name="flushLog">If true, the change log will be reset</param>
        /// <returns>The JSON-Patch string (see RFC6902)</returns>
        public string Generate(Json json, bool flushLog, bool includeNamespace) {
            bool addComma;
            bool versioning;
            int pos;
            Change[] changes;
            StringBuilder sb;
            
            var changeLog = json.ChangeLog;
            if (changeLog == null) {
                throw new Exception("Cannot generate patches on json that has no changelog attached.");
            }

            var session = json.Session;
            if (session != null) {
                session.enableNamespaces = true;
                session.enableCachedReads = true;
            }
            try {
                if (!changeLog.Generate(false, out changes)) {
                    return null;
                }

                sb = new StringBuilder();
                using (var writer = new StringWriter(sb)) {
                    versioning = (changeLog.Version != null);

                    writer.Write('[');

                    addComma = false;
                    if (versioning) {
                        writer.Write(replaceLocalVersionPatchStart);
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
                        addComma = true;
                    }
                    
                    for (int i = 0; i < changes.Length; i++) {
                        pos = sb.Length;

                        if (addComma)
                            writer.Write(',');
                        addComma = true;

                        if (!WritePatch(changes[i], writer, includeNamespace))
                            sb.Length = pos;
                    }

                    writer.Write(']');

                    if (flushLog)
                        changeLog.Checkpoint();
                    return sb.ToString();
                }
            } finally {
                if (session != null) {
                    session.enableNamespaces = false;
                    session.enableCachedReads = false;
                }
            }
        }

        /// <summary>
        /// Builds the json patch.
        /// </summary>
        /// <param name="patchType">Type of the patch.</param>
        /// <param name="nearestApp">The nearest app.</param>
        /// <param name="from">From.</param>
        /// <param name="index">The index.</param>
        /// <returns>String.</returns>
        private bool WritePatch(Change change, TextWriter writer, bool includeNamespace) {
            ITypedJsonSerializer serializer;

            writer.Write(patchStartToOp);
            writer.Write(patchOpArr[change.ChangeType]);
            writer.Write(patchEndToPath);
            
            if (!WritePath(change, writer, includeNamespace)) {
                return false;
            }

            if (change.ChangeType == Change.MOVE)
                throw new NotImplementedException("Move operation is not yet implemented.");
            
            if (change.ChangeType != (int)JsonPatchOperation.Remove) {
                writer.Write(patchEndToValue);
                
                if (change.Property == null) {
                    change.Parent.calledFromStepSibling = change.SuppressNamespace;
                    try {
                        serializer = change.Parent.JsonSerializer;
                        serializer.Serialize(change.Parent, writer);
                    } finally {
                        change.Parent.calledFromStepSibling = false;
                    }
                } else if (change.Index != -1) {
                    change.Item.calledFromStepSibling = change.SuppressNamespace;
                    try {
                        serializer = ((TValue)change.Item.Template).JsonSerializer;
                        serializer.Serialize(change.Item, writer);
                    } finally {
                        change.Parent.calledFromStepSibling = false;
                    }
                } else {
                    change.Parent.JsonSerializer.Serialize(change.Parent, change.Property, writer);
                }
                
            } else {
                writer.Write('"');
            }

            writer.Write('}');
            return true;
        }

        private bool WritePath(Change change, TextWriter writer, bool includeNamespace) {
            if (WritePathRecursive(change.Parent, writer, includeNamespace, false)) {
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

        private bool WritePathRecursive(Json json, TextWriter writer, bool includeNamespace, bool calledFromStepSibling) {
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
            if (!calledFromStepSibling && json.Siblings != null) {
                foreach (Json stepSibling in json.Siblings) {
                    if (stepSibling == json)
                        continue;

                    pathWritten = WritePathRecursive(stepSibling, writer, includeNamespace, true);
                    if (pathWritten)
                        break;
                }
            }

            parent = null;
            if (!pathWritten) {
                parent = json.Parent;
                pathWritten = WritePathRecursive(parent, writer, includeNamespace, false);
            }

            if (!pathWritten)
                return false;

            if (parent != null) {
                if (parent.IsArray) {
                    if (json.cacheIndexInArr == -1)
                        json.UpdateCachedIndex();
                    writer.Write('/');
                    writer.Write(json.cacheIndexInArr);
                } else {
                    // We use the cacheIndexInArr to keep track of obj that is set
                    // in the parent as an untyped object since the template here is not
                    // the template in the parent (which we want).
                    if (json.cacheIndexInArr != -1) {
                        template = ((TObject)parent.Template).Properties[json.cacheIndexInArr];
                    } else {
                        template = json.Template;
                    }
                    writer.Write('/');
                    writer.Write(template.TemplateName);
                }
            }

            if (includeNamespace && !calledFromStepSibling && json.wrapInAppName) {
                writer.Write('/');
                writer.Write(json.appName);
            }
            return true;
        }

        /// <summary>
        /// Applies patches and triggers the appropiate inputhandlers
        /// </summary>
        /// <param name="root">The root json</param>
        /// <param name="patchArray">The array containing all patches.</param>
        /// <param name="strictPatchRejection">
        /// If set to true, rejected patches will throw exception otherwise they will 
        /// be ignored (and the rest of the patches will be applied)
        /// </param>
        /// <returns>The number of patches applied.</returns>
        public int Apply(Json root, byte[] patchArray, bool strictPatchRejection = true) {
            int count;
            var status = Apply(root, patchArray, strictPatchRejection, out count);
            return ConvertStatusToCount(status, count);
        }

        /// <summary>
        /// Applies patches and triggers the appropiate inputhandlers
        /// </summary>
        /// <param name="root">The root json</param>
        /// <param name="patch">The string containing all patches.</param>
        /// <param name="strictPatchRejection">
        /// If set to true, rejected patches will throw exception otherwise they will 
        /// be ignored (and the rest of the patches will be applied)
        /// </param>
        /// <returns>The number of patches applied.</returns>
        public int Apply(Json root, string patch, bool strictPatchRejection = true) {
            int count;
            var status = Apply(root, patch, strictPatchRejection, out count);
            return ConvertStatusToCount(status, count);
        }

        /// <summary>
        /// Applies patches and triggers the appropiate inputhandlers
        /// </summary>
        /// <param name="root">The root json</param>
        /// <param name="patchArray">The bytearray containing all patches.</param>
        /// <param name="strictPatchRejection">
        /// If set to true, rejected patches will throw exception otherwise they will 
        /// be ignored (and the rest of the patches will be applied)
        /// </param>
        /// <param name="patchCount">The number of applied patches.</param>
        /// <returns>The status of the patches that was applied.</returns>
        public JsonPatchStatus Apply(Json root, byte[] patchArray, bool strictPatchRejection, out int patchCount) {
            string patch = Encoding.UTF8.GetString(patchArray);
            return Apply(root, patch, strictPatchRejection, out patchCount);
        }

        /// <summary>
        /// Applies patches and triggers the appropiate inputhandlers
        /// </summary>
        /// <param name="root">The root json</param>
        /// <param name="patch">The string containing all patches.</param>
        /// <param name="strictPatchRejection">
        /// If set to true, rejected patches will throw exception otherwise they will 
        /// be ignored (and the rest of the patches will be applied)
        /// </param>
        /// <param name="patchCount">The number of applied patches.</param>
        /// <returns>The status of the patches that was applied.</returns>
        public JsonPatchStatus Apply(Json root, string patch, bool strictPatchRejection, out int patchCount) {
            using (var reader = new JsonTextReader(new StringReader(patch))) {
                reader.CloseInput = true;
                return this.Apply(root, reader, strictPatchRejection, patch, out patchCount);
            }
        }

        private JsonPatchStatus Apply(Json root, JsonTextReader reader, bool strictPatchRejection, object sourceJson, out int patchCount) {
            ChangeLog changeLog = null;
            int rejectedPatches = 0;
            JsonPatchStatus status = JsonPatchStatus.Unknown;
            JsonPatchMember member;
            JsonPointer pointer = null;
            JsonPatchOperation operation = JsonPatchOperation.Undefined;
            bool hasValue = false;
            string value = null;
            ViewModelVersion version = null;

            if (root != null) {
                changeLog = root.ChangeLog;
                version = changeLog?.Version;
                if (version != null) {
                    version.RemoteLocalVersion = version.LocalVersion;
                }
            }
            patchCount = 0;

            try {
                JsonToken token = JsonToken.Null;
                while (reader.Read()) {
                    token = reader.TokenType;
                    switch (token) {
                        case JsonToken.StartObject:
                            if (status == JsonPatchStatus.Processing)
                                ThrowPatchException(patchCount, "TODO!", sourceJson);

                            patchCount++;
                            member = JsonPatchMember.Invalid;
                            pointer = null;
                            operation = JsonPatchOperation.Undefined;
                            hasValue = false;
                            status = JsonPatchStatus.Processing;
                            break;
                        case JsonToken.PropertyName:
                            if (status != JsonPatchStatus.Processing)
                                ThrowPatchException(patchCount, "TODO!", sourceJson);

                            if (!Enum.TryParse<JsonPatchMember>((string)reader.Value, true, out member) 
                                || member == JsonPatchMember.Invalid)
                                throw new Exception("Unknown property in patch: " + reader.Value);

                            switch (member) {
                                case JsonPatchMember.Op:
                                    if (!Enum.TryParse<JsonPatchOperation>(reader.ReadAsString(), true, out operation))
                                        throw new Exception("TODO!");
                                    break;
                                case JsonPatchMember.Path:
                                    pointer = new JsonPointer(reader.ReadAsString());
                                    break;
                                case JsonPatchMember.Value:
                                    // TODO: 
                                    // Read depending on type from property
                                    value = reader.ReadAsString();
                                    hasValue = true;
                                    break;
                            }
                            break;
                        case JsonToken.EndObject:
                            if (status != JsonPatchStatus.Processing)
                                ThrowPatchException(patchCount, "TODO!", sourceJson);

                            if (operation == JsonPatchOperation.Undefined)
                                ThrowPatchException(patchCount,"Unsupported patch operation in patch.", sourceJson);

                            if (pointer == null)
                                ThrowPatchException(patchCount, "No path found in patch.", sourceJson);

                            if ((operation != JsonPatchOperation.Remove) && (!hasValue))
                                ThrowPatchException(patchCount, "No value found in patch.", sourceJson);

                            status = HandleOnePatch(root, operation, pointer, value, patchCount, changeLog, sourceJson, strictPatchRejection);
                            if (status == JsonPatchStatus.AlreadyApplied || status == JsonPatchStatus.Queued)
                                return status;

                            if (status == JsonPatchStatus.Rejected)
                                rejectedPatches++;                            
                            break;
                    }
                }

                // TODO:
                // Check unexpected end of content.
                if (status == JsonPatchStatus.Processing)
                    ThrowPatchException(-1, "Unexpected end of content", sourceJson);

                if (version != null && patchCount >= 0) {
                    byte[] enqueuedPatch = version.GetNextEnqueuedPatch();
                    if (enqueuedPatch != null) {
                        // TODO:
                        int otherPatchCount;
                        JsonPatchStatus otherStatus = Apply(root, enqueuedPatch, strictPatchRejection, out otherPatchCount);
                        patchCount += otherPatchCount;
                    }
                }
            } catch (JsonPatchException jpex) {
                if (string.IsNullOrEmpty(jpex.Patch))
                    jpex.Patch = GetPatchAsString(patchCount, sourceJson);
                throw;
            }

            if (rejectedPatches > 0) {
                // TODO:
                // Callback for rejected patches.
            }

            return JsonPatchStatus.Applied;
        }

        private JsonPatchStatus HandleOnePatch(Json root, 
                                               JsonPatchOperation op, 
                                               JsonPointer ptr, 
                                               string value, 
                                               int patchCount, 
                                               ChangeLog changeLog, 
                                               object sourceJson, 
                                               bool strictRejection) {
            long remoteVersion = -1;
            ViewModelVersion version = changeLog?.Version;

            if ((version != null) && patchCount <= 2) {
                if (patchCount == 1) {
                    if ((op != JsonPatchOperation.Replace) || !VerifyVersioningPatchPath(ptr, version.RemoteVersionPropertyName)) {
                        // First patch need to be replace for client version.
                        ThrowPatchException(patchCount, "First patch when versioncheck is enabled have to be replace for remote version.", sourceJson);
                    }

                    if (!long.TryParse(value, out remoteVersion))
                        JsonHelper.ThrowWrongValueTypeException(null, version.RemoteVersionPropertyName, "Int64", value);

                    if (remoteVersion != (version.RemoteVersion + 1)) {
                        if (remoteVersion <= version.RemoteVersion) {
                            return JsonPatchStatus.AlreadyApplied;
                        } else {
                            version.EnqueuePatch(GetSourceAsBytes(sourceJson), (int)(remoteVersion - version.RemoteVersion - 2));
                            return JsonPatchStatus.Queued;
                        }
                    }
                    version.RemoteVersion = remoteVersion;
                } else {
                    // Second patch -> determine if transformations are needed and what version.
                    if ((op != JsonPatchOperation.Test) || !VerifyVersioningPatchPath(ptr, version.LocalVersionPropertyName)) {
                        ThrowPatchException(patchCount, "Second patch when versioncheck is enabled have to be test of local version.", sourceJson);
                    }

                    if (!long.TryParse(value, out remoteVersion))
                        JsonHelper.ThrowWrongValueTypeException(null, version.LocalVersionPropertyName, "Int64", value);
                    version.RemoteLocalVersion = remoteVersion;
                    
                    changeLog.CleanupOldVersionLogs();
                }
            } else {
                if (patchHandler != null) {
                    try {
                        patchHandler(root, op, ptr, value);
                    } catch (JsonPatchException jpe) {
                        if (strictRejection || jpe.Severity > 0)
                            throw;
                        return JsonPatchStatus.Rejected;
                    } catch (FormatException) {
                        if (strictRejection)
                            throw;
                        return JsonPatchStatus.Rejected;
                    }
                }
            }
            return JsonPatchStatus.Applied;
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
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="patchStart"></param>
        /// <param name="patchEnd"></param>
        /// <param name="data"></param>
        /// <param name="msg"></param>
        private void ThrowPatchException(int patchNo, string msg, object source) {
            throw new JsonPatchException(1, msg, GetPatchAsString(patchNo, source));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="patchStart"></param>
        /// <param name="data"></param>
        /// <param name="dataSize"></param>
        /// <returns></returns>
        private string GetPatchAsString(int patchNo, object source) {
            string patch;

            patch = source as string;
            if (patch == null) {
                patch = "";
                if (source is byte[]) {
                    patch = Encoding.UTF8.GetString((byte[])source);
                } 
            }

            int objCount = 0;
            int startPos = 0;
            int patchCount = 0;

            // If patchNo is zero or below we want the whole source.
            if (patchNo > 0) {
                for (int i = 0; i < patch.Length; i++) {
                    char c = patch[i];
                    if (c == '{') {
                        if (objCount == 0) {
                            startPos = i;
                            patchCount++;
                        }
                        objCount++;
                    } else if (c == '}') {
                        objCount--;
                        if ((objCount == 0) && (patchCount == patchNo)) {
                            // We found the patch we're interested of.
                            patch = patch.Substring(startPos, i - startPos + 1);
                            break;
                        }
                    }
                }
            }
            return patch;
        }

        private byte[] GetSourceAsBytes(object source) {
            if (source is byte[])
                return (byte[])source;
            else
                return Encoding.UTF8.GetBytes((string)source);
        }

        /// <summary>
        /// Converts the status to the old hacky way of determining the status of
        /// a batch of patches when versioning is used.
        /// </summary>
        /// <param name="status">The status of the applied patches</param>
        /// <param name="patchCount">The count of applied patches</param>
        /// <returns>
        /// If the status says that the patch is either queued or already applied
        /// the return value will be negative (-1 for already applied, -2 for queued),
        /// otherwise the applied count will be returned.
        /// </returns>
        private int ConvertStatusToCount(JsonPatchStatus status, int patchCount) {
            if (status == JsonPatchStatus.AlreadyApplied)
                return -1;
            else if (status == JsonPatchStatus.Queued)
                return -2;

            return patchCount;
        }
    }
}
