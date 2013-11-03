using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using FastReflectionLib;
using System.Collections;

namespace Starcounter.Applications.UsageTrackerApp.Import {
    /// <summary>
    /// 
    /// </summary>
    public class Utils {

        private static Hashtable propertyCache = new Hashtable();  // TODO: Clear the Hashtable when new itemssource is set or when Items.Length is 0

        /// <summary>
        /// 
        /// </summary>
        public static void ClearCache() {
            FastReflectionCaches.ClearAllCaches();
            propertyCache.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static object GetValue(object item, string propertyName) {

            PropertyInfo propertyInfo;

            if (propertyCache.ContainsKey(propertyName)) {
                propertyInfo = (PropertyInfo)propertyCache[propertyName];
            }
            else {
                Type theType = item.GetType();
                propertyInfo = theType.GetProperty(propertyName);
                propertyCache.Add(propertyName, propertyInfo);
            }

            if (propertyInfo == null) {
                // TODO: Throw exception
                throw new MissingFieldException(item.GetType().ToString(), propertyName);
                //return "*Error* Property \"" + this.AxisHeaderDisplayName + "\" dosent exist on " + item.GetType(); // TODO: throw error? property name is missing?
            }

            return propertyInfo.FastGetValue(item);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="propertyName"></param>
        /// <param name="value"></param>
        public static void SetValue(object item, string propertyName, object value) {

            PropertyInfo propertyInfo;

            if (propertyCache.ContainsKey(propertyName)) {
                propertyInfo = (PropertyInfo)propertyCache[propertyName];
            }
            else {
                Type theType = item.GetType();
                propertyInfo = theType.GetProperty(propertyName);
                propertyCache.Add(propertyName, propertyInfo);
            }

            if (propertyInfo == null) {
                throw new MissingFieldException(item.GetType().ToString(), propertyName);
            }

            if (propertyInfo.CanWrite) {
                object obj = Convert.ChangeType(value, propertyInfo.PropertyType);
                propertyInfo.FastSetValue(item, obj);
            }
        }


    }
}
