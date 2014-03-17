/**
 * ----------------------------------------------------------------------------
 * Job Directive
 * ----------------------------------------------------------------------------
 */
adminModule.directive("jobs", ['JobFactory', function (JobFactory) {
    return {
        restrict: "E",
        scope: {},
        template: "<div style='margin-left:15px;margin-right:15px' ng-repeat='job in jobs' class='progress progress-striped active'><div class='progress-bar' role='progressbar' aria-valuenow='100' aria-valuemin='0' aria-valuemax='100' style='width: 100%'>{{job.message}}</div></div>",
        link: function (scope) {
            scope.jobs = JobFactory.jobs;
        }
    }

}]);
