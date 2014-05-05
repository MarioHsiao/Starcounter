/**
 * ----------------------------------------------------------------------------
 * Sql page Controller
 * ----------------------------------------------------------------------------
 */
adminModule.controller('SqlCtrl', ['$scope', '$log', '$sce', '$document', 'NoticeFactory', 'SqlService', 'DatabaseService', 'UserMessageFactory', function ($scope, $log, $sce, $document, NoticeFactory, SqlService, DatabaseService, UserMessageFactory) {

    // List of databases
    $scope.databases = DatabaseService.databases;

    // Show/Hide progressbar
    $scope.isBusy = false;

    // Execute button title
    $scope.executeButtonTitle = function (isBusy) {
        if (isBusy) {
            return "Executing...";
        }
        else {
            return "Execute";
        }
    }

    // Query history array
    $scope.queryHistory = []; // { statement: "select s from materialized_table s", databaseName: "" }

    /**
     * Remember Query
     * @param {object} query Query
     */
    $scope.rememberQuery = function (query) {

        var maxItems = 10;

        var trimmedQuery = query.statement.trim();

        // Check if file is already 'rememberd'
        for (var i = 0; i < $scope.queryHistory.length ; i++) {

            // Query already rememberd
            if (trimmedQuery == $scope.queryHistory[i].statement.trim()) {
                return;
            }

        }

        // Add new items to the beginning of an array:
        $scope.queryHistory.unshift(query);

        var toMany = $scope.queryHistory.length - maxItems;

        if (toMany > 0) {
            $scope.queryHistory.splice(maxItems, toMany);
        }

        var str = JSON.stringify($scope.queryHistory);
        localStorage.setItem("queryHistory", str);
    }


    /**
     * Can execute query
     * @return {boolean} True if the conditions is correct.
     */
    $scope.canExecute = function () {
        return $scope.queryState.selectedDatabaseName && $scope.isBusy == false && $scope.queryState.sqlQuery;
    }


    /**
     * Refresh Query history
     */
    $scope.refreshQueryHistory = function () {

        if (typeof (Storage) !== "undefined") {
            var result = localStorage.getItem("queryHistory");
            if (result) {
                try {
                    $scope.queryHistory = JSON.parse(result);
                }
                catch (err) {
                    $log.error(err, "Removing invalid query history");
                    localStorage.removeItem("queryHistory");
                }
            }
        }
        else {
            // No web storage support..
        }
    }


    /**
     * Button click, Select One Query from history
     * @param {string} query Query
     */
    $scope.btnSelectQuery = function (query) {
        $scope.queryState.sqlQuery = query.statement;
    }


    /**
     * Button click, Execut query
     * @param {string} query Query
     * @param {string} databaseName Database name
     */
    $scope.btnExecute = function (query, databaseName) {
        $scope.execute(query, databaseName);
    }


    /**
     * Button click, Execut query
     * @param {string} query Query
     * @param {string} databaseName Database name
     */
    $scope.execute = function (query, databaseName) {

        NoticeFactory.ClearAll();

        if (!query) {
            // if this occure then the binding the the textarea failed..
            var message = "Failed to retrieve the query text due to some binding issues. Refresh the page and try again.";
            NoticeFactory.ShowNotice({ type: 'danger', msg: message, helpLink: null });
            return;
        }
        $scope.isBusy = true;

        // Execute query
        SqlService.executeQuery(query, databaseName, function (response) {

            $scope.isBusy = false;

            $scope.rememberQuery({ statement: query, databaseName: databaseName });


            // Success
            $scope.queryState.columns = response.columns;
            $scope.queryState.rows = response.rows.rows;

            // Make all columns readonly
            for (var i = 0; i < $scope.queryState.columns.length ; i++) {
                $scope.queryState.columns[i].readOnly = true;
            }

            if (response.queryPlan) {

                // Replace all occurrences of \r\n with the html tag <br>
                // Replace all occurrences of \t with &emsp;
                var plan = response.queryPlan.replace(/\r\n/g, "<br>").replace(/\t/g, "&emsp;");

                $scope.queryState.queryPlan = $sce.trustAsHtml(plan);
            }


        },
            function (messageObject) {
                // Error
                $scope.isBusy = false;

                if (messageObject.isError) {
                    UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);
                }
                else {
                    NoticeFactory.ShowNotice({ type: 'danger', msg: messageObject.message, helpLink: messageObject.helpLink });
                }

            });

    }


    // Controller destructor
    $scope.$on('$destroy', function iVeBeenDismissed() {
        // Unbind the keypress listener
        $document.unbind('keypress', onKeyPress);
    })
    // bind the keypress listener
    $document.bind('keypress', onKeyPress);


    /**
     * On keypress event
     * @param {event} event Key event
     */
    function onKeyPress(event) {

        if ($scope.canExecute() && event.ctrlKey && (event.keyCode == 10 || event.keyCode == 13)) {
            $scope.execute($scope.queryState.sqlQuery, $scope.queryState.selectedDatabaseName);
            event.preventDefault();
        }
    }


    // Init
    DatabaseService.refreshDatabases(function () {

        var database = DatabaseService.getDatabase($scope.queryState.selectedDatabaseName);
        if (database != null) {
            $scope.queryState.selectedDatabaseName = database.name;
        }
        else {
            if ($scope.databases.length > 0 && !$scope.queryState.selectedDatabaseName && $scope.databases[0].running) {
                $scope.queryState.selectedDatabaseName = $scope.databases[0].name;
            }
            else {
                $scope.queryState.selectedDatabaseName = null;
            }
        }


    }, function (messageObject) {
        // Error
        UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);
    });


    $scope.afterRender = scrollRefresh;

    $scope.refreshQueryHistory();

}]);