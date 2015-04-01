// ***********************************************************************
// <copyright file="App.Json.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************

using Starcounter.Templates;
using System;
using System.Text;

namespace Starcounter {
	public partial class Json {
		internal void WriteToDebugString(StringBuilder sb, int indentation, bool includeStepsiblings) {
			if (Data != null) {
				sb.Append("(data=" + Data.GetType().Name + ")");
			}
			if (_Dirty) {
				sb.Append("(dirty)");
			}
			if (Parent != null) {
				Parent.WriteChildStatus(sb, this.IndexInParent);
			}
			if (IsArray) {
				WriteArrayToDebugString(sb, indentation);
			} else {
                WriteObjectToDebugString(sb, indentation, includeStepsiblings);
			}
		}

		private void WriteArrayToDebugString(StringBuilder sb, int indentation) {

			sb.Append("[");
			indentation += 3;
			int t = 0;
			var values = list;
			foreach (var e in values) {
				if (t > 0) {
					sb.AppendLine(",");
					sb.Append(' ', indentation);
				}
				(e as Json).WriteToDebugString(sb, indentation, true);
				t++;
			}
			indentation -= 3;
			sb.AppendLine();
			sb.Append(' ', indentation);
			sb.Append("]");
		}

		internal string DebugString {
			get {
				var sb = new StringBuilder();
				WriteToDebugString(sb, 0, true);
				return sb.ToString();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sb"></param>
		/// <param name="i"></param>
		private void WriteObjectToDebugString(StringBuilder sb, int i, bool includeStepsiblings) {
			if (this.IsArray) {
				throw new NotImplementedException();
				//                WriteToDebugString(sb, i, (ArrSchema<Json>)Template);
			} else {
                WriteObjectToDebugString(sb, i, (TObject)Template, includeStepsiblings);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="sb"></param>
		/// <param name="i"></param>
		/// <param name="template"></param>
		private void WriteObjectToDebugString(StringBuilder sb, int i, TObject template, bool includeStepsiblings) {
			if (Template == null) {
				sb.Append("{}");
				return;
			}

            //if (Session != null && Parent != null) {
            //    var index = Parent.IndexOf(this);
            //    if (Parent._isStatefulObject && Parent._SetFlag[index]) {
            //        sb.Append("(set)");
            //    }
            //}

            if (Parent != null && !string.IsNullOrEmpty(_appName)) {
                sb.Append('<');
                sb.Append(_appName);
                sb.Append(">");
            }

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
				if (_trackChanges && WasReplacedAt(prop.TemplateIndex)) {
					sb.Append("(direct-set)");
				}

				if (prop is TContainer) {
                    ((Json)((TContainer)prop).GetUnboundValueAsObject(this)).WriteToDebugString(sb, i, true);
				} else {
					WriteChildStatus(sb, t);
					//sb.Append(((TValue)prop).ValueToJsonString(this));
				}
				t++;
			}
			i -= 3;
			sb.AppendLine();
			sb.Append(' ', i);
			sb.Append("}");

            if (includeStepsiblings && _stepSiblings != null && _stepSiblings.Count > 1) {
                foreach (var stepSibling in _stepSiblings) {
                    if (stepSibling == this)
                        continue;
                    stepSibling.WriteToDebugString(sb, i, false);
                }
            }
		}

		private void WriteChildStatus(StringBuilder sb, int index) {
            object oldValue;
            string binding;
            TValue template;

			if (IsArray) {
				template = (Template as TObjArr).ElementType as TValue;
			} else {
				template = (Template as TObject).Properties[index] as TValue;
			}

			if (template != null) {
                binding = template.Bind;
                if (binding != null && Data != null) {
					if (binding != template.PropertyName) {
						sb.Append("(bound path=" + binding + ")");
					} else {
						sb.Append("(bound)");
					}
				}

                if (IsArray) {
                    var cjson = this._GetAt(index) as Json;
                    oldValue = "notsent";
                    if (cjson != null && cjson.HasBeenSent)
                        oldValue = cjson;
                } else {
                    oldValue = template.GetUnboundValueAsObject(this);
                    if (template.GetUnboundValueAsObject(this) != oldValue) {
                        if (oldValue == null)
                            oldValue = "notsent";
                    }
                }
                sb.Append("(indirect-set old=" + oldValue + ")");
			}
		}
	}
}