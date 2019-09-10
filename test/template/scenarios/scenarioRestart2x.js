var GreatPushWrapper = require("../greatPushWrapper.js");
import GreatPush from "react-native-great-push";

module.exports = {
    startTest: function(testApp) {
        GreatPush.restartApp(true);
        GreatPushWrapper.checkAndInstall(testApp, 
            () => {
                GreatPush.restartApp(true);
            }
        );
    },
    
    getScenarioName: function() {
        return "Restart2x";
    }
};