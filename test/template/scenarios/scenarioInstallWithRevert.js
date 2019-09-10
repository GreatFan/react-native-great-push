var GreatPushWrapper = require("../greatPushWrapper.js");
import GreatPush from "react-native-great-push";

module.exports = {
    startTest: function(testApp) {
        GreatPushWrapper.checkAndInstall(testApp, undefined, undefined, GreatPush.InstallMode.IMMEDIATE);
    },
    
    getScenarioName: function() {
        return "Install with Revert";
    }
};