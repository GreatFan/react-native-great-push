var GreatPushWrapper = require("../greatPushWrapper.js");

module.exports = {
    startTest: function(testApp) {
        GreatPushWrapper.checkForUpdate(testApp, undefined, undefined, "CUSTOM-DEPLOYMENT-KEY");
    },
    
    getScenarioName: function() {
        return "Check for Update Custom Key";
    }
};