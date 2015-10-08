/**
 * ----------------------------------------------------------------------------
 * Sql page Controller
 * ----------------------------------------------------------------------------
 */
adminModule.controller('SqlCtrl', ['$scope', '$log', '$sce', '$document', '$location', '$routeParams', 'HostModelService', 'NoticeFactory', 'SqlService', 'UserMessageFactory', function ($scope, $log, $sce, $document, $location, $routeParams, HostModelService, NoticeFactory, SqlService, UserMessageFactory) {

    $scope.database = null;

    $scope.HasErrorMessage = false;
    $scope.ErrorMessage = {
        "Title": "",
        "Message": "",
        "HelpLink": ""
    };

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

        // Check if query is already 'rememberd'
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

        return $scope.isBusy == false && $scope.database._queryState.sqlQuery;
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

        $scope.database._queryState.sqlQuery = query.statement;
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

        $scope.HasErrorMessage = false;

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

            // Success
            $scope.isBusy = false;

            $scope.rememberQuery({ statement: query, databaseName: databaseName });
            // Fill columns keeping reference to old array
            $scope.database._queryState.columns.length = 0;
            angular.forEach(response.columns, function(column) {
              column.data = column.value;
              $scope.database._queryState.columns.push(column);
            });
            $scope.database._queryState.rows.length = 0;
            angular.forEach(response.rows.rows, function(row) {
              $scope.database._queryState.rows.push(row);
            });

            // Make all columns readonly
            for (var i = 0; i < $scope.database._queryState.columns.length ; i++) {
                $scope.database._queryState.columns[i].readOnly = true;
            }

            if (response.queryPlan) {

                // Replace all occurrences of \r\n with the html tag <br>
                // Replace all occurrences of \t with &emsp;
                var plan = response.queryPlan.replace(/\r\n/g, "<br>").replace(/\t/g, "&emsp;");

                $scope.database._queryState.queryPlan = $sce.trustAsHtml(plan);
            }
        },
            function (messageObject) {
                // Error
                $scope.isBusy = false;

                $scope.HasErrorMessage = true;
                $scope.HasSupportedMessage = messageObject.showSupportedMessage;
                $scope.ErrorMessage = {
                    "Title": messageObject.header,
                    "Message": messageObject.message,
                    "HelpLink": messageObject.helpLink

                };

                if (messageObject.isError) {
                    UserMessageFactory.showErrorMessage(messageObject.header, messageObject.message, messageObject.helpLink, messageObject.stackTrace);
                }
            });
    }

    // Set Data
    $scope.database = HostModelService.getDatabase($routeParams.name);

    // Add empty query state on database if it doent exist
    if ($scope.database._queryState == null) {
        $scope.database._queryState = {
            sqlQuery: "",
            columns: [],
            rows: []
        }
    }

    /**
     * On keypress event
     * @param {event} event Key event
     */
    function onKeyPress(event) {

        if ($scope.canExecute() && event.ctrlKey && (event.keyCode == 10 || event.keyCode == 13)) {
            $scope.execute($scope.database._queryState.sqlQuery, $scope.database.ID);
            event.preventDefault();
        }
    }

    $scope.$on('$destroy', function iVeBeenDismissed() {

        // Unbind the keypress listener
        $document.unbind('keypress', onKeyPress);
    })

    // bind the keypress listener
    $document.bind('keypress', onKeyPress);
    $scope.refreshQueryHistory();
}]);