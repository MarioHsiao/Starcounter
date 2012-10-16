	using System;
using System.Collections.Generic;
using System.Text;
using Starcounter.Internal;
using Starcounter.Internal.Application;
using Starcounter.Templates;

namespace Starcounter.Internal.JsonPatch
{
    // TODO:
    // There already is a Metadata class for each property in an app.
    // We should use that one instead of this struct.
    public struct AppAndTemplate
    {
        public readonly App App;
        public readonly Template Template;

        public AppAndTemplate(App app, Template template)
        {
            App = app;
            Template = template;
        }
    }

    public class JsonPatch
    {
        public const Int32 UNDEFINED = 0;
        public const Int32 REMOVE = 1;
        public const Int32 REPLACE = 2;
        public const Int32 ADD = 3;

        private static String[] _patchTypeToString;

        private static Byte[] _addPatchArr;
        private static Byte[] _removePatchArr;
        private static Byte[] _replacePatchArr;

        static JsonPatch()
        {
            _patchTypeToString = new String[4];
            _patchTypeToString[UNDEFINED] = "undefined";
            _patchTypeToString[REMOVE] = "remove";
            _patchTypeToString[REPLACE] = "replace";
            _patchTypeToString[ADD] = "add";

            _addPatchArr = Encoding.UTF8.GetBytes(_patchTypeToString[ADD]);
            _removePatchArr = Encoding.UTF8.GetBytes(_patchTypeToString[REMOVE]);
            _replacePatchArr = Encoding.UTF8.GetBytes(_patchTypeToString[REPLACE]);
        }

        private static String PatchTypeToString(Int32 patchType)
        {
            if ((patchType < 0) || (patchType >= _patchTypeToString.Length))
                throw new ArgumentOutOfRangeException("patchType");
            return _patchTypeToString[patchType];
        }

        public static void EvaluatePatches(String body)
        {
            Byte[] contentArr;
            Byte current;
            Int32 bracketCount;

            Int32 patchType = UNDEFINED;
            JsonPointer pointer = null;
            Byte[] value = null;

            bracketCount = 0;
            contentArr = Encoding.UTF8.GetBytes(body);

            Int32 offset = 0;
            while (offset < contentArr.Length)
            {
                current = contentArr[offset]; 
                if (current == (byte)'{')
                {
                    offset++;
                    if (bracketCount == 0)
                    {
                        // Start of a new patch. Lets read the needed items from it.
                        offset = GetPatchVerb(contentArr, offset, out patchType);
                        offset = GetPatchPointer(contentArr, offset, out pointer);

                        if (patchType != REMOVE)
                        {
                            offset = GetPatchValue(contentArr, offset, out value);
                        }
                        HandleParsedPatch(patchType, pointer, value);
                    }
                    bracketCount++;
                }
                else if (current == '}')
                {
                    bracketCount--;
                }
                offset++;
            }
        }

        private static void HandleParsedPatch(Int32 patchType, JsonPointer pointer, Byte[] value)
        {
            AppAndTemplate aat = JsonPatch.Evaluate(Session.Current.RootApp, pointer);
            Property property = aat.Template as Property;

            if (property == null)
                throw new Exception("Unable to handle input for property of type: " + aat.Template);

            property.ProcessInput(aat.App, value);
        }

        private static Int32 GetPatchValue(Byte[] contentArr, Int32 offset, out Byte[] value)
        {
            Byte current;
            Int32 start;
            Int32 length;

            start = -1;
            length = 0;
            while (offset < contentArr.Length)
            {
                current = contentArr[offset];
                if (current == ':')
                {
                    offset++;
                    current = contentArr[offset];
                    while (current == ' ')
                    {
                        offset++;
                        current = contentArr[offset];
                    }

                    start = offset;
                    if (current == '"') start++;
                }
                else if ((start != -1) && (current == '"' || current == '}'))
                {
                    length = offset - start;
                    break;
                }
                offset++;
            }

            if (start < 0)
            {
                throw new Exception("Cannot find value in patch");
            }

            Byte[] ret = new Byte[length];
            Buffer.BlockCopy(contentArr, start, ret, 0, length);
            value = ret;
//            value = Encoding.UTF8.GetString(contentArr, start, length);
            return offset;
        }

        private static Int32 GetPatchPointer(Byte[] contentArr, Int32 offset, out JsonPointer pointer)
        {
            Byte current;
            Byte[] temp;
            Int32 start;
            Int32 length;

            start = -1;
            length = -1;
            while (offset < contentArr.Length)
            {
                current = contentArr[offset];
                if (current == '"')
                {
                    if (start != -1)
                    {
                        length = offset - start;
                        offset++; 
                        break;
                    }
                    else
                    {
                        offset++;
                        start = offset;
                    }
                }
                else if (current == ',')
                {
                    length = offset - start;
                    offset++;
                    break;
                }
                offset++;
            }

            if (start < 0 || length < 0)
            {
                throw new Exception("Cannot find json pointer in patch");
            }

            // TODO: 
            // Change the pointer so we can use an existing buffer with 
            // pointers to where to read
            temp = new Byte[length];
            Buffer.BlockCopy(contentArr, start, temp, 0, length);
            pointer = new JsonPointer(temp);
            return offset;
        }

        private static Int32 GetPatchVerb(Byte[] contentArr, Int32 offset, out Int32 patchType)
        {
            Boolean quotationMark = false;

            while (contentArr[offset] == ' ')
                offset++;

            if (contentArr[offset] == (byte)'"')
            {
                quotationMark = true;
                offset++;
            }
                
            if (IsPatchVerb(_replacePatchArr, contentArr, offset, contentArr.Length))
            {
                patchType = REPLACE;
                offset += _replacePatchArr.Length + 1;
            }
            else
            {
                throw new Exception("Unsupported json-patch");
            }

            if (quotationMark)
                offset++;
            return offset;
        }

        private static Boolean IsPatchVerb(Byte[] fieldName, Byte[] buffer, Int32 offset, Int32 length)
        {
            Int32 i;

            for (i = 0; i < fieldName.Length; i++)
            {
                if (i == length) return false;
                if (buffer[offset] == fieldName[i])
                {
                    offset++;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        internal static AppAndTemplate Evaluate(App mainApp, String jsonPtr)
        {
            return Evaluate(mainApp, new JsonPointer(jsonPtr));
        }

        /// <summary>
        /// Enumerates the json patch and retrieves the value.
        /// </summary>
        /// <param name="jsonPatch"></param>
        /// <returns></returns>
        internal static AppAndTemplate Evaluate(App mainApp, JsonPointer ptr)
        {
            Boolean nextTokenShouldBeIndex;
            Int32 index;
            Object current = null;

            nextTokenShouldBeIndex = false;
            while (ptr.MoveNext())
            {
                if (nextTokenShouldBeIndex)
                {
                    // Previous object was a Set. This token should be an index
                    // to that Set. If not, it's an invalid patch.
                    nextTokenShouldBeIndex = false;
                    index = ptr.CurrentAsInt;

                    Listing list = mainApp.GetValue((ListingProperty)current);
                    current = list[index];
                }
                else
                {
                    Template t = mainApp.Template.Properties.GetTemplateByName(ptr.Current);

                    if (t == null)
                    {
                        throw new Exception
                        (
                            String.Format("Unknown token '{0}' in patch message '{1}'",
                                          ptr.Current,
                                          ptr.ToString())
                        );
                    }

                    current = t;
                }

                if (current is App)
                {
                    mainApp = current as App;
                }
                else if (current is ListingProperty)
                {
                    nextTokenShouldBeIndex = true;
                }
                else
                {
                    // Current token points to a value or an action.
                    // No more tokens should exist. If it does we need to 
                    // return an error.
                    if (ptr.MoveNext())
                        throw new Exception("Invalid json patch. No further tokens were expected.");
                }
            }

            // TODO:
            // We should return the Metadata instance for the specific 
            // template instead of instancing or own structure here.
            return new AppAndTemplate(mainApp, current as Template);
        }

        // TODO:
        // Change this to return a bytearray since that is the way we are going to send it
        // in the response, or make it so it creates a series of json patches in a submitted
        // buffer instead of allocating a new one here.
        public static String BuildJsonPatch(Int32 patchType, App nearestApp, Template from, Object value, Int32 index)
        {
            List<String> pathList = new List<String>();
            StringBuilder sb = new StringBuilder(40);

            sb.Append('"');
            sb.Append(PatchTypeToString(patchType));
            sb.Append('"');
            sb.Append(':');
            sb.Append('"');
            IndexPathToString(sb, from, nearestApp);
            
            if (index != -1)
            {
                sb.Append('/');
                sb.Append(index);
            }

            sb.Append('"');
            if (patchType != REMOVE)
            {
                sb.Append(", \"value\":");

                if (value is String)
                {
                    sb.Append('"');
                    sb.Append(value);
                    sb.Append('"');
                }
                else if (value is App)
                {
                    sb.Append(((App)value).ToJson());
                }
            }
            return sb.ToString();
        }

        private static void IndexPathToString(StringBuilder sb, Template from, App nearestApp)
        {
            App app;
            AppNode parent;
            Boolean nextIndexIsPositionInList;
            Int32[] path;
            Listing list;
            ListingProperty listProp;
            Template template;
            
            // Find the root app.
            parent = nearestApp;
            while (parent.Parent != null)
                parent = parent.Parent;
            app = (App)parent;

            nextIndexIsPositionInList = false;
            listProp = null;
            path = nearestApp.IndexPathFor(from);
            for (Int32 i = 0; i < path.Length; i++)
            {
                if (nextIndexIsPositionInList)
                {
                    nextIndexIsPositionInList = false;
                    list = (Listing)app.GetValue(listProp);
                    app = list[path[i]];
                    sb.Append('/');
                    sb.Append(path[i]);
                }
                else
                {
                    template = app.Template.Properties[path[i]];
                    sb.Append('/');
                    sb.Append(template.Name);

                    if (template is ListingProperty)
                    {
                        // next index in the path is the index in the list.
                        listProp = (ListingProperty)template;
                        nextIndexIsPositionInList = true;
                    }
                }
            }
        }
    }


}
