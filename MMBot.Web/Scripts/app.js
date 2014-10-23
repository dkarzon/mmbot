angular.module('mmbotweb', [])
//cool story bro...

//add jQuery because things
.value('$', $)

.service('signalRSvc', ['$', '$rootScope', function ($, $rootScope) {

    var _mmbotweb;

    function _initialize() {

        // Declare a proxy to reference the hub.
        _mmbotweb = $.connection.mmbotHub;
        // Create a function that the hub can call to broadcast messages.
        _mmbotweb.client.buildResult = _buildResult;
        // Start the connection.
        $.connection.hub.start();
    };

    function _buildResult(message) {
        $rootScope.$emit("buildResult", message);
    };

    function _buildScript(message) {
        _mmbotweb.server.buildScript(message);
    };

    return {
        initialize: _initialize,
        buildScript: _buildScript
    };
}])

.controller('mmbotCtrl', ['$scope', '$rootScope', 'signalRSvc', function ($scope, $rootScope, signalRSvc) {

    //start me up baby
    signalRSvc.initialize();

    $scope.scriptfile = 'var robot = Require<Robot>();\nrobot.Respond("updog", msg => msg.Send("What\'s up dog?"));';

    $rootScope.compile = function () {
        signalRSvc.buildScript($scope.scriptfile);
    };

    $rootScope.$on('buildResult', function (e, message) {
        //scope.$apply because different thread/context/angle
        $rootScope.$apply(function () {
            $rootScope.buildResult = message;
        });
    });

}]);