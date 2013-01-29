///<reference path="json-pointer.ts"/>


module JsonPatch {

    /// A Patch Processor applies JSON Patch changes to a JSON document.
    export class PatchProcessor {

        constructor () {
            console.log("Creating patch processor");
        }

        // A patch has a path and a value. Replaces the value at the given path in
        // the document target.
        replace(patch: any, target: any) {
            var jp = new JsonPointer(patch.path);
            var obj = jp.Find(target);
            obj[jp.Key] = patch.value;
        }

        add(patch: any, target: any) {
            throw "add";
        }

        remove(patch: any, target: any) {
            throw "remove";
        }

        test(patch: any, target: any) {
            throw "test";
        }

        copy(patch: any, target: any) {
            throw "copy";
        }

        move(patch: any, target: any) {
            throw "move";
        }
        
        /// Finds the parent object of the JSON object property pointed to by path in the JSON document
        // obj.
        Find(path: string, obj: any) : any {
            var steps: string[];
            if ((steps = path.split('/')).shift() != '')
                throw "Invalid JSON patch path";
            for (var t = 0 ; t < steps.length; t++) {
                steps[t] = decodeURIComponent(steps[t]);
            }
            var lastKey = steps.pop();

            for (var t = 0 ; t < steps.length ; t++) {
                var key: string = steps[t];
                if (obj instanceof Array)
                    obj = obj[parseInt(key, 10)];
                else
                    obj = obj[key];
            }

            return obj[lastKey];
        }
    }
}