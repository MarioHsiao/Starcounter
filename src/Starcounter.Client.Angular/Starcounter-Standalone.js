/*
 * starcounter-standalone v0.1.1
 * (c) 2012 Starcounter. http://www.starcounter.com
 */

angular.module('starcounter-standalone', [])
  .directive('serverScope', ['$http',  function ($http) {
  var directiveDefinitionObject = {
    restrict: 'A',
    compile: function compile(tElement, tAttrs, transclude) {

      return function postLink(scope, element, attrs, controller) {

			if(!attrs.serverScope) {
			 console.log("WARNING: No json file specified.\nUse the attribut server-scope='<path to json file>");
			 return;
			}

	    	$http.get(attrs.serverScope).success(function(data, status, headers, config) {

	    		// Json file loaded
		        for (var i in data) {
		          if (data.hasOwnProperty(i)) {
		            scope[i] = data[i];
		          }
	        	}

			}).error( function(data, status, headers, config) {
				// console.log("ERROR "+status+": Loading "+attrs.serverScope);
			});

      }

    }
  };
  return directiveDefinitionObject;
  
}]);
