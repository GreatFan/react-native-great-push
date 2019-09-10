var GreatPushWrapper = require("../greatPushWrapper.js");

module.exports = {
    startTest: function(testApp) {
        GreatPushWrapper.checkForUpdate(testApp);
    },
    
    getScenarioName: function() {
        return "Check for Update";
    }
};