var AngularRemote;
(function (AngularRemote) {
    function LoadMockup(mockup, rootScope) {
        var doc = angular.element(document);
        if(true) {
            console.log("Requesting " + mockup);
            var xhrobj = new XMLHttpRequest();
            xhrobj.open('GET', mockup);
            xhrobj.onreadystatechange = function () {
                if(xhrobj.readyState == 4) {
                    if(xhrobj.status == 200) {
                        var appJs = xhrobj.responseText;
                        var js = AngularRemote.EvalToProxy(appJs);
                        AngularRemote.CopyProperties(js, rootScope);
                        rootScope.$digest();
                        xhrobj = new XMLHttpRequest();
                        xhrobj.open('GET', rootScope.__AppMockupPatch);
                        xhrobj.onreadystatechange = function () {
                            if(xhrobj.readyState == 4) {
                                if(xhrobj.status == 200) {
                                    var appJs = xhrobj.responseText;
                                    LogIncomming(appJs);
                                    var x = eval("(" + appJs + ")");
                                    AngularRemote.Patch(x, rootScope);
                                    rootScope.$digest();
                                }
                            }
                        };
                        xhrobj.send(null);
                    }
                }
            };
            xhrobj.send(null);
        } else {
            console.log("veem=" + JSON.stringify(__elim_req));
            var js = __elim_req;
            AngularRemote.CopyProperties(js, rootScope);
            rootScope.$apply();
        }
    }
    AngularRemote.LoadMockup = LoadMockup;
    function EvalToProxy(str) {
        var obj = eval("(" + str + ")");
        JsonPatch.Observe(obj);
        JsonPatch.JsonPointer.AttachPointers(obj);
        return obj;
    }
    AngularRemote.EvalToProxy = EvalToProxy;
    function LogIncomming(str) {
        var e = document.getElementById("Incomming");
        if(e) {
            e.innerHTML = e.innerHTML + "\r\n" + str;
        }
    }
    AngularRemote.LogIncomming = LogIncomming;
    function LogOutgoing(str) {
        var e = document.getElementById("Outgoing");
        if(e) {
            e.innerHTML = e.innerHTML + "\r\n" + str;
        }
    }
    AngularRemote.LogOutgoing = LogOutgoing;
})(AngularRemote || (AngularRemote = {}));
