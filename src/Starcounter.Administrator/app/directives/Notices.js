/**
 * ----------------------------------------------------------------------------
 * Notices Directive
 * ----------------------------------------------------------------------------
 */
adminModule.directive("notices", ['NoticeFactory', function (NoticeFactory) {
    return {
        restrict: "E",
        scope: {},
        template: "<div style='margin-left:15px;margin-right:15px' title='Click to close' ng-class='{\"alert-{{notice.type}}\":true}' ng-repeat='notice in notices' class='alert'>  <button type='button' class='close' ng-click='closeClicked(notice)' aria-hidden='true'>&times;</button>{{notice.msg}}<div data-ng-hide='notice.helpLink == null'><p>Help page: <a class='alert-link' href='{{notice.helpLink}}' target='_blank'>{{notice.helpLink}}</a></p></div></div>",
        link: function (scope) {
            scope.notices = NoticeFactory.notises;
            scope.closeNotice = function (notice) {
                NoticeFactory.CloseNotice(notice);
            }
            scope.closeClicked = function( notice) {
                NoticeFactory.CloseNotice(notice);   
            }
        }
    }
}]);
