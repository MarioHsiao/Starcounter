// Generic Json Pointer Implementation
// See http://tools.ietf.org/html/draft-ietf-appsawg-json-pointer-04.
// Author: Joachim Wester


module JsonPatch {

    /// <summary>
    /// Represents a Json Pointer addressing a single element 
    /// in a Json tree.
    ///
    /// Json pointer is defined here:
    /// http://tools.ietf.org/html/draft-ietf-appsawg-json-pointer-04.
    /// </summary>
    export class JsonPointer {

        /// <summary>
        /// The path string to the containing object to the value pointed to by this JsonPointer.
        /// </summary>
        public ObjectPath: string[];

        /// <summary>
        /// The name of the property with the value pointed to by this JsonPointer
        /// </summary>
        public Key: string;

        /// <summary>
        /// Creates a new pointer using a Json Patch formatted path string
        /// </summary>
        constructor (path?: string) {
            if (path) {
                var keys = path.split('/');
                keys.shift(); // path starts with '/' such that first element is empty
                for (var t = 0 ; t < keys.length; t++)
                    keys[t] = decodeURIComponent(keys[t]);
                this.Key = keys.pop();
                this.ObjectPath = keys;
            }
        }

        /// <summary>
        /// Returns the object pointed to by the 'Path' array.
        /// This object should contain the property specified
        /// in the 'Key' property. I.e. to get the value pointed to
        /// by this Json pointer, you should perform the following code:
        /// var obj = myPointer.Find( root );
        /// var value = obj[myPointer.Key];
        /// </summary>
        Find(object: any): any {
            var steps = this.ObjectPath;
            for (var t = 0 ; t < steps.length ; t++) {
                var key: string = steps[t];
                if (object instanceof Array)
                    object = object[parseInt(key, 10)];
                else
                    object = object[key];
            }
            return object;
        }

        /// <summary>
        /// Returns this pointer as a path string according to the Json Patch standard.
        /// </summary>
        GetPath() : string {
            var str = "/";
            for (var t = 0; t < this.ObjectPath.length; t++) {
                str += this.ObjectPath[t] + "/";
            }
            return str + this.Key;
        }

        /// <summary>
        /// Notes parent objects and the property key in the parent.
        /// This makes it possible to quickly find the Json pointer of
        /// any object in a json tree.
        /// </summary>
        static AttachPointers(object: any, parent?: any, key?: any) : void {

            if (object === null || typeof object != "object")
                return;

            if (object instanceof Array) { // if (toString.apply(a) == '[object Array]') {
                var arr: Array = object;
                for (var i = 0; i < arr.length ; i++)
                    AttachPointers(arr[i], object, i);
            }
            else {
                for (var prop in object) {
                    if (object.hasOwnProperty(prop))
                        AttachPointers(object[prop], object, prop);
                }
            }   
            object.__sc0537_parent = parent;  // Should use ES6 symbol when ES6 is widely available
            object.__sc0537_key = key;        // Should use ES6 symbol when ES6 is widely available
        }

        /// <summary>
        /// Given that the object tree has been amended using the
        /// AttachPointers function, this method create a JsonPointer
        /// that identifies an individual property.
        /// </summary>
        static Create(object: any,key:any) : JsonPointer {
            var p = new JsonPointer();
            var path = [];
            while (object && object.__sc0537_key != undefined ) {
                path.splice(0,0,object.__sc0537_key); // Should use ES6 symbol when ES6 is widely available
                object = object.__sc0537_parent; // Should use ES6 symbol when ES6 is widely available
            }
            p.ObjectPath = path;
            p.Key = key;
            return p;
        }
    }
}