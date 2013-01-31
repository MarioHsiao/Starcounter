var JsonPatch;
(function (JsonPatch) {
    var JsonPointer = (function () {
        function JsonPointer(path) {
            if(path) {
                var keys = path.split('/');
                keys.shift();
                for(var t = 0; t < keys.length; t++) {
                    keys[t] = decodeURIComponent(keys[t]);
                }
                this.Key = keys.pop();
                this.ObjectPath = keys;
            }
        }
        JsonPointer.prototype.Find = function (object) {
            var steps = this.ObjectPath;
            for(var t = 0; t < steps.length; t++) {
                var key = steps[t];
                if(object instanceof Array) {
                    object = object[parseInt(key, 10)];
                } else {
                    object = object[key];
                }
            }
            return object;
        };
        JsonPointer.prototype.GetPath = function () {
            var str = "/";
            for(var t = 0; t < this.ObjectPath.length; t++) {
                str += this.ObjectPath[t] + "/";
            }
            return str + this.Key;
        };
        JsonPointer.AttachPointers = function AttachPointers(object, parent, key) {
            if(object === null || typeof object != "object") {
                return;
            }
            if(object instanceof Array) {
                var arr = object;
                for(var i = 0; i < arr.length; i++) {
                    JsonPointer.AttachPointers(arr[i], object, i);
                }
            } else {
                for(var prop in object) {
                    if(object.hasOwnProperty(prop)) {
                        JsonPointer.AttachPointers(object[prop], object, prop);
                    }
                }
            }
            object.__sc0537_parent = parent;
            object.__sc0537_key = key;
        }
        JsonPointer.Create = function Create(object, key) {
            var p = new JsonPointer();
            var path = [];
            while(object && object.__sc0537_key != undefined) {
                path.splice(0, 0, object.__sc0537_key);
                object = object.__sc0537_parent;
            }
            p.ObjectPath = path;
            p.Key = key;
            return p;
        }
        return JsonPointer;
    })();
    JsonPatch.JsonPointer = JsonPointer;    
})(JsonPatch || (JsonPatch = {}));
