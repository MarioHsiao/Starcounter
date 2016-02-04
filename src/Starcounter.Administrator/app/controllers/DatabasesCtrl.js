/**
 * ----------------------------------------------------------------------------
 * Databases page Controller
 * ----------------------------------------------------------------------------
 */
adminModule.controller('DatabasesCtrl', ['$scope', '$log', 'HostModelService', '$sce', 'NoticeFactory', 'UserMessageFactory', function ($scope, $log, HostModelService, $sce, NoticeFactory, UserMessageFactory) {

    $scope.data = HostModelService.data;
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

        var message = $sce.trustAsHtml("This will delete the database <strong>'" + database.ID + "'</strong> <strong>permanently!</strong>.</br>All data will be completly deleted, with no ways to recover it.</br>This action is not possible to reverse.");
        var buttons = [{ result: 0, label: 'Delete Database', cssClass: 'btn-danger' }, { result: 1, label: 'Cancel', cssClass: 'btn' }];
        var model = { "title": title, "message": message, "buttons": buttons, enteredDatabaseName: "" };
        model.pattern = database.ID;
//        model.pattern = "/^" + database.ID + "$/";
        UserMessageFactory.showModal('app/partials/databaseDeleteModal.html', 'UserErrorMessageCtrl', model, function (result) {

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

        UserMessageFactory.showMessageBox(title, message, buttons, function (result) {

            if (result == 0) {

                database.Stop$++;
            }
        });
    }

    /**
     * Badges filter
     * @param {object} application Application
     */
    $scope.filterByState = function (application) {

        return application.IsInstalled || application.IsRunning;
    };

    $scope.$watch('viewmode', function (newValue, oldValue) {

        // Save user state
        localStorage.setItem('databasesviewmode', newValue);
    });

    // Get user state
    if (localStorage.getItem('databasesviewmode') != null) {

        $scope.viewmode = localStorage.getItem('databasesviewmode');
    }
}]);