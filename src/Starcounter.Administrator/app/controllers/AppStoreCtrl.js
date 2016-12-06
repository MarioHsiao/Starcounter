/**
 * ----------------------------------------------------------------------------
 * AppStore page Controller
 * ----------------------------------------------------------------------------
 */
var appCtrl = adminModule.controller('AppStoreCtrl', ['$scope', '$routeParams', 'HostModelService', 'ServerService', function ($scope, $routeParams, HostModelService, ServerService) {

    $scope.database = null;

    $scope.settings = {
        showCompatibleVersions: true
    };

    $scope.serverModel = ServerService.model;

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
	
	$scope.defaultVisibleStores = ["Developer Samples", "Essentials", "polyjuice", "smörgåsbord", "starcounter"];
	
	window.addVisibleStore = function() {
		$scope.defaultVisibleStores.push.apply($scope.defaultVisibleStores, arguments);
		$scope.$apply();
	};

    // Set Data
    $scope.database = HostModelService.getDatabase($routeParams.name);
    $scope.database.RefreshAppStoreStores$++;

    $scope.$watch('settings.showCompatibleVersions', function (newValue, oldValue) {
        // Save user state
        localStorage.setItem('showCompatibleVersions', newValue);
    });

    if (localStorage.getItem('showCompatibleVersions') != null) {
        $scope.settings.showCompatibleVersions = localStorage.getItem('showCompatibleVersions') === 'true';
    }

}]);

