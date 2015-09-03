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
     * Delete Application
     * @param {object} application Application
     */
    $scope.btnDeleteApplication = function (application) {

        application.Delete$++;
    }
    

    /**
     * Install Application
     * this will download/install and start the application
     * @param {object} application Application
     */
    $scope.btnInstallApplication = function (application) {

        application.Install$++;
    }

    /**
      * Upgrade Application
      * @param {object} application Application
      */
    $scope.btnUpgradeApplication = function (application) {

        application.Upgrade$++;
    }
    /**
     * Open Application
     * @param {object} application Application
     */
    $scope.btnOpenApplication = function (application) {

        application.Open$++;
    }

    $scope.btnRefreshAppStoreStores = function () {

        $scope.database.RefreshAppStoreStores$++;
    }

    // Set Data
    $scope.database = HostModelService.getDatabase($routeParams.name);
    $scope.database.RefreshAppStoreStores$++;
}]);

