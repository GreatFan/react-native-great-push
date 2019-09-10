/*
The script serves to generate GreatPushified React Native app to reproduce issues or for testing purposes.

Requirements:
    1. npm i -g react-native-cli
    2. npm i -g great-push-cli
    3. great-push register

Usage: node create-app.js <appName> <reactNativeVersion> <reactNativeGreatPushVersion>
    1. node create-app.js 
    2. node create-app.js myapp
    3. node create-app.js myapp react-native@0.47.1 react-native-great-push@5.0.0-beta
    4. node create-app.js myapp react-native@latest Microsoft/react-native-great-push

Parameters:
    1. <appName> - GreatPushDemoAppTest
    2. <reactNativeVersion> - react-native@latest
    3. <reactNativeGreatPushVersion> - react-native-great-push@latest
*/

let fs = require('fs');
let path = require('path');
let nexpect = require('./nexpect');
let child_proces = require('child_process');
let execSync = child_proces.execSync;

let args = process.argv.slice(2);
let appName = args[0] || 'GreatPushDemoAppTest';

if (fs.existsSync(appName)) {
    console.error(`Folder with name "${appName}" already exists! Please delete`);
    process.exit();
}

let appNameAndroid = `${appName}-android`;
let appNameIOS = `${appName}-ios`;
let reactNativeVersion = args[1] || `react-native@${execSync('npm view react-native version')}`.trim();
let reactNativeGreatPushVersion = args[2] || `react-native-great-push@${execSync('npm view react-native-great-push version')}`.trim();

console.log(`App name: ${appName}`);
console.log(`React Native version: ${reactNativeVersion}`);
console.log(`React Native Module for GreatPush version: ${reactNativeGreatPushVersion} \n`);

let androidStagingDeploymentKey = null;
let iosStagingDeploymentKey = null;



//GENERATE START
createGreatPushApp(appNameAndroid, 'android');
createGreatPushApp(appNameIOS, 'ios');

generatePlainReactNativeApp(appName, reactNativeVersion);
process.chdir(appName);
installGreatPush(reactNativeGreatPushVersion);
linkGreatPush(androidStagingDeploymentKey, iosStagingDeploymentKey);
//GENERATE END



function createGreatPushApp(name, platform) {
    try {
        console.log(`Creating GreatPush app "${name}" to release updates for ${platform}...`);
        execSync(`great-push app add ${name} ${platform} react-native`);
        console.log(`App "${name}" has been created \n`);
    } catch (e) {
        console.log(`App "${name}" already exists \n`);
    }
    let deploymentKeys = JSON.parse(execSync(`great-push deployment ls ${name} -k --format json`));
    let stagingDeploymentKey = deploymentKeys[1].key;
    console.log(`Deployment key for ${platform}: ${stagingDeploymentKey}`);
    console.log(`Use "great-push release-react ${name} ${platform}" command to release updates for ${platform} \n`);

    switch (platform) {
        case 'android':
            androidStagingDeploymentKey = stagingDeploymentKey;
            break;
        case 'ios':
            iosStagingDeploymentKey = stagingDeploymentKey;
            break;
    }
}

function generatePlainReactNativeApp(appName, reactNativeVersion) {
    console.log(`Installing React Native...`);
    execSync(`react-native init ${appName} --version ${reactNativeVersion}`);
    console.log(`React Native has been installed \n`);
}

function installGreatPush(reactNativeGreatPushVersion) {
    console.log(`Installing React Native Module for GreatPush...`);
    execSync(`npm i --save ${reactNativeGreatPushVersion}`);
    console.log(`React Native Module for GreatPush has been installed \n`);
}

function linkGreatPush(androidStagingDeploymentKey, iosStagingDeploymentKey) {
    console.log(`Linking React Native Module for GreatPush...`);
    nexpect.spawn(`react-native link react-native-great-push`)
        .wait("What is your GreatPush deployment key for Android (hit <ENTER> to ignore)")
        .sendline(androidStagingDeploymentKey)
        .wait("What is your GreatPush deployment key for iOS (hit <ENTER> to ignore)")
        .sendline(iosStagingDeploymentKey)
        .run(function (err) {
            if (!err) {
                console.log(`React Native Module for GreatPush has been linked \n`);
                setupAssets();
            }
            else {
                console.log(err);
            }
        });
}

function setupAssets() {
    fs.unlinkSync('./index.ios.js');
    fs.unlinkSync('./index.android.js');

    fs.writeFileSync('demo.js', fs.readFileSync('../GreatPushDemoApp/demo.js'));
    fs.writeFileSync('index.ios.js', fs.readFileSync('../GreatPushDemoApp/index.ios.js'));
    fs.writeFileSync('index.android.js', fs.readFileSync('../GreatPushDemoApp/index.android.js'));

    copyRecursiveSync('../GreatPushDemoApp/images', './images');

    fs.readFile('demo.js', 'utf8', function (err, data) {
        if (err) {
            return console.error(err);
        }
        var result = data.replace(/GreatPushDemoApp/g, appName);

        fs.writeFile('demo.js', result, 'utf8', function (err) {
            if (err) return console.error(err);

            if (!/^win/.test(process.platform)) {
                optimizeToTestInDebugMode();
                process.chdir('../');
                grantAccess(appName);
            }
            console.log(`\nReact Native app "${appName}" has been generated and GreatPushified!`);
            process.exit();
        });
    });
}

function optimizeToTestInDebugMode() {
    let rnXcodeShLocationFolder = 'scripts';
    try {
        let rnVersions = JSON.parse(execSync(`npm view react-native versions --json`));
        let currentRNversion = JSON.parse(fs.readFileSync('./package.json'))['dependencies']['react-native'];
        if (rnVersions.indexOf(currentRNversion) > -1 &&
            rnVersions.indexOf(currentRNversion) < rnVersions.indexOf("0.46.0-rc.0")) {
            rnXcodeShLocationFolder = 'packager';
        }
    } catch(e) {}
    
    execSync(`perl -i -p0e 's/#ifdef DEBUG.*?#endif/jsCodeLocation = [GreatPush bundleURL];/s' ios/${appName}/AppDelegate.m`);
    execSync(`sed -ie '17,20d' node_modules/react-native/${rnXcodeShLocationFolder}/react-native-xcode.sh`);
    execSync(`sed -ie 's/targetName.toLowerCase().contains("release")$/true/' node_modules/react-native/react.gradle`);
}

function grantAccess(folderPath) {
    execSync('chown -R `whoami` ' + folderPath);
    execSync('chmod -R 755 ' + folderPath);
}

function copyRecursiveSync(src, dest) {
    var exists = fs.existsSync(src);
    var stats = exists && fs.statSync(src);
    var isDirectory = exists && stats.isDirectory();
    if (exists && isDirectory) {
        fs.mkdirSync(dest);
        fs.readdirSync(src).forEach(function (childItemName) {
            copyRecursiveSync(path.join(src, childItemName),
                path.join(dest, childItemName));
        });
    } else {
        fs.linkSync(src, dest);
    }
}