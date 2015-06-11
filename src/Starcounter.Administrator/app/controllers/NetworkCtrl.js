/**
 * ----------------------------------------------------------------------------
 * Network page Controller
 * ----------------------------------------------------------------------------
 */
adminModule.controller('NetworkCtrl', ['$scope', 'NetworkService', 'UserMessageFactory', function ($scope, NetworkService, UserMessageFactory) {

    // Network statistics as text
    $scope.model = NetworkService.model;

    /**
     * Refresh Network Statistics
     */
    $scope.refreshNetworkStatistics = function () {

        NetworkService.refreshNetworkStatistics(function () { },
            function (messageObject) {
                // Error
                UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);
            });
    }

    /**
     * Refresh Network Statistics
     */
    $scope.btnRefresh = function () {
        $scope.refreshNetworkStatistics();
    }

    /**
     * Navigate to database
     * @param {object} database Database
     */
    $scope.gotoDatabase = function (database) {
        $location.path("/databases/" + database.ID);
    }

    // Init
    $scope.refreshNetworkStatistics();
}]);