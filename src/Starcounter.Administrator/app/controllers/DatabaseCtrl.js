/**
 * ----------------------------------------------------------------------------
 * Database page Controller
 * ----------------------------------------------------------------------------
 */
adminModule.controller('DatabaseCtrl', ['$scope', '$log', '$sce', '$location', '$routeParams', 'HostModelService', 'UserMessageFactory', function ($scope, $log, $sce, $location, $routeParams, HostModelService, UserMessageFactory) {

    $scope.database = null;
    $scope.viewmode = "list";

    $scope.appsFilter = function (application) {

        return application.IsInstalled && (application.HasErrorMessage || application.IsRunning == false);
    }

    /**
     * Start database
     * @param {object} database Database
     */
    $scope.btnStartDatabase = function (database) {

        database.Start$++;
    }

    /**
      * Delete database
      * @param {object} database Database
      */
    $scope.btnDeleteDatabase = function (database) {

        var title = "Delete database";

        var message = $sce.trustAsHtml("This will delete the database <strong>'" + database.ID + "'</strong> <strong>permantely!</strong>.</br>All data will be completly deleted, with no ways to recover it.</br>This action is not possible to reverse.");
        var buttons = [{ result: 0, label: 'Delete Database', cssClass: 'btn-danger' }, { result: 1, label: 'Cancel', cssClass: 'btn' }];
        var model = { "title": title, "message": message, "buttons": buttons, enteredDatabaseName: "" };
        model.pattern = "/^" + database.ID + "$/";
        UserMessageFactory.showModal('app/partials/database-delete-modal.html', 'UserErrorMessageCtrl', model, function (result) {

            if (result == 0) {

                database.Delete$++;
            }
        });
    }

    /**
     * Stop Database
     * @param {object} database Database
     */
    $scope.btnStopDatabase = function (database) {

        var title = "Stop database";
        var message = "Do you want to stop the database " + database.DisplayName;
        var buttons = [{ result: 0, label: 'Stop', cssClass: 'btn-danger' }, { result: 1, label: 'Cancel', cssClass: 'btn' }];

        database.Stop$++;

        //UserMessageFactory.showMessageBox(title, message, buttons, function (result) {

        //    if (result == 0) {

        //        database.Stop$++;
        //    }
        //});
    }

    /**
     * Start Application
     * @param {object} application Application
     */
    $scope.btnStartApplication = function (application) {

        application.Start$++;
    }

    /**
     * Stop Application
     * @param {object} application Application
     */
    $scope.btnStopApplication = function (application) {

        application.Stop$++;
    }

    /**
     * Install Application
     * @param {object} application Application
     */
    $scope.btnInstallApplication = function (application) {

        application.Install$++;
    }

    /**
     * Uninstall Application
     * @param {object} application Application
     */
    $scope.btnUninstallApplication = function (application) {

        application.Uninstall$++;
    }

    /**
     * Delete Application
     * @param {object} application Application
     */
    $scope.btnDeleteApplication = function (application) {

        var title = "Delete application";
        var message = "Do you want to delete the application " + application.DisplayName;
        var buttons = [{ result: 0, label: 'Delete', cssClass: 'btn-danger' }, { result: 1, label: 'Cancel', cssClass: 'btn' }];

        UserMessageFactory.showMessageBox(title, message, buttons, function (result) {

            if (result == 0) {

                application.Delete$++;
            }
        });
    }

    // Set Data
    $scope.database = HostModelService.getDatabase($routeParams.name);

    $scope.$watch('viewmode', function (newValue, oldValue) {
        // Save user state
        localStorage.setItem('databaseviewmode', newValue);
    });

    // Get user state
    if (localStorage.getItem('databaseviewmode') != null) {
        $scope.viewmode = localStorage.getItem('databaseviewmode');
    }
}]);