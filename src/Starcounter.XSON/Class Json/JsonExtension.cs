﻿using System.Collections.Generic;

namespace Starcounter.Advanced.XSON {
    /// <summary>
    /// Extension class for Json. Contains advanced features that can be excluded for normal use.
    /// </summary>
    public static class JsonExtension {
        public static void AddStepSibling(this Json json, Json stepSibling) {
            if (json._stepSiblings == null)
                json._stepSiblings = new List<Json>();
            json._stepSiblings.Add(stepSibling);
            stepSibling._stepParent = json;
        }

        public static bool RemoveStepSibling(this Json json, Json stepSibling) {
            bool b = false;
            if (json._stepSiblings != null) {
                b = json._stepSiblings.Remove(stepSibling);
                stepSibling._stepParent = null;
            }
            return b;
        }

        public static bool HasStepSiblings(this Json json) {
            return (json._stepSiblings != null && json._stepSiblings.Count > 0);
        }

        public static IEnumerable<Json> GetStepSiblings(this Json json) {
            return json._stepSiblings;
        }

        public static string GetAppName(this Json json) {
            return json._appName;
        }

        public static void SetEnableDirtyCheck(this Json json, bool value) {
            json._dirtyCheckEnabled = value;
        }
    }
}
