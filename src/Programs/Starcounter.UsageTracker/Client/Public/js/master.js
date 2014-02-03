'use strict';

var downloadApp = angular.module('downloadApp', ['ngResource']);

downloadApp.controller('DownloadCtrl', function ($scope, $http) {

    var mySelf = this;

    $http.get('/api/versions').then(function (response) {
        // success handler
        mySelf.editions = response.data.editions;

    }, function (response) {
        // error handler
        $scope.alertMessage = response.data;
    });
    
});





