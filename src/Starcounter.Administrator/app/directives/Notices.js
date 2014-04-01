/**
 * ----------------------------------------------------------------------------
 * Notices Directive
 * ----------------------------------------------------------------------------
 */
adminModule.directive("notices", ['NoticeFactory', function (NoticeFactory) {
    return {
        restrict: "E",
        scope: {},
        template: "<div style='margin-left:15px;margin-right:15px' title='Click to close' ng-class='{\"alert-{{notice.type}}\":true}' ng-repeat='notice in notices' ng-click='clicked(notice)' class='alert alert-dismissable'>  <button type='button' class='close' data-dismiss='alert' aria-hidden='true'>&times;</button>{{notice.msg}}<div data-ng-hide='notice.helpLink == null'><p>Help page: <a class='alert-link' href='{{notice.helpLink}}' target='_blank'>{{notice.helpLink}}</a></p></div></div>",
        link: function (scope) {
            scope.notices = NoticeFactory.notises;
            scope.closeNotice = function (notice) {
                NoticeFactory.CloseNotice(notice);
            }

            scope.clicked = function (notice) {
                NoticeFactory.CloseNotice(notice);
            }
        }
    }
}]);
