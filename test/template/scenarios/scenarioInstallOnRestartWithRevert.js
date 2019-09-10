var GreatPushWrapper = require("../greatPushWrapper.js");
import GreatPush from "react-native-great-push";

module.exports = {
    startTest: function(testApp) {
        GreatPushWrapper.checkAndInstall(testApp, undefined, undefined, GreatPush.InstallMode.ON_NEXT_RESTART);
    },
    
    getScenarioName: function() {
        return "Install on Restart with Revert";
    }
};