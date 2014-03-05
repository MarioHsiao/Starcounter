/**
 * ----------------------------------------------------------------------------
 * Console Window Directive
 * ----------------------------------------------------------------------------
 */
adminModule.directive("consolewindow", function () {
    return {
        scope: {
            ngModel: '=',
            height: '@heightAttr',
            width: '@widthAttr'
    },
        restrict: "E",
        template: "<div class='console' style='height:{{height}}; width:{{width}}; overflow: scroll; background-color: #000000' ng-bind-html=ngModel></div>",
        link: function (scope, elem, attrs) {
            scope.$watch('ngModel', function (newValue, oldValue) {

                var element = elem.find('div');
                element.scrollTop(element[0].scrollHeight);
                //$("#consolewin").scrollTop($("#consolewin")[0].scrollHeight);
            });
        }
    }

});
