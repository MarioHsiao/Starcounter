var JsonPatch;
(function (JsonPatch) {
    var PatchProcessor = (function () {
        function PatchProcessor() {
            console.log("Creating patch processor");
        }
        PatchProcessor.prototype.replace = function (patch, target) {
            var jp = new JsonPatch.JsonPointer(patch.path);
            var obj = jp.Find(target);
            obj[jp.Key] = patch.value;
        };
        PatchProcessor.prototype.add = function (patch, target) {
            throw "add";
        };
        PatchProcessor.prototype.remove = function (patch, target) {
            throw "remove";
        };
        PatchProcessor.prototype.test = function (patch, target) {
            throw "test";
        };
        PatchProcessor.prototype.copy = function (patch, target) {
            throw "copy";
        };
        PatchProcessor.prototype.move = function (patch, target) {
            throw "move";
        };
        PatchProcessor.prototype.Find = function (path, obj) {
            var steps;
            if((steps = path.split('/')).shift() != '') {
                throw "Invalid JSON patch path";
            }
            for(var t = 0; t < steps.length; t++) {
                steps[t] = decodeURIComponent(steps[t]);
            }
            var lastKey = steps.pop();
            for(var t = 0; t < steps.length; t++) {
                var key = steps[t];
                if(obj instanceof Array) {
                    obj = obj[parseInt(key, 10)];
                } else {
                    obj = obj[key];
                }
            }
            return obj[lastKey];
        };
        return PatchProcessor;
    })();
    JsonPatch.PatchProcessor = PatchProcessor;    
})(JsonPatch || (JsonPatch = {}));
