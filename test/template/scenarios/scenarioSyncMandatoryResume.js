var GreatPushWrapper = require("../greatPushWrapper.js");
import GreatPush from "react-native-great-push";

module.exports = {
    startTest: function(testApp) {
        GreatPushWrapper.sync(testApp, undefined, undefined,
            { installMode: GreatPush.InstallMode.ON_NEXT_RESTART,
                mandatoryInstallMode: GreatPush.InstallMode.ON_NEXT_RESUME });
    },
    
    getScenarioName: function() {
        return "Sync Mandatory Resume";
    }
};