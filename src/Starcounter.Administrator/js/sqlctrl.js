

function sqlctrl($scope) {

    $scope.$parent.Rows = [];
    $scope.$parent.Columns = [];

    $scope.$parent.$watch("Output", function (newValue) {

        if (newValue == "") return;
        var json = angular.fromJson(newValue);

        $scope.hasErrorMessage = json.error.message != null;

        if ($scope.hasErrorMessage) {
            // Has error.
            $scope.ErrorMessage = json.error.message;
            return;
        }

        $scope.$parent.Columns = json.columns;
        $scope.$parent.Rows = json.rows;

    }, false);

}