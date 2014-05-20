'use strict';

var downloadApp = angular.module('loginApp', ['ngResource']);

downloadApp.controller('LoginCtrl',['$scope', '$http', '$log', '$window','$location',function ($scope, $http, $log, $window , $location) {

    var mySelf = this;
    
    this.credentials = {
        name: '',
        password: ''
    };
   this.login = function (credentials) {

        var data = this.credentials;
        $http.post("/login", data).then(function (response) {
            // Success     
            $window.location.href = $window.location.href;

        }, function (response) {
            // Error
            // TODO: Show wrong password message
            alert("Invalid password");
            $log.error("Failed to login", response);

            mySelf.credentials.password = '';
            $( "#password" ).focus();
        });


    };


}]);




