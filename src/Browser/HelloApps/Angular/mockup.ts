///<reference path="../json-patch/observe.ts"/>

declare var __elim_req: string;

module AngularRemote {

    /// <summary>
    /// Loads a view model from a json resource (file).
    /// </summary>
    export function LoadMockup( mockup : string, rootScope : any ) {

            var doc = angular.element(document);

            if (true) {     
                console.log("Requesting " + mockup );

                var xhrobj = new XMLHttpRequest();
                xhrobj.open('GET', mockup );

                xhrobj.onreadystatechange = function () {
                    if (xhrobj.readyState == 4) {
                        if (xhrobj.status == 200) {
                            var appJs = xhrobj.responseText;
                            //LogIncomming(appJs);
                            var js = AngularRemote.EvalToProxy(appJs);
                            AngularRemote.CopyProperties(js, rootScope);
                            rootScope.$digest();
                            // ----- 

                            xhrobj = new XMLHttpRequest();
                            xhrobj.open('GET', rootScope.__AppMockupPatch);

                            xhrobj.onreadystatechange = function () {
                                if (xhrobj.readyState == 4) {
                                    if (xhrobj.status == 200) {
                                        var appJs = xhrobj.responseText;
                                        LogIncomming(appJs);
                                        var x: any[] = eval("(" + appJs + ")");
                                        AngularRemote.Patch(x, rootScope);
                                        rootScope.$digest();
                                    }
                                }
                            }

                            xhrobj.send(null);
                        }
                    }



                }

                xhrobj.send(null);


            }
            else {

                //                vm = b64_to_utf8(vm);
                //                delete_cookie("vm");
                console.log("veem=" + JSON.stringify(__elim_req));

                var js = __elim_req; //Starcounter.sample(vm);
                AngularRemote.CopyProperties(js, rootScope);
                rootScope.$apply();


            }

    }

    
    export function EvalToProxy(str: string) {
        var obj = eval("(" + str + ")");
        JsonPatch.Observe(obj);
        JsonPatch.JsonPointer.AttachPointers(obj);
        return obj;
    }

    export function LogIncomming( str: string) {
        var e = document.getElementById("Incomming");
        if (e)
            e.innerHTML = e.innerHTML + "\r\n" + str;
    }

    export function LogOutgoing( str: string) {
        var e = document.getElementById("Outgoing");
        if (e)
            e.innerHTML = e.innerHTML + "\r\n" + str;
    }
}


