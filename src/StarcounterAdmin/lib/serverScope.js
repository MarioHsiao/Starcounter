angular.module('StarcounterLib', ['panelApp'])
  .directive('serverScope', ['$http', 'appContext', function ($http, appContext) {
  var directiveDefinitionObject = {
    restrict: 'A',
    compile: function compile(tElement, tAttrs, transclude) {

      var rootLoaded = false;

      function overwriteRoot(scope, data) {
        for (var i in data) {
          if (data.hasOwnProperty(i)) {
            scope[i] = data[i];
          }
        }
      }

      function patchRoot(scope, patch) {
        if (patch.length) {
          console.log("patch", patch);
          jsonpatch.apply(scope, patch);
        }
      }

      function parseViewModelId(scope) {
        var meta = document.getElementsByTagName('meta');
        for (var i = 0, ilen = meta.length; i < ilen; i++) {
          if (angular.element(meta[i]).attr('name') == 'View-Model') {
            scope['View-Model'] = angular.element(meta[i]).attr('content');
            break;
          }
        }
      }

      function getRequestUrl(scope) {
        return window.location.href + '/../__vm/' + scope['View-Model'];
      }

      function getRoot(scope) {
        $http({method: 'GET', url: getRequestUrl(scope)}).success(function (data, status, headers, config) {
          overwriteRoot(scope, data);
          rootLoaded = true;
        });
      }

      function updateServer(scope, path, value) {
        var data = {
          "replace": path,
          "value": value
        };
        $http({method: 'PATCH', url: getRequestUrl(scope), data: data}).success(function (data, status, headers, config) {
          patchRoot(scope, data);
        });
      }

      function setWatchers(scope, props) {
        for (var i = 0, ilen = props.length; i < ilen; i++) {
          scope.$watch(props[i], (function (prop) {
            return (function (current, previous, scope) {
              if (rootLoaded) {
                updateServer(scope, '/' + prop.replace(/\./g, '/'), current);
              }
            })
          })(props[i]), true);
        }
      }

      return function postLink(scope, element, attrs, controller) {
        if (typeof window.__elim_rq !== 'undefined') {
          overwriteRoot(scope, window.__elim_rq);
          rootLoaded = true;
        }
        else {
          parseViewModelId(scope);
          getRoot(scope);
        }

        var tree = appContext.getScopeTree(scope);
        var watched = [];

        function findWatchedRecursive(watched, obj, parent) {
          parent = parent || '';
          for (var i in obj) {
            if (obj.hasOwnProperty(i)) {
              if (Object.prototype.toString.apply(obj[i]) === '[object Object]') {
                findWatchedRecursive(watched, obj[i], parent + i + '.');
              }
              else if (typeof obj[i] !== "function") {
                watched.push(parent + i);
              }
            }
          }
        }

        findWatchedRecursive(watched, tree.locals);

        setWatchers(scope, watched);
      }
    }
  };
  return directiveDefinitionObject;
}]);