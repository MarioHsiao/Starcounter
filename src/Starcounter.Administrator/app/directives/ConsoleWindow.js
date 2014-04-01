/**
 * ----------------------------------------------------------------------------
 * Console Window Directive
 * ----------------------------------------------------------------------------
 */
adminModule.directive("consolewindow", function () {
    return {
        scope: {
            ngModel: '='
        },
        restrict: "E",
        template: "<textarea class='console' readonly=true style='height:100%; width:100%; resize:none; color: white;background-color: #000000' >{{ngModel}}</textarea>",
        link: function (scope, elem, attrs) {
            scope.$watch('ngModel', function (newValue, oldValue) {
                var element = elem.find('textarea');
                element[0].scrollTop = element[0].scrollHeight;
            });
        }
    }

});


