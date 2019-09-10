var GreatPushWrapper = require("../greatPushWrapper.js");
import GreatPush from "react-native-great-push";

module.exports = {
    startTest: function(testApp) {
        testApp.readyAfterUpdate((responseBody) => {
            if (responseBody !== "SKIP_NOTIFY_APPLICATION_READY") {
                GreatPush.notifyAppReady();
                GreatPushWrapper.checkAndInstall(testApp, undefined, undefined, GreatPush.InstallMode.ON_NEXT_RESTART);
            } else {
                testApp.setStateAndSendMessage("Skipping notifyApplicationReady!", "SKIPPED_NOTIFY_APPLICATION_READY");
            }
        });
    },
    
    getScenarioName: function() {
        return "Conditional Update";
    }
};