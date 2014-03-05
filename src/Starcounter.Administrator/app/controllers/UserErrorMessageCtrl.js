/**
 * User ErrorMessage controller
 */
adminModule.controller('UserErrorMessageCtrl', function ($scope, dialog) {

    $scope.model = dialog.options.data;

    /**
     * Close dialog
     * @param {object} result Result
     */
    $scope.close = function (result) {
        dialog.close(result);
    };

});