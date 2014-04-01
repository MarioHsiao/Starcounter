/**
 * ----------------------------------------------------------------------------
 * Server Settings page Controller
 * ----------------------------------------------------------------------------
 */
adminModule.controller('ServerSettingsCtrl', ['$scope', '$log', '$location', 'NoticeFactory', 'ServerService', 'UserMessageFactory', function ($scope, $log, $location, NoticeFactory, ServerService, UserMessageFactory) {

    // Database Default settings
    $scope.model = ServerService.model;


    /**
     * Reset server settings
     */
    $scope.btnResetSettings = function () {
        $scope.refreshServerSettings();
    }


    /**
     * Refresh server settings
     */
    $scope.refreshServerSettings = function () {

        ServerService.refreshServerSettings(function () {
            // Success

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
     * Button click, Save settings
     * @param {object} settings Settings
     */
    $scope.btnSaveSettings = function (settings) {

        ServerService.saveSettings(settings, function (settings) {

            // Success

            NoticeFactory.ShowNotice({ type: "success", msg: "Settings saved. The new settings will be used at the next start of the server" });

            $scope.myForm.$setPristine();

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
     * Button click, Reset settings
     */
    $scope.btnResetSettings = function () {
        $scope.refreshServerSettings();
    }

    // Init
    $scope.refreshServerSettings();

}]);