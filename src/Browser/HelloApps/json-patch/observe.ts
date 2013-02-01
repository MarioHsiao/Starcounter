

module JsonPatch {

    /// <summary>
    /// Subscribe to any changes made to the viewmodel using Ecmascript 6 Object.observe.
    /// </summary>
    export function Observe(viewmodel: Object) {
        if ((<any>Object).observe) {
            (<any>Object).observe(viewmodel, () => { console.log("observing"); });
            for (var key in viewmodel) {
                if (viewmodel.hasOwnProperty(key)) { // && key.substr(0,9) !== "__sc0537_") {
                    var v: any = viewmodel[key];
                    if (v && typeof (v) === "object") {
                        console.log("Observing key " + key);
                        Observe(v);
                    }
                }
            }
        }
    };
}