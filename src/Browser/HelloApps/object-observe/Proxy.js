var AngularRemote;
(function (AngularRemote) {
    function Wrap(original) {
        if(original === null) {
            return original;
        }
        if(typeof original != "object") {
            return original;
        }
        if(original instanceof Array) {
            var arr = original;
            for(var i = 0; i < arr.length; i++) {
                console.log("arr[" + i + "]=" + ((arr[i])).FirstName);
                arr[i] = Wrap(arr[i]);
            }
            return original;
        }
        var aclass = function () {
        };
        aclass.prototype.hasOwnProperty = function (prop) {
            return typeof (this[prop]) != "undefined";
        };
        var p = new aclass();
        var keys = Object.keys(original);
        var count = keys.length;
        p.Values = new Array(count);
        for(var t = 0; t < count; t++) {
            var prop = keys[t];
            var str = "this.Values[" + t + "]";
            Object.defineProperty(aclass.prototype, prop, eval("({get:function(){return " + str + ";},set:function(v){" + str + "=v;},enumerable:true})"));
            p.Values[t] = Wrap(original[prop]);
        }
        return p;
    }
    AngularRemote.Wrap = Wrap;
    ; ;
})(AngularRemote || (AngularRemote = {}));
