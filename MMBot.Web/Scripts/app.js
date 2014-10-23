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

    function _buildResult(buildLog) {
        $rootScope.$emit("buildResult", buildLog);
    };

    function _buildScript(message) {
        _mmbotweb.server.buildScript(message);
    };

    function _sendCommand(command) {
        _mmbotweb.server.sendCommand(command);
    };

    return {
        initialize: _initialize,
        buildScript: _buildScript,
        sendCommand: _sendCommand
    };
}])

.controller('mmbotCtrl', ['$scope', '$rootScope', 'signalRSvc', function ($scope, $rootScope, signalRSvc) {

    //start me up baby
    signalRSvc.initialize();

    $scope.scriptfile = 'var robot = Require<Robot>();\nrobot.Respond("updog", msg => msg.Send("What\'s up dog?"));';
    $scope.command = 'mmbot updog';
    $rootScope.buildLogs = [];

    $rootScope.compile = function () {
        signalRSvc.buildScript($scope.scriptfile);
    };

    $rootScope.send = function () {
        signalRSvc.sendCommand($scope.command);
    };

    $rootScope.$on('buildResult', function (e, buildLog) {
        //scope.$apply because different thread/context/angle
        $rootScope.$apply(function () {
            $rootScope.buildLogs.push(buildLog);
        });
    });

}]);