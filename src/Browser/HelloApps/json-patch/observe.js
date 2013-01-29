var JsonPatch;
(function (JsonPatch) {
    function Observe(viewmodel) {
        if((Object).observe) {
            (Object).observe(viewmodel, function () {
                console.log("observing");
            });
            for(var key in viewmodel) {
                if(viewmodel.hasOwnProperty(key)) {
                    var v = viewmodel[key];
                    if(v && typeof (v) === "object") {
                        console.log("Observing key " + key);
                        Observe(v);
                    }
                }
            }
        }
    }
    JsonPatch.Observe = Observe;
    ; ;
})(JsonPatch || (JsonPatch = {}));
