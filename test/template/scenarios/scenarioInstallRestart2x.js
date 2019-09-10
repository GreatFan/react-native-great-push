var GreatPushWrapper = require("../greatPushWrapper.js");
import GreatPush from "react-native-great-push";

module.exports = {
    startTest: function(testApp) {
        GreatPushWrapper.checkAndInstall(testApp, 
            () => {
                GreatPush.restartApp();
                GreatPush.restartApp();
            }
        );
    },
    
    getScenarioName: function() {
        return "Install and Restart 2x";
    }
};