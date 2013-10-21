using System;
using System.Reflection;
using Starcounter.Internal;
using Starcounter.Logging;
using Starcounter.Templates;
using Starcounter.Internal.XSON;
using System.Collections.Generic;

namespace Starcounter.XSON {
    internal static class DataBindingHelper {
        internal static bool ThrowExceptionOnBindindRecreation = false;
        private static LogSource logSource = new LogSource("json");
        private static string propNotFound = "Property '{2}' was not found in type '{3}' (or baseclass). Json property: '{0}.{1}'.";

        /// <summary>
        /// Returns the property with the specified name from the data type. If not found an exception 
        /// is thrown.
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="bindingName"></param>
        /// <param name="template"></param>
		/// <param name="throwException"></param>
        /// <returns></returns>
        internal static BindingInfo GetBindingPath(Type dataType, object data, string bindingName, Template template, bool throwException) {
			int index;
			int offset;
			string partName;
			Type partType;
			BindingInfo binfo;
			MemberInfo memberInfo = null;
			List<MemberInfo> memberPath = null;
			object currentValue = data;

			index = bindingName.IndexOf('.');
			if (index == -1) {
				memberInfo = GetMemberForBinding(dataType, bindingName, template, throwException);
			} else {
				offset = 0;
				partType = dataType;
				while (offset != -1) {
					if (memberPath == null)
						memberPath = new List<MemberInfo>();

					if (memberInfo != null)
						memberPath.Add(memberInfo);

					if (index == -1) {
						partName = bindingName.Substring(offset);
						offset = -1;
					} else {
						partName = bindingName.Substring(offset, index - offset);
						offset = index + 1;
						index = bindingName.IndexOf('.', offset);
					}

					memberInfo = GetMemberForBinding(partType, partName, template, throwException);
					if (memberInfo == null) {
						memberPath = null;
						break;
					}

					partType = null;
					if (memberInfo is PropertyInfo) {
						if (currentValue != null){
							currentValue = ((PropertyInfo)memberInfo).GetValue(currentValue);
							if (currentValue != null)
								partType = currentValue.GetType();
						}

						if (partType == null)
							partType = ((PropertyInfo)memberInfo).PropertyType;
					} else {
						if (currentValue != null) {
							currentValue = ((FieldInfo)memberInfo).GetValue(currentValue);
							if (currentValue != null)
								partType = currentValue.GetType();
						}

						if (partType == null)
							partType = ((FieldInfo)memberInfo).FieldType;
					}
				}
			}

			binfo.Member = memberInfo;
			binfo.Path = memberPath;
            return binfo;
        }

		/// <summary>
		/// Returns the property with the specified name from the data type.
		/// </summary>
		/// <param name="dataType"></param>
		/// <param name="bindingName"></param>
		/// <param name="template"></param>
		/// <param name="throwException"></param>
		/// <returns></returns>
		private static MemberInfo GetMemberForBinding(Type dataType, string bindingName, Template template, bool throwException) {
			var pInfo = ReflectionHelper.FindPropertyOrField(dataType, bindingName);
			if (pInfo == null && throwException) {
				throw ErrorCode.ToException(Error.SCERRCREATEDATABINDINGFORJSON,
											string.Format(propNotFound,
														  "TODO!", //GetParentClassName(template),
														  template.TemplateName,
														  bindingName,
														  dataType.FullName
										   ));
			}
			return pInfo;
		}

        /// <summary>
        /// Used when exception is raised to get the correct classname for the template.
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        internal static string GetParentClassName(Template template) {
            string className = null;
            if (template.Parent is TObject)
                className = ((TObject)template.Parent).ClassName;
            else if (template.Parent is TObjArr) {
                className = ((TObjArr)template.Parent).ElementType.ClassName;
            }

            if (className == null)
                className = "<noname>";
            return className;
        }

		/// <summary>
		/// 
		/// </summary>
		/// <param name="mInfo"></param>
		/// <returns></returns>
		internal static Type GetMemberReturnType(MemberInfo mInfo) {
			PropertyInfo pInfo = mInfo as PropertyInfo;
			if (pInfo != null)
				return pInfo.PropertyType;
			return ((FieldInfo)mInfo).FieldType;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="mInfo"></param>
		/// <returns></returns>
		internal static Type GetMemberDeclaringType(MemberInfo mInfo) {
			Type type;
			PropertyInfo pInfo = mInfo as PropertyInfo;

			type = mInfo.DeclaringType;
			if (pInfo != null) {
				var method = pInfo.GetGetMethod();
				if (method.IsVirtual) {
					method = method.GetBaseDefinition();
					type = method.DeclaringType;
				}
			}
			return type;
		}
    }

}
