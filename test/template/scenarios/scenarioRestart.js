var GreatPushWrapper = require("../greatPushWrapper.js");
import GreatPush from "react-native-great-push";

module.exports = {
    startTest: function(testApp) {
        testApp.sendCurrentAndPendingPackage()
            .then(() => {
                GreatPushWrapper.sync(testApp, (status) => {
                    if (status === GreatPush.SyncStatus.UPDATE_INSTALLED) {
                        testApp.sendCurrentAndPendingPackage().then(GreatPush.restartApp);
                    }
                }, undefined, { installMode: GreatPush.InstallMode.ON_NEXT_RESTART });
            });
    },
    
    getScenarioName: function() {
        return "Restart";
    }
};