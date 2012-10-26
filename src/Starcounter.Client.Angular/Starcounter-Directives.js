
/*
 * StarcounterDirectives v0.1.0
 * (c) 2012 Starcounter. http://www.starcounter.com
 */
angular.module('StarcounterDirectives', [])
 
.directive('typeahead', function() {
    return {
        restrict: 'E',
        replace: true,
        scope: {
            bindModel: '=',
            resultlist: '&'
        },
        template: '<input type="text" ng-model="bindModel"/>',
        link: function(scope, element, attrs) {
            $(element).typeahead({
                source: function(query , process) { 

                // Quick and dirty, TODO: Fix this
                var list = scope.resultlist();
                var newArray = new Array();
                for (var i = 0; i < list.length; i++) {
                    newArray[i] = list[i].Description;
                }
                return newArray;
                },
                updater: function(item) {
                    scope.$apply(read(item));
                    return item;
                }
            });

            function read(value) {
                scope.bindModel = value;
            }
        }
    };
})



