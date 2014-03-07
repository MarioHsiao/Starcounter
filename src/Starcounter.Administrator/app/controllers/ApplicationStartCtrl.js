/**
 * ----------------------------------------------------------------------------
 * Start Application page Controller
 * ----------------------------------------------------------------------------
 */
adminModule.controller('ApplicationStartCtrl', ['$scope', '$log', '$location', 'NoticeFactory', 'UserMessageFactory', 'DatabaseService', 'ApplicationService', function ($scope, $log, $location, NoticeFactory, UserMessageFactory, DatabaseService, ApplicationService) {


    // List of databases
    $scope.databases = DatabaseService.databases;

    // Entered or Selected file
    $scope.file = "";

    // Selected databasename
    $scope.selectedDatabaseName = null;

    // List of recent successfully started applications
    $scope.recentApplications = [];


    /**
     * Start Application
     */
    $scope.btnStart = function () {

        ApplicationService.startApplication($scope.file, $scope.selectedDatabaseName,
            function () {
                // Success

                // Remember successfully started applications
                $scope.rememberRecentFile($scope.file);

                // Navigate to Application list if user has not navigated to another page
                if ($location.path() == "/applicationStart") {
                    $location.path("/applications");
                }

            },
            function (messageObject) {
                // Error

                if (messageObject.isError) {
                    UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);
                }
                else {
                    NoticeFactory.ShowNotice({ type: 'error', msg: messageObject.message, helpLink: messageObject.helpLink });
                }

            });

    }


    /**
     * Select file
     * @param {string} file Path to file
     */
    $scope.btnSetCurrent = function (file) {
        $scope.file = file.name;
    }


    /**
     * Remember file
     * @param {string} file Path to file
     */
    $scope.rememberRecentFile = function (file) {

        var maxItems = 5;
        // Check if file is already 'rememberd'
        for (var i = 0; i < $scope.recentApplications.length ; i++) {

            // File already rememberd
            if (file == $scope.recentApplications[i].name) {
                return;
            }

        }
        $scope.recentApplications.unshift({ name: file });

        var toMany = $scope.recentApplications.length - maxItems;

        if (toMany > 0) {
            $scope.recentApplications.splice(maxItems, toMany);
        }

        localStorage.recentApplications = JSON.stringify($scope.recentApplications);
    }


    /**
     * Refresh recent remembered applications list
     */
    $scope.refreshRecentApplications = function () {
        if (typeof (Storage) !== "undefined") {
            if (localStorage.recentApplications !== "undefined") {
                $scope.recentApplications = JSON.parse(localStorage.recentApplications);
            }
        }
        else {
            // No web storage support..
        }
    }


    // Init
    DatabaseService.refreshDatabases(function () {

        if ($scope.databases.length > 0) {
            $scope.selectedDatabaseName = $scope.databases[0].name;
        }

    },
        function (messageObject) {
            // Error
            UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);
        });


    $scope.refreshRecentApplications();


}]);