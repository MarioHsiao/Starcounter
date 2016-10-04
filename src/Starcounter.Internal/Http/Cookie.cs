using System;
using System.Web;

namespace Starcounter {

    /// <summary>
    /// HTTP cookie helper class.
    /// </summary>
    public class Cookie {
        public string Name { get; set; }
        public string Value { get; set; }
        public string Path { get; set; }
        public DateTime? Expires { get; set; }

        public Cookie() { 
        }

        /// <summary>
        /// String Value to parse cookie from.
        /// </summary>
        /// <param name="Value"></param>
        public Cookie(string Value) {
            this.ParseString(Value);
        }

        public Cookie(string Name, string Value) {
            this.Name = Name;
            this.Value = Value;
        }

        public static Cookie Parse(string Value) {
            return new Cookie(Value);
        }

        public static bool TryParse(string Value, out Cookie Cookie) {
            try {
                Cookie = new Cookie(Value);
                return true;
            } catch {
                Cookie = null;
                return false;
            }
        }

        public void ParseString(string Value) {
            if (string.IsNullOrEmpty(Value)) {
                throw new ArgumentNullException("Value cannot be null or empty!");
            }

            string[] parts = Value.Split(';');

            foreach (string part in parts) {
                string[] nameValue = part.Split('=');

                if (nameValue.Length != 2) {
                    throw new ArgumentException("Invalid cookie part: " + part);
                }

                string name = nameValue[0].Trim();
                string value = nameValue[1].Trim();

                switch (name.ToLower()) { 
                    case "expires":
                        this.Expires = DateTime.Parse(value);
                        break;
                    case "path":
                        this.Path = value;
                        break;
                    default:
                        this.Name = name;
                        this.Value = HttpUtility.UrlDecode(value);
                        break;
                }
            }
        }

        public void Delete() {
            this.Value = string.Empty;
        }

        public string GetFullValueString() {

            string path = this.Path ?? "/";
            string expires = string.Empty;
            string value = HttpUtility.UrlEncode(this.Value);

            if (this.Expires.HasValue) {
                expires = "; expires=" + this.Expires.Value.ToUniversalTime().ToString("r");
            }

            return string.Format("{0}{1}; path={2}", value, expires, path);
        }

        public override string ToString() {

            return string.Format("{0}={1}", this.Name, this.GetFullValueString());
        }
    }
}
