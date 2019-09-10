var GreatPushWrapper = require("../greatPushWrapper.js");
import GreatPush from "react-native-great-push";

module.exports = {
    startTest: function(testApp) {
        GreatPushWrapper.sync(testApp, undefined, undefined, { installMode: GreatPush.InstallMode.IMMEDIATE });
        GreatPushWrapper.sync(testApp, undefined, undefined, { installMode: GreatPush.InstallMode.IMMEDIATE });
    },
    
    getScenarioName: function() {
        return "Sync 2x";
    }
};