/**
 * ----------------------------------------------------------------------------
 * Databases Settings page Controller
 * ----------------------------------------------------------------------------
 */
adminModule.controller('DatabaseSettingsCtrl', ['$scope', '$log', '$location', 'HostModelService', '$routeParams', '$anchorScroll', 'NoticeFactory', 'DatabaseService', 'UserMessageFactory', function ($scope, $log, $location, HostModelService, $routeParams, $anchorScroll, NoticeFactory, DatabaseService, UserMessageFactory) {

    var self = this;

    // Model
    $scope.model = {
        database: null,
        settings: null
    }

    $scope.HasErrorMessage = false;
    $scope.ErrorMessage = {
        Message: "",
        HelpLink: ""
    }

    $scope.SuccessMessage = null;
    $scope.WarnMessage = null;

    /**
     * Refresh database settings
     */
    $scope.refreshSettings = function () {

        $scope.SuccessMessage = null;
        $scope.WarnMessage = null;

        DatabaseService.getSettings($scope.model.database, function (settings) {
            // Success
            $scope.model.settings = settings;
            $scope.myForm.$setPristine(); // This dosent work, the <select> breaks the pristine state :-(

        },
            function (messageObject) {
                // Error

                self.HasErrorMessage = true;
                self.ErrorMessage.Message = messageObject.message;
                self.ErrorMessage.HelpLink = messageObject.helpLink;

                if (messageObject.isError) {
                    UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);
                }
            });
    }

    /**
     * Save settings
     * @param {object} database Database
     * @param {object} settings Settings
     */
    $scope.btnSaveSettings = function (database, settings) {

        $scope.SuccessMessage = null;
        $scope.WarnMessage = null;
        
        DatabaseService.saveSettings(database, settings, function (settings) {

            // Success

            // TODO: Ask user if he wants to restart database?

            if (database.IsRunning) {
                $scope.WarnMessage = "The new settings will be used the next time the database is started.";
                //                NoticeFactory.ShowNotice({ type: "success", msg: "The new settings will be used next time the database is started" });
            } else {
                $scope.SuccessMessage = "Saved.";
            }

            $scope.myForm.$setPristine();

            // Navigate to database list if user has not navigated to another page
            //if ($location.path() == "/databases/" + database.name + "/settings") {
            //    $location.path("/databases");
            //}

        }, function (messageObject, validationErrors) {

            // Error

            if (validationErrors != null && validationErrors.length > 0) {
                // Validation errors

                // Show errors on screen
                for (var i = 0; i < validationErrors.length; i++) {
                    //$scope.alerts.push({ type: 'danger', msg: validationErrors[i].message });

                    if ($scope.myForm[validationErrors[i].PropertyName] == undefined) {
                        //NoticeFactory.ShowNotice({ type: 'danger', msg: "Missing or invalid property: " + validationErrors[i].PropertyName });

                        self.HasErrorMessage = true;
                        self.ErrorMessage.Message = "Missing or invalid property: " + validationErrors[i].PropertyName;
                        self.ErrorMessage.HelpLink = null;

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

                self.HasErrorMessage = true;
                self.ErrorMessage.Message = messageObject.message;
                self.ErrorMessage.HelpLink = messageObject.helpLink;

                if (messageObject.isError) {
                    UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);
                }

                //if (messageObject.isError) {

                //    //var message = messageObject.message.replace(/\r\n/g, "<br>");

                //    UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);
                //}
                //else {
                //    NoticeFactory.ShowNotice({ type: 'danger', msg: messageObject.message, helpLink: messageObject.helpLink });
                //}
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

    // Set Data
    $scope.model.database = HostModelService.getDatabase($routeParams.name);

    $scope.refreshSettings();
}]);