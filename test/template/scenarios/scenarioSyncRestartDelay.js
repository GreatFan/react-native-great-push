var GreatPushWrapper = require("../greatPushWrapper.js");
import GreatPush from "react-native-great-push";

module.exports = {
    startTest: function(testApp) {
        GreatPushWrapper.sync(testApp, undefined, undefined,
            { installMode: GreatPush.InstallMode.ON_NEXT_RESTART,
                minimumBackgroundDuration: 15 });
    },
    
    getScenarioName: function() {
        return "Sync Restart Delay";
    }
};