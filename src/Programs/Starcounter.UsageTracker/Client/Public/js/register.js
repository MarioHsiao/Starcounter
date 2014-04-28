'use strict';

var downloadApp = angular.module('registerApp', ['ngResource']);

downloadApp.controller('RegisterCtrl', function ($scope, $http, $log, $window ) {

    var mySelf = this;

    this.hasRegistered = false;
    this.userEmail = "";

    this.register = function(userEmail) {

    	var data = { useremail:userEmail };

        $http.post("/register", data).then(function (response) {
            // Success     

        }, function (response) {
            // Error
            $log.error("Failed to register user", response);
        });

    	this.hasRegistered = true;


    }

    this.close = function() {
    	  $window.history.back();
    }
    
});





