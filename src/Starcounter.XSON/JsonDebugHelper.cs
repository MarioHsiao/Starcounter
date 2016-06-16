using System.Collections;
using System.Text;
using Starcounter.Advanced;
using Starcounter.Advanced.XSON;
using Starcounter.Templates;

namespace Starcounter.XSON {
    internal static class JsonDebugHelper {
        /// <summary>
        /// Returns the classname for the specified template.
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        internal static string GetClassName(Template template) {
            string className = null;
            if (template is TObject)
                className = ((TObject)template).ClassName;
            else if (template is TObjArr) {
                className = ((TObjArr)template).ElementType.ClassName;
            }

            if (className == null)
                className = "<noname>";
            return className;
        }
        
        /// <summary>
        /// Returns the fully qualified name for the specified json, and 
        /// optionally property.
        /// </summary>
        /// <param name="json"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        internal static string GetFullName(Json json, Template property = null) {
            var sb = new StringBuilder();
            WriteFullName(json, sb);
            if (property != null) {
                sb.Append('.');
                sb.Append(property.TemplateName);
            }

            return sb.ToString();
        }
        
        /// <summary>
        /// Returns a string representation of the current state of the specified json, 
        /// including fullname and information about the specified property. 
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        internal static string ToBasicString(Json json, Template property = null) {
            string str = GetFullName(json, property);
            if (property != null) {
                str += " <type: " + property.GetType().Name;
            } else {
                str += " <type: " + json.GetType().Name;
            }

            var data = json.Data;
            if (data != null) {
                str += ", " + data.GetType().FullName;
                if (data is IBindable) {
                    str += ", oid: " + ((IBindable)data).Identity;
                }
                str += ">";
            } else {
                str += ", data: null>";
            }

            return str;
        }

        /// <summary>
        /// Returns a string representation of the current state of the specified json, 
        /// including all properties and values as well as additional info (like dirtyflags etc.)
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        internal static string ToFullString(Json json) {
            var sb = new StringBuilder();
            WriteToDebugString(json, sb, 0, true);
            return sb.ToString();
        }

        private static void WriteFullName(Json json, StringBuilder sb) {
            if (json.Parent != null) {
                WriteFullName(json.Parent, sb);
                sb.Append('.');
                sb.Append(json.Template.TemplateName);
            } else {
                sb.Append(GetClassName(json.Template));
            }
        }

        private static void WriteToDebugString(Json json, StringBuilder sb, int indentation, bool includeStepsiblings) {
            json.Scope(() => {
                if (json.Data != null) {
                    sb.Append(" <data=" + json.Data.GetType().Name + ">");
                }
                if (json.IsDirty()) {
                    sb.Append(" <dirty>");
                }
                if (json.Parent != null) {
                    WriteChildStatus(json.Parent, sb, json.IndexInParent);
                }

                WriteAdditionalInfo(json, sb);

                if (json.IsArray) {
                    WriteArrayToDebugString(json, sb, indentation);
                } else if (json.IsObject) {
                    WriteObjectToDebugString(json, sb, indentation, includeStepsiblings);
                } else { // Single primitive value
                    WriteChildStatus(json, sb, -1);
                }
            });
        }

        private static void WriteAdditionalInfo(Json json, StringBuilder sb) {
            sb.Append("<app: ");
            if (json.appName != null)
                sb.Append(json.appName);
            else
                sb.Append("<null>");

            if (json.wrapInAppName)
                sb.Append(", addNamespaces");

            sb.Append('>');
        }

        private static void WriteArrayToDebugString(Json array, StringBuilder sb, int indentation) {
            sb.Append("[");
            indentation += 3;
            for (int i = 0; i < ((IList)array).Count; i++) {
                if (i > 0) {
                    sb.AppendLine(",");
                    sb.Append(' ', indentation);
                }
                WriteToDebugString((Json)array._GetAt(i), sb, indentation, true);
            }
            indentation -= 3;
            sb.AppendLine();
            sb.Append(' ', indentation);
            sb.Append("]");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="i"></param>
        /// <param name="template"></param>
        private static void WriteObjectToDebugString(Json json, StringBuilder sb, int i, bool includeStepsiblings) {
            TObject template;

            if (json == null || json.Template == null) {
                sb.Append("<null>");
                return;
            }
            template = json.Template as TObject;

            sb.AppendLine("{");

            i += 3;
            int t = 0;
            foreach (var prop in template.Properties.ExposedProperties) {
                if (t > 0) {
                    sb.AppendLine(",");
                }
                sb.Append(' ', i);

                sb.Append('"');
                sb.Append(prop.PropertyName);
                sb.Append("\":");
                if (json.IsTrackingChanges && json.IsDirty(prop.TemplateIndex)) {
                    sb.Append("<changed>");
                }

                if (prop is TContainer) {
                    var cont = (Json)((TContainer)prop).GetUnboundValueAsObject(json);
                    if (cont != null)
                        WriteToDebugString(cont, sb, i, true);
                } else {
                    WriteChildStatus(json, sb, t);
                }
                t++;
            }
            i -= 3;
            sb.AppendLine();
            sb.Append(' ', i);
            sb.Append("}");

            if (includeStepsiblings && json.HasSiblings) {
                foreach (var siblingList in json.allSiblingLists) {
                    foreach (var stepSibling in siblingList) {
                        if (stepSibling == json)
                            continue;
                        sb.AppendLine();
                        sb.Append(' ', i);
                        sb.Append("<stepsibling> ");
                        WriteToDebugString(stepSibling, sb, i, false);
                    }
                }
            }
        }

        private static void WriteChildStatus(Json json, StringBuilder sb, int index) {
            string binding;
            TValue template;

            if (index == -1) {
                template = (TValue)json.Template;
            } else if (json.IsArray) {
                template = null;//((TObjArr)(json.Template)).ElementType as TValue;
            } else {
                template = (json.Template as TObject).Properties[index] as TValue;
            }

            if (template != null) {
                if (template.UseBinding(json)) {
                    binding = template.Bind;
                    sb.Append("<bound: " + binding + ">");
                    if (json.IsArray) {
                        sb.Append("<value: " + template.GetUnboundValueAsObject(json) + ">");
                    } else {
                        sb.Append("<old: " + template.GetUnboundValueAsObject(json));
                        sb.Append(", new: " + template.GetValueAsObject(json) + ">");
                    }
                } else {
                    sb.Append("<value: " + template.GetValueAsObject(json) + ">");
                }
            }
        }
    }
}
