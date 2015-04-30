/**
 * ----------------------------------------------------------------------------
 * AppStore page Controller
 * ----------------------------------------------------------------------------
 */
var appCtrl = adminModule.controller('AppStoreCtrl', ['$scope', '$routeParams', 'HostModelService', function ($scope, $routeParams, HostModelService) {

    $scope.database = null;

    /**
     * Download Application
     * @param {object} application Application
     */
    $scope.btnDownloadApplication = function (application) {

        application.Download$++;
    }

    /**
     * Open Application
     * @param {object} application Application
     */
    $scope.btnOpenApplication = function (application) {

        application.Open$++;
    }

    // Set Data
    $scope.database = HostModelService.getDatabase($routeParams.name);
}]);

