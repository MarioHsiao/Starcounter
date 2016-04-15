// ***********************************************************************
// <copyright file="App.Json.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Templates;
using System;
using System.Text;
using Starcounter.Advanced.XSON;

namespace Starcounter {
	public partial class Json {
        internal string DebugString {
            get {
                var sb = new StringBuilder();
                WriteToDebugString(this, sb, 0, true);
                return sb.ToString();
            }
        }

		private static void WriteToDebugString(Json json, StringBuilder sb, int indentation, bool includeStepsiblings) {
            json.Scope(() => {
                if (json.Data != null) {
                    sb.Append(" <data=" + json.Data.GetType().Name + ">");
                }
                if (json.dirty) {
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
			int t = 0;
			var values = array.valueList;
			foreach (var e in values) {
				if (t > 0) {
					sb.AppendLine(",");
					sb.Append(' ', indentation);
				}
				WriteToDebugString((Json)e, sb, indentation, true);
				t++;
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
				if (json.trackChanges && json.IsDirty(prop.TemplateIndex)) {
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

            if (includeStepsiblings && json.siblings != null && json.siblings.Count > 1) {
                foreach (var stepSibling in json.siblings) {
                    if (stepSibling == json)
                        continue;
                    sb.AppendLine();
                    sb.Append(' ', i);
                    sb.Append("<stepsibling> ");
                    WriteToDebugString(stepSibling, sb, i, false);
                }
            }
		}

		private static void WriteChildStatus(Json json, StringBuilder sb, int index) {
            string binding;
            TValue template;

            if (index == -1) {
                template = (TValue)json.Template;
            } else if (json.IsArray) {
				template = ((TObjArr)(json.Template)).ElementType as TValue;
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