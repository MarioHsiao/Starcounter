using System;
using System.Collections.Generic;
using System.Reflection;
using Starcounter.Internal;
using Starcounter.Internal.XSON;
using Starcounter.Templates;

namespace Starcounter.XSON {
    internal static class DataBindingHelper {
        internal static bool ThrowExceptionOnBindindRecreation = false;
        private static string propNotFound = "Property '{2}' was not found in type '{3}' (or baseclass). Json property: '{0}.{1}'.";

        /// <summary>
        /// Looks first in the parent for a property with the same name as the binding name. If no property is 
        /// found the dataobject is used (if any).
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="bindingName"></param>
        /// <param name="template"></param>
        /// <param name="throwException"></param>
        /// <returns></returns>
        internal static BindingInfo SearchForBinding(Json parent, string bindingName, TValue template, bool throwException) {
            BindingInfo bInfo;
            object dataObject;
            TObject tobj;

            bInfo = BindingInfo.Null;
            tobj = parent.Template as TObject;

            bInfo.BoundToType = parent.GetType();
            bInfo.IsBoundToParent = true;

            string pname = bindingName;
            int index = pname.IndexOf('.');
            if (index != -1) {
                pname = pname.Substring(0, index);
            }

            // First check in code-behind. We exclude all (non-user) generated properties from the result to avoid
            // binding to an generated accessor property.
            bInfo = GetBindingPath(bInfo.BoundToType, parent, bindingName, template, false, true);
            bInfo.BoundToType = parent.GetType();
            bInfo.IsBoundToParent = true;
          
            if (bInfo.Member == null) {
                // No member found in code-behind, lets try the dataobject.
                bInfo.IsBoundToParent = false;
                bInfo.BoundToType = null;
                dataObject = parent.Data;
                if (dataObject != null) {
                    bInfo = GetBindingPath(dataObject.GetType(), dataObject, bindingName, template, throwException, false);
                    if (bInfo.BoundToType == null) {
                        if (bInfo.Member != null) {
                            bInfo.BoundToType = bInfo.Member.DeclaringType;
                        } else {
                            bInfo.BoundToType = dataObject.GetType();
                        }
                    }
                }
            } 
            return bInfo;
        }

        /// <summary>
        /// Returns the property with the specified name from the data type. If not found an exception 
        /// is thrown.
        /// </summary>
        /// <param name="dataType"></param>
        /// <param name="bindingName"></param>
        /// <param name="template"></param>
		/// <param name="throwException"></param>
        /// <returns></returns>
        private static BindingInfo GetBindingPath(Type dataType, object data, string bindingName, Template template, bool throwException, bool excludeCodegenMembers) {
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
				memberInfo = GetMemberForBinding(dataType, bindingName, template, throwException, excludeCodegenMembers);
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

					memberInfo = GetMemberForBinding(partType, partName, template, throwException, excludeCodegenMembers);
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
            binfo.BoundToType = null;
            binfo.IsBoundToParent = false;

            if (memberPath != null && memberPath.Count > 0) {
                binfo.BoundToType = memberPath[0].DeclaringType;
            } 
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
		private static MemberInfo GetMemberForBinding(Type dataType, string bindingName, Template template, bool throwException, bool excludeCodegenMembers) {
			var pInfo = ReflectionHelper.FindPropertyOrField(dataType, bindingName, excludeCodegenMembers);
			if (pInfo == null && throwException) {
				throw ErrorCode.ToException(Error.SCERRCREATEDATABINDINGFORJSON,
											string.Format(propNotFound,
														  JsonDebugHelper.GetClassName(template.Parent),
														  template.TemplateName,
														  bindingName,
														  dataType.FullName
										   ));
			}
			return pInfo;
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

		internal static bool HasSetter(MemberInfo mInfo) {
			var pInfo = mInfo as PropertyInfo;
			if (pInfo != null) {
				return (pInfo.GetSetMethod() != null);
			}
			return true;
		}
    }

}
