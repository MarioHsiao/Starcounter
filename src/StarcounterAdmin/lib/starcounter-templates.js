
function Template()
{
}

Template.prototype.toString = function () { return this.value; }


function Wrap(obj) {
    if (obj instanceof Template)
        return obj;
    var a = new Template()
    if (obj instanceof String)
        a.data = obj.toString();
    else
        a.data = obj;
    a.meta = {};
    return a;
}

/*

Object.prototype.meta = function (p) {
    var w = Wrap(this);
    w.meta = p;
    return w;
}

Object.prototype.input = function (b) {
    if (b == undefined)
        b = true;
    var w = Wrap(this);
    w.meta.edit = b;
    return w;
}

Object.prototype.cargo = function (p) {
    var w = Wrap(this);
    w.cargo = p;
    return w;
}
*/

function TransformToSample(o) {
    // TODO! Needs to change o as well. Remodel!
    for (var key in o) {
        var val = o[key];
        if (val instanceof Template) {
            var meta = val.meta;
            var cargo = val.cargo;
            val = val.data;
            o[key] = val;
            o[key + "__meta"] = meta;
            o[key + "__cargo"] = cargo;
        }
        if (typeof (val) == "object") {
            TransformToSample(val);
        }
    }
}


var $event = {};

