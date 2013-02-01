///<reference path="Parser.ts" />
///<reference path="../object-observe/proxy.ts" />
///<reference path="../json-patch/patch-processor.ts"/>
///<reference path="mockup.ts"/>

declare var angular;

module AngularRemote {

    var Patcher = new JsonPatch.PatchProcessor();


    // Applies a Json Patch (http://tools.ietf.org/html/draft-ietf-appsawg-json-patch-05)
    // array supplied in 'patches' to the view-model supplied in 'viewmodel'
    export function Patch(patches: any[], viewmodel:any) {
        var cnt = patches.length;
        for (var t = 0 ; t < cnt ; t++ ) {
            var p = patches[t];
            Patcher[p.op](p,viewmodel);
        }
    }

    export function CopyProperties(from: Object, to: Object) {
        for (var key in from) {
            //   if (from.hasOwnProperty(key)) {
            to[key] = from[key];
            //   }
        }
    };

    
    function isDefined(value) { return typeof value != 'undefined'; }

    export function Bootstrap() {
        var partialDirective = function ($http, $templateCache, $anchorScroll, $compile) {
            return {
                restrict: 'ECA',
                terminal: true,
                compile: function (element, attr) {
                  var srcExp = attr.ngInclude || attr.src,
                      onloadExp = attr.onload || '',
                      autoScrollExp = attr.autoscroll;

                    return function (scope, element) {
                      var changeCounter = 0,
                          childScope;

                        var clearContent = function () {
                            if (childScope) {
                                childScope.$destroy();
                                childScope = null;
                            }

                            element.html('');
                        };

                        scope.$watch(srcExp, function ngIncludeWatchAction(src) {
                            var thisChangeId = ++changeCounter;

                            if (src) {
                             //   $http.get(src, { cache: $templateCache }).success(function (response) {
                                    if (thisChangeId !== changeCounter) return;

                                    if (childScope) childScope.$destroy();
                                    childScope = scope.$new();

                                    element.html(src);
                                    $compile(element.contents())(childScope);

                                    if (isDefined(autoScrollExp) && (!autoScrollExp || scope.$eval(autoScrollExp))) {
                                        $anchorScroll();
                                    }

                              //      childScope.$emit('$includeContentLoaded');
                              //      scope.$eval(onloadExp);
                              //  }).error(function () {
                              //      if (thisChangeId === changeCounter) clearContent();
                              //  });
                            } else clearContent();
                        });
                    };
                }
            }
        };
        (<any>partialDirective).$inject = ['$http', '$templateCache', '$anchorScroll', '$compile'];

        var ang = angular.module('AngularRemote', []);

        ang
            .directive('partial', partialDirective )

            .directive('appMockup', function () {
                return {
                    restrict: 'A',

                    compile: function () {
                        return {
                                pre: function (scope, element, attrs) {
                                    LoadMockup(attrs.appMockup, scope);
                            }
                        }
                    },


//                    link:
//                        function (scope, elem, attr) {
//                            scope.__AppMockup = attr.appMockup;
//                        }
                }
            })
            .directive('appMockupPatch', function () {
                return {
                    restrict: 'A',
                    link:
                        function (scope, elem, attr) {
                            scope.__AppMockupPatch = attr.appMockupPatch;
                        }
                }
            })
            .directive('app', function () {
                return {
                    restrict: 'A',
                    link: function (scope, element, attrs) {
                        if (!this.func) {
                            this.func = eval("(function( scope, element, attrs ) { if ( scope." + attrs.app + " != undefined ) element.html( scope." +
                                        attrs.app +
                                        ".__vc  ); else element.html(\"No content in " + attrs.app + "\"); })")
                        }
                        console.log(this.func);
                        return this.func.call(null, scope, element, attrs);
                    }
                }
            }).

            directive('app2', function () {

                return {
                    restrict: 'ECA',
                    terminal: true,
                    compile: function (element, attr) {
                      var srcExp = attr.app2,
                          autoScrollExp = attr.autoscroll;

                        return function (scope, element) {
                          var changeCounter = 0,
                              childScope;

                            //var $compile = Starcounter.AngularInjector.get('$compile');
                            var $compile = (<any>window).JockeTemp;

                            var clearContent = function () {
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

                                element.html("<ul><li>{{Test}}</li></ul>");
                                //if (scope.Page != undefined)
                                //    console.log( "Found " + scope.Page.__vc);
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


        if (!AngularRemote.HasNativeObserver()) {
            ang.provider('$parse', function () {
                var cache = {};

                (<any[]>(this.$get)) = ['$filter', '$sniffer', function ($filter, $sniffer) {
                    return function (exp) {
                        switch (typeof exp) {
                            case 'string':
                                return cache.hasOwnProperty(exp)
                                  ? cache[exp]
                                  : cache[exp] = parser2(exp, false, $filter, $sniffer.csp);
                            case 'function':
                                return exp;
                            default:
                                return angular.noop;
                        }
                    };
                }];
            });
        }
    }
}

AngularRemote.Bootstrap();

