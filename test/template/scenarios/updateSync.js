var GreatPushWrapper = require("../greatPushWrapper.js");

module.exports = {
    startTest: function(testApp) {
        testApp.readyAfterUpdate();
        GreatPushWrapper.sync(testApp);
    },
    
    getScenarioName: function() {
        return "Good Update (w/ Sync)";
    }
};