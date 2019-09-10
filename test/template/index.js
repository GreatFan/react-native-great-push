/**
 * GreatPush React-Native Test App
 */

import React, {
  Component
} from 'react';

import {
  AppRegistry,
  StyleSheet,
  Text,
  View
} from 'react-native';

import GreatPush from "react-native-great-push";

var testScenario = require("./GREAT_PUSH_INDEX_JS_PATH");

/** A promise that maintains synchronous sending of the test messages. */
var testMessageQueue;

var GREAT_PUSH_TEST_APP_NAME = React.createClass({
    // GreatPush API Callbacks
    
    // checkForUpdate
    checkUpdateSuccess(remotePackage) {
        if (remotePackage) {
            if (!remotePackage.failedInstall) {
                return this.setStateAndSendMessage("There is an update available. Remote package:" + JSON.stringify(remotePackage), "CHECK_UPDATE_AVAILABLE", [remotePackage]);
            } else {
                return this.setStateAndSendMessage("An update is available but failed previously. Remote package:" + JSON.stringify(remotePackage), "UPDATE_FAILED_PREVIOUSLY");
            }
        } else {
            return this.setStateAndSendMessage("The application is up to date.", "CHECK_UP_TO_DATE");
        }
    },
    checkUpdateError(error) {
        return this.setStateAndSendMessage("An error occured while checking for updates:\n" + error, "CHECK_ERROR");
    },
    
    // remotePackage.download
    downloadSuccess(localPackage) {
        return this.setStateAndSendMessage("Download succeeded.", "DOWNLOAD_SUCCEEDED", [localPackage]);
    },
    downloadError(error) {
        return this.setStateAndSendMessage("Download error:\n" + error, "DOWNLOAD_ERROR");
    },
    
    // localPackage.install
    installSuccess() {
        return this.setStateAndSendMessage("Update installed.", "UPDATE_INSTALLED");
    },
    installError() {
        return this.setStateAndSendMessage("Install error.", "INSTALL_ERROR");
    },
    
    // sync
    onSyncStatus(status) {
        return this.setStateAndSendMessage("Sync status " + status + " received.", "SYNC_STATUS", [status]);
    },
    onSyncError(error) {
        return this.setStateAndSendMessage("Sync error " + error + " received.", "SYNC_STATUS", [GreatPush.SyncStatus.UNKNOWN_ERROR]);
    },
    
    
    // Test Output Methods
    
    readyAfterUpdate(callback) {
        return this.setStateAndSendMessage("Ready after update.", "DEVICE_READY_AFTER_UPDATE", undefined, callback);
    },
    
    sendCurrentAndPendingPackage() {
        return GreatPush.getUpdateMetadata(GreatPush.UpdateState.PENDING)
            .then((pendingPackage) => {
                this.setStateAndSendMessage("Pending package: " + pendingPackage, "PENDING_PACKAGE", [pendingPackage ? pendingPackage.packageHash : null]);
                return GreatPush.getUpdateMetadata(GreatPush.UpdateState.RUNNING);
            })
            .then((currentPackage) => {
                return this.setStateAndSendMessage("Current package: " + currentPackage, "CURRENT_PACKAGE", [currentPackage ? currentPackage.packageHash : null]);
            });
    },
    
    setStateAndSendMessage(message, testMessage, args, callback) {
        this.setState({
            message: this.state.message + "\n...\n" + message
        });
        return this.sendTestMessage(testMessage, args, callback);
    },
    
    sendTestMessage(message, args, callback) {
        function makeNetworkCall() {
            return new Promise(function(resolve, reject) {
                var xhr = new XMLHttpRequest();
        
                xhr.onreadystatechange = function () {
                    if (xhr.readyState == 4 && xhr.status == 200) {
                        callback && callback(xhr.response);
                        resolve();
                    }
                };
                
                xhr.open("POST", "GREAT_PUSH_SERVER_URL/reportTestMessage", true);
                var body = JSON.stringify({ message: message, args: args});
                console.log("Sending test message body: " + body);

                xhr.setRequestHeader("Content-type", "application/json");
                
                xhr.send(body);
            });
        }
        
        if (!testMessageQueue) testMessageQueue = makeNetworkCall();
        else testMessageQueue = testMessageQueue.then(makeNetworkCall);
        
        return testMessageQueue;
    },
    
    
    // Test Setup Methods
    
    componentDidMount() {
        testScenario.startTest(this);
    },
    
    getInitialState() {
        return {
            message: ""
        };
    },

    render() {
        return (
        <View style={styles.container}>
            <Text style={styles.welcome}>
            GreatPush React-Native Plugin Tests
            </Text>
            <Text style={styles.instructions}>
            {testScenario.getScenarioName()}{this.state.message}
            </Text>
        </View>
        );
    }
});

const styles = StyleSheet.create({
    container: {
        flex: 1,
        justifyContent: 'center',
        alignItems: 'center',
        backgroundColor: '#F5FCFF',
    },
    welcome: {
        fontSize: 20,
        textAlign: 'center',
        margin: 10,
    },
    instructions: {
        textAlign: 'center',
        color: '#333333',
        marginBottom: 5,
    },
});

AppRegistry.registerComponent('GREAT_PUSH_TEST_APP_NAME', () => GREAT_PUSH_TEST_APP_NAME);