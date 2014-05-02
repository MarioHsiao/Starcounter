/**
 * ----------------------------------------------------------------------------
 * Databases Settings page Controller
 * ----------------------------------------------------------------------------
 */
adminModule.controller('DatabaseSettingsCtrl', ['$scope', '$log', '$location', '$routeParams', '$anchorScroll', 'NoticeFactory', 'DatabaseService', 'UserMessageFactory', function ($scope, $log, $location, $routeParams, $anchorScroll, NoticeFactory, DatabaseService, UserMessageFactory) {


    // Model
    $scope.model = {
        database: null,
        settings: null
    }


    /**
     * Refresh database settings
     */
    $scope.refreshSettings = function () {

        DatabaseService.getSettings($scope.model.database, function (settings) {
            // Success
            $scope.model.settings = settings;
            $scope.myForm.$setPristine(); // This dosent work, the <select> breaks the pristine state :-(

        },
            function (messageObject) {
                // Error

                if (messageObject.isError) {
                    UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);
                }
                else {
                    NoticeFactory.ShowNotice({ type: 'danger', msg: messageObject.message, helpLink: messageObject.helpLink });
                }

            });
    }


    /**
     * Save settings
     * @param {object} database Database
     * @param {object} settings Settings
     */
    $scope.btnSaveSettings = function (database, settings) {

        DatabaseService.saveSettings(database, settings, function (settings) {

            // Success

            // TODO: Ask user if he wants to restart database?

            NoticeFactory.ShowNotice({ type: "success", msg: "Settings saved. The new settings will be used at the next start of the database" });

            $scope.myForm.$setPristine();

            // Navigate to database list if user has not navigated to another page
            if ($location.path() == "/databases/" + database.name + "/settings") {
                $location.path("/databases");
            }


        }, function (messageObject, validationErrors) {

            // Error

            if (validationErrors != null && validationErrors.length > 0) {
                // Validation errors

                // Show errors on screen
                for (var i = 0; i < validationErrors.length; i++) {
                    //$scope.alerts.push({ type: 'danger', msg: validationErrors[i].message });

                    if ($scope.myForm[validationErrors[i].PropertyName] == undefined) {
                        NoticeFactory.ShowNotice({ type: 'danger', msg: "Missing or invalid property: " + validationErrors[i].PropertyName });
                    } else {

                        $scope.myForm[validationErrors[i].PropertyName].$setValidity("validationError", false);
                        var id = validationErrors[i].PropertyName;
                        var unregister = $scope.$watch("settings." + validationErrors[i].PropertyName, function (newValue, oldValue) {
                            if (newValue == oldValue) return;
                            $scope.myForm[id].$setValidity("validationError", true);
                            unregister();
                        }, false);
                    }
                }

            }
            else {

                if (messageObject.isError) {

                    //var message = messageObject.message.replace(/\r\n/g, "<br>");

                    UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);
                }
                else {
                    NoticeFactory.ShowNotice({ type: 'danger', msg: messageObject.message, helpLink: messageObject.helpLink });
                }
            }

            if (typeof (errorCallback) == "function") {
                errorCallback(messageObject);
            }

        });

    }


    /**
     * Reset server settings
     */
    $scope.btnResetSettings = function () {
        $scope.refreshSettings();
    }


    // Init
    // Refresh databases list
    DatabaseService.refreshDatabases(

        function () {
            // Success
            var database = DatabaseService.getDatabase($routeParams.name);
            if (database != null) {
                $scope.model.database = database;
                $scope.refreshSettings();
            }
            else {

                var title = "Missing database";
                var message = "Failed to retrieve the database " + $routeParams.name;
                var buttons = [{ result: 0, label: 'Ok', cssClass: 'btn btn-primary' }];

                UserMessageFactory.showMessageBox(title, message, buttons);
            }

        },
        function (messageObject) {
            // Error
            UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);
        });
}]);