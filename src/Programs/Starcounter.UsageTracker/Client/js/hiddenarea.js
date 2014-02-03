'use strict';

var adminApp = angular.module('admin', ['ngResource']);


adminApp.controller('MasterCtrl', function ($scope, $http) {

    console.log("MasterCtrl");

    $scope.versions = [];

    this.getVersions = function () {

        $http.get('hiddenarea/versions').then(function (response) {
            // success handler
            console.log("Success:" + response.data);
            $scope.versions = response.data.versions;

        }, function (response) {
            // error handler
            $scope.versions = [];
            console.log("Error:" + response);
        });
    }

    this.getVersions();


});





