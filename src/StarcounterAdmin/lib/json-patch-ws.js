
window.Starcounter = {};

angular.module('StarcounterLib',[])
    .directive('mockup', function() {
        return {
            restrict: 'A',
            link:
                function(scope,elem,attr)
                {
                	console.log("Has set mockup to " + attr.mockup);
                    scope.__AppMockup = attr.mockup;
                }
        }
    })
    .directive('appClass', function() {
        return {
            restrict: 'A',
            link:
                function(scope,elem,attr)
                {
                    scope.__AppClass = attr.appLive;
                }
        }
    })
    .directive('app', function() {
    	return {
    		restrict: 'A',
    		link: function( scope, element, attrs ) {
    			if (!this.func) {
    				this.func = eval( "(function( scope, element, attrs ) { element.html( scope." +
    									attrs.app +
    									".__vc  );})")
    			}
    			return this.func.call(null,scope,element,attrs);
    		}
    		
    	}
    }).
    directive('app2', function() {

	      return {
    restrict: 'ECA',
    terminal: true,
    compile: function(element, attr) {
      var srcExp = attr.app2,
          autoScrollExp = attr.autoscroll;

      return function(scope, element ) {
        var changeCounter = 0,
            childScope;
            
        var $compile = Starcounter.AngularInjector.get('$compile');

        var clearContent = function() {
          if (childScope) {
            childScope.$destroy();
            childScope = null;
          }

          element.html('');
        };

        //scope.$watch(srcExp, function(src) {
        //  var thisChangeId = ++changeCounter;

          if (srcExp) {
//            $http.get(src, {cache: $templateCache}).success(function(response) {
        //      if (thisChangeId !== changeCounter) return;

              if (childScope) childScope.$destroy();
              childScope = scope.$new();

              element.html("<ul><li>Advanced stuff</li></ul>");
              $compile(element.contents())(childScope);

             // if (isDefined(autoScrollExp) && (!autoScrollExp || scope.$eval(autoScrollExp))) {
             //   $anchorScroll();
             // }

              childScope.$emit('$includeContentLoaded');
//              scope.$eval(onloadExp);
            }
           else clearContent();
        //});
      };
    }
  };

	    
    });


Starcounter.sample = function(str) {
//    Object.prototype.Class = function(className)
//    {
//        return this;
//    }
//    Object.prototype.Uri = function(uri)
//    {
//        return this;
    //    }
    event = function () { console.log("Hello!!!!") };
    return eval( "(" + str + ")" );
};

Starcounter.copyProperties = function(from,to) {
    for (var key in from) {
      if (from.hasOwnProperty(key)) {
         // console.log( key );
          to[key] = from[key];
      }
    }
};

(function () {

    /* Polyfill for btoa and atob */ (function () { var a = typeof window != "undefined" ? window : exports, b = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/=", c = function () { try { document.createElement("$") } catch (a) { return a } }(); a.btoa || (a.btoa = function (a) { for (var d, e, f = 0, g = b, h = ""; a.charAt(f | 0) || (g = "=", f % 1) ; h += g.charAt(63 & d >> 8 - f % 1 * 8)) { e = a.charCodeAt(f += .75); if (e > 255) throw c; d = d << 8 | e } return h }), a.atob || (a.atob = function (a) { a = a.replace(/=+$/, ""); if (a.length % 4 == 1) throw c; for (var d = 0, e, f, g = 0, h = ""; f = a.charAt(g++) ; ~f && (e = d % 4 ? e * 64 + f : f, d++ % 4) ? h += String.fromCharCode(255 & e >> (-2 * d & 6)) : 0) f = b.indexOf(f); return h }) })();

    var appElement = document;
    var doc = angular.element(appElement);
    //   console.log(appElement);

    var utf8_to_b64 = function (str) {
        return window.btoa(unescape(encodeURIComponent(str)));
    };

    var b64_to_utf8 = function (str) {
        return decodeURIComponent(escape(window.atob(str)));
    };
    //    console.log(document.cookie);
    var delete_cookie = function (cookie_name) {
        var cookie_date = new Date();  // current date & time
        cookie_date.setTime(cookie_date.getTime() - 1);
        document.cookie = cookie_name += "=; expires=" + cookie_date.toGMTString();
    };
    var get_cookie = function (cookie_name) {
        var results = document.cookie.match('(^|;) ?' + cookie_name + '=([^;]*)(;|$)');

        if (results)
            return (unescape(results[2]));
        else
            return null;
    };
//    var vm = get_cookie("vm");

    angular.element(document).ready(function () {
       var injector = angular.injector(["ng", "StarcounterLib"]);
       window.Starcounter.AngularInjector = injector;
       var rootScope = injector.get('$rootScope');
       var compile = injector.get('$compile');
       var q = injector.get("$q");
       doc.data('$injector', injector);
//       rootScope.$apply(function () { compile(doc)(rootScope); });

       if (window.__elim_req == undefined) {
          var xhrobj = new XMLHttpRequest();
          console.log( "Mockup is set to " + rootScope.__AppMockup);
          xhrobj.open('GET', rootScope.__AppMockup);
	
          xhrobj.onreadystatechange = function () {
             if (xhrobj.readyState == 4) {
                var appJs = xhrobj.responseText;
                console.log("Mockup Json=" + appJs);
                var js = Starcounter.sample(appJs);
                Starcounter.copyProperties(js, rootScope);
             }
             rootScope.$apply( function() { compile(doc)(rootScope); } );
          }
          xhrobj.send(null);
       }
       else {
//          if ( rootScope.__AppMockup ) {
             //                vm = b64_to_utf8(vm);
             //                delete_cookie("vm");
             console.log("veem=" + __elim_req);

             var js = __elim_req; //Starcounter.sample(vm);
             Starcounter.copyProperties(js, rootScope);
             rootScope.$apply( function() { compile(doc)(rootScope); } );
//          }
//          else {
//             console.log("No mockup JSON is defined. Use the app-mockup attribute.");
//          }
       }
    });
})();







    // Create a user-defined object.
    // To instead use an existing DOM object, uncomment the line below.
    var obj = Object.prototype;
// var obj = window.document;

// Add an accessor property to the object.
Object.defineProperty(obj, "$userId", {
    set: function (x) {
        console.log("in property set accessor");
        this.newaccpropvalue = x;
    },
    get: function () {
        console.log("in property get accessor");
        return this.newaccpropvalue;
    },
    enumerable: true,
    configurable: true
});
// Set the property value.

var x = {};
x.$userId = "test";
console.log("Property value: " + x.$userId);



