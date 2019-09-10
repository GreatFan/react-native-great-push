var GreatPushWrapper = require("../greatPushWrapper.js");
import GreatPush from "react-native-great-push";

module.exports = {
    startTest: function(testApp) {
        testApp.readyAfterUpdate();
        GreatPush.notifyAppReady();
    },
    
    getScenarioName: function() {
        return "Good Update";
    }
};