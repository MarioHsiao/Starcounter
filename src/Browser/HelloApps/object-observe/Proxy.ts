

module AngularRemote {

    // Creates an object that emulates the supplied object.
    // Whenever the wrapped object is updates, any registred
    // observers are notified according to
    // http://wiki.ecmascript.org/doku.php?id=harmony:observe
    export function Wrap(original : any) {

        if (original===null)
            return original;

        if ( typeof original != "object") // In javascript, arrays and objects return 'object' [sic]
            return original;

        if (original instanceof Array) { // if (toString.apply(a) == '[object Array]') {
            var arr: Array = original;
            for (var i = 0; i < arr.length ; i++) {
                console.log("arr[" + i + "]=" + (<any>(arr[i])).FirstName);
                arr[i] = Wrap(arr[i]);
            }
            return original;
        }

        var aclass = function () {};
        aclass.prototype.hasOwnProperty = function (prop: string) { return typeof(this[prop]) != "undefined" }
        var p : any = new aclass;
        var keys = Object.keys(original);
        var count = keys.length;
        p.Values = new Array(count);

        for (var t = 0; t < count; t++) {
            var prop = keys[t];
            var str = "this.Values["+t+"]"; 
            Object.defineProperty(aclass.prototype, prop, 
                eval("({get:function(){return "+str+";},set:function(v){"+str+"=v;},enumerable:true})") );
            p.Values[t] = Wrap(original[prop]);
        }
        return p;
   };

}
