'use strict';

function StarcounterController($scope)
{
//    console.log("In controller");
//    console.log($scope);

    window.StarcounterSession.OnMessage = function( payload )
    {
        $scope.Results = payload.Results;
        $scope.$apply();
    }
    window.StarcounterSession.OnTrace = function( str )
    {
        console.log("TRACE: "+str);
    }


    TransformToSample($$DESIGNTIME$$);

   // Convert the $$$ object into a angular scope function
   for (var name in $$DESIGNTIME$$)
   {
       $scope[name] = window.$$DESIGNTIME$$[name];
   }

    /*
    Object.defineProperty( $scope, 'SearchText',
    	{
    		enumerable:		false,
    		configurable:	true,
    		get: function()
    		{
    			return this._SearchText;
    		},
    		set: function( v )
    		{
                console.log("setting!")
    			this._SearchText = v;
    		}

    	});
    	*/


    $scope._SearchText = "ABC";
    $scope.SearchText = function(v)
    {
        if (!this)
            throw "This is not defined!";
        if (v)
        {
          this._SearchText = v;
            console.log("set searchtext");
          return;
        }
        console.log("get searchtext");
        return this._SearchText;
    }

    $scope.ChangeProperty = function( id, ov, nv )
    {
        alert('heureka!' + id + nv + ov);
      //  var str = "Starcounter was notified that " + id + " changed from " + ov + " to " + nv + "."
       // console.log( str );
        window.StarcounterSession.SendMessage( JSON.stringify( {Id:id,Value:nv} ) );
    }

//    console.log("test = "+ this.myProp);

//   var scope = this;
//   this.$root.$on("$viewChange", function(event)
//   {
//       console.log("on " + angular.toJson(event));
//        console.log( angular.nextUid() );
    //   console.log("trying to find " + angular.toJson(event.currentScope) + " in scope " + angular.toJson(event.targetScope.$parent) );
 //      console.log( event.currentScope.$element );
//   });

   window.$$DESIGNTIME$$ = $scope; // Replace $$$ with this scope function

}

var e = document.documentElement;
//!e.getAttribute("ng-app") && e.setAttribute("ng-app","");
//!e.getAttribute("ng-controller") &&  e.setAttribute("ng-controller","StarcounterController");
//console.log("Setting up...");

//angular.element(document).ready(function() {
//          angular.bootstrap(document.documentElement);
//        });
