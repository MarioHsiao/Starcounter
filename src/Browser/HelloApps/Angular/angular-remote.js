var AngularRemote;
(function (AngularRemote) {
    var Patcher = new JsonPatch.PatchProcessor();
    function Patch(patches, viewmodel) {
        var cnt = patches.length;
        for(var t = 0; t < cnt; t++) {
            var p = patches[t];
            Patcher[p.op](p, viewmodel);
        }
    }
    AngularRemote.Patch = Patch;
    function CopyProperties(from, to) {
        for(var key in from) {
            to[key] = from[key];
        }
    }
    AngularRemote.CopyProperties = CopyProperties;
    ; ;
    function isDefined(value) {
        return typeof value != 'undefined';
    }
    function Bootstrap() {
        var partialDirective = function ($http, $templateCache, $anchorScroll, $compile) {
            return {
                restrict: 'ECA',
                terminal: true,
                compile: function (element, attr) {
                    var srcExp = attr.ngInclude || attr.src, onloadExp = attr.onload || '', autoScrollExp = attr.autoscroll;
                    return function (scope, element) {
                        var changeCounter = 0, childScope;
                        var clearContent = function () {
                            if(childScope) {
                                childScope.$destroy();
                                childScope = null;
                            }
                            element.html('');
                        };
                        scope.$watch(srcExp, function ngIncludeWatchAction(src) {
                            var thisChangeId = ++changeCounter;
                            if(src) {
                                if(thisChangeId !== changeCounter) {
                                    return;
                                }
                                if(childScope) {
                                    childScope.$destroy();
                                }
                                childScope = scope.$new();
                                element.html(src);
                                $compile(element.contents())(childScope);
                                if(isDefined(autoScrollExp) && (!autoScrollExp || scope.$eval(autoScrollExp))) {
                                    $anchorScroll();
                                }
                            } else {
                                clearContent();
                            }
                        });
                    }
                }
            };
        };
        (partialDirective).$inject = [
            '$http', 
            '$templateCache', 
            '$anchorScroll', 
            '$compile'
        ];
        var ang = angular.module('AngularRemote', []);
        ang.directive('partial', partialDirective).directive('appMockup', function () {
            return {
                restrict: 'A',
                compile: function () {
                    return {
                        pre: function (scope, element, attrs) {
                            AngularRemote.LoadMockup(attrs.appMockup, scope);
                        }
                    };
                }
            };
        }).directive('appMockupPatch', function () {
            return {
                restrict: 'A',
                link: function (scope, elem, attr) {
                    scope.__AppMockupPatch = attr.appMockupPatch;
                }
            };
        }).directive('app', function () {
            return {
                restrict: 'A',
                link: function (scope, element, attrs) {
                    if(!this.func) {
                        this.func = eval("(function( scope, element, attrs ) { if ( scope." + attrs.app + " != undefined ) element.html( scope." + attrs.app + ".__vc  ); else element.html(\"No content in " + attrs.app + "\"); })");
                    }
                    console.log(this.func);
                    return this.func.call(null, scope, element, attrs);
                }
            };
        }).directive('app2', function () {
            return {
                restrict: 'ECA',
                terminal: true,
                compile: function (element, attr) {
                    var srcExp = attr.app2, autoScrollExp = attr.autoscroll;
                    return function (scope, element) {
                        var changeCounter = 0, childScope;
                        var $compile = (window).JockeTemp;
                        var clearContent = function () {
                            if(childScope) {
                                childScope.$destroy();
                                childScope = null;
                            }
                            element.html('');
                        };
                        if(srcExp) {
                            if(childScope) {
                                childScope.$destroy();
                            }
                            childScope = scope.$new();
                            element.html("<ul><li>{{Test}}</li></ul>");
                            $compile(element.contents())(childScope);
                            childScope.$emit('$includeContentLoaded');
                        } else {
                            clearContent();
                        }
                    }
                }
            };
        });
        if(!AngularRemote.HasNativeObserver()) {
            ang.provider('$parse', function () {
                var cache = {
                };
                ((this.$get)) = [
                    '$filter', 
                    '$sniffer', 
                    function ($filter, $sniffer) {
                        return function (exp) {
                            switch(typeof exp) {
                                case 'string': {
                                    return cache.hasOwnProperty(exp) ? cache[exp] : cache[exp] = AngularRemote.parser2(exp, false, $filter, $sniffer.csp);

                                }
                                case 'function': {
                                    return exp;

                                }
                                default: {
                                    return angular.noop;

                                }
                            }
                        }
                    }                ];
            });
        }
    }
    AngularRemote.Bootstrap = Bootstrap;
})(AngularRemote || (AngularRemote = {}));
AngularRemote.Bootstrap();
