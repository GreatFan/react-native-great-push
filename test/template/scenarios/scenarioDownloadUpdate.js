var GreatPushWrapper = require("../greatPushWrapper.js");

module.exports = {
    startTest: function(testApp) {
        GreatPushWrapper.checkForUpdate(testApp,
            GreatPushWrapper.download.bind(undefined, testApp, undefined, undefined));
    },
    
    getScenarioName: function() {
        return "Download Update";
    }
};