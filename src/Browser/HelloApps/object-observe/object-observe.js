"use strict";
var AngularRemote;
(function (AngularRemote) {
    function HasNativeObserver() {
        if(Object.observe) {
            console.log("-- Native Object.observe detected!");
            return true;
        }
        console.log("-- Native Object.observe is not found");
        return false;
    }
    AngularRemote.HasNativeObserver = HasNativeObserver;
    var ChangeRecord = (function () {
        function ChangeRecord(type, object, name, oldValue) {
            this.type = type;
            this.object = object;
            this.name = name;
            this.oldValue = oldValue;
            AngularRemote.LogOutgoing(type + " " + JsonPatch.JsonPointer.Create(object, name).GetPath() + " value=" + object[name] + " (" + oldValue + ")");
        }
        return ChangeRecord;
    })();
    AngularRemote.ChangeRecord = ChangeRecord;    
})(AngularRemote || (AngularRemote = {}));
