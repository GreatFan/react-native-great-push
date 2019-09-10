## API Reference

The GreatPush plugin is made up of two components:

1. A JavaScript module, which can be imported/required, and allows the app to interact with the service during runtime (e.g. check for updates, inspect the metadata about the currently running app update).

2. A native API (Objective-C and Java) which allows the React Native app host to bootstrap itself with the right JS bundle location.

The following sections describe the shape and behavior of these APIs in detail:

### JavaScript API Reference

When you require `react-native-great-push`, the module object provides the following top-level methods in addition to the root-level [component decorator](#greatpush):

* [allowRestart](#greatpushallowrestart): Re-allows programmatic restarts to occur as a result of an update being installed, and optionally, immediately restarts the app if a pending update had attempted to restart the app while restarts were disallowed. This is an advanced API and is only necessary if your app explicitly disallowed restarts via the `disallowRestart` method.

* [checkForUpdate](#greatpushcheckforupdate): Asks the GreatPush service whether the configured app deployment has an update available.

* [disallowRestart](#greatpushdisallowrestart): Temporarily disallows any programmatic restarts to occur as a result of a GreatPush update being installed. This is an advanced API, and is useful when a component within your app (e.g. an onboarding process) needs to ensure that no end-user interruptions can occur during its lifetime.

* [getCurrentPackage](#greatpushgetcurrentpackage): Retrieves the metadata about the currently installed update (e.g. description, installation time, size). *NOTE: As of `v1.10.3-beta` of the GreatPush module, this method is deprecated in favor of [`getUpdateMetadata`](#greatpushgetupdatemetadata)*.

* [getUpdateMetadata](#greatpushgetupdatemetadata): Retrieves the metadata for an installed update (e.g. description, mandatory).

* [notifyAppReady](#greatpushnotifyappready): Notifies the GreatPush runtime that an installed update is considered successful. If you are manually checking for and installing updates (i.e. not using the [sync](#greatpushsync) method to handle it all for you), then this method **MUST** be called; otherwise GreatPush will treat the update as failed and rollback to the previous version when the app next restarts.

* [restartApp](#greatpushrestartapp): Immediately restarts the app. If there is an update pending, it will be immediately displayed to the end user. Otherwise, calling this method simply has the same behavior as the end user killing and restarting the process.

* [sync](#greatpushsync): Allows checking for an update, downloading it and installing it, all with a single call. Unless you need custom UI and/or behavior, we recommend most developers to use this method when integrating GreatPush into their apps

* [clearUpdates](#clearupdates): Clear all downloaded GreatPush updates. This is useful when switching to a different deployment which may have an older release than the current package. 
   
    _Note: we don’t recommend to use this method in scenarios other than that (GreatPush will call this method automatically when needed in other cases) as it could lead to unpredictable behavior._

#### greatPush

```javascript
// Wrapper function
greatPush(rootComponent: React.Component): React.Component;
greatPush(options: GreatPushOptions)(rootComponent: React.Component): React.Component;
```
```javascript
// Decorator; Requires ES7 support
@greatPush
@greatPush(options: GreatPushOptions)
```

Used to wrap a React component inside a "higher order" React component that knows how to synchronize your app's JavaScript bundle and image assets when it is mounted. Internally, the higher-order component calls [`sync`](#greatpushsync) inside its `componentDidMount` lifecycle handle, which in turns performs an update check, downloads the update if it exists and installs the update for you.

This decorator provides support for letting you customize its behaviour to easily enable apps with different requirements. Below are some examples of ways you can use it (you can pick one or even use a combination):

1. **Silent sync on app start** *(the simplest, default behavior)*. Your app will automatically download available updates, and apply them the next time the app restarts (e.g. the OS or end user killed it, or the device was restarted). This way, the entire update experience is "silent" to the end user, since they don't see any update prompt and/or "synthetic" app restarts.

    ```javascript
    // Fully silent update which keeps the app in
    // sync with the server, without ever
    // interrupting the end user
    class MyApp extends Component<{}> {}
    MyApp = greatPush(MyApp);
    export default MyApp;
    ```

2. **Silent sync everytime the app resumes**. Same as 1, except we check for updates, or apply an update if one exists every time the app returns to the foreground after being "backgrounded".

    ```javascript
    // Sync for updates everytime the app resumes.
    class MyApp extends Component<{}> {}
    MyApp = greatPush({ checkFrequency: greatPush.CheckFrequency.ON_APP_RESUME, installMode: greatPush.InstallMode.ON_NEXT_RESUME })(MyApp);
    export default MyApp;
    ```

3. **Interactive**. When an update is available, prompt the end user for permission before downloading it, and then immediately apply the update. If an update was released using the `mandatory` flag, the end user would still be notified about the update, but they wouldn't have the choice to ignore it.

    ```javascript
    // Active update, which lets the end user know
    // about each update, and displays it to them
    // immediately after downloading it
    class MyApp extends Component<{}> {}
    MyApp = greatPush({ updateDialog: true, installMode: greatPush.InstallMode.IMMEDIATE })(MyApp);
    export default MyApp;
    ```

4. **Log/display progress**. While the app is syncing with the server for updates, make use of the `greatPushStatusDidChange` and/or `greatPushDownloadDidProgress` event hooks to log down the different stages of this process, or even display a progress bar to the user.

    ```javascript
    // Make use of the event hooks to keep track of
    // the different stages of the sync process.
    class MyApp extends Component<{}> {
        greatPushStatusDidChange(status) {
            switch(status) {
                case greatPush.SyncStatus.CHECKING_FOR_UPDATE:
                    console.log("Checking for updates.");
                    break;
                case greatPush.SyncStatus.DOWNLOADING_PACKAGE:
                    console.log("Downloading package.");
                    break;
                case greatPush.SyncStatus.INSTALLING_UPDATE:
                    console.log("Installing update.");
                    break;
                case greatPush.SyncStatus.UP_TO_DATE:
                    console.log("Up-to-date.");
                    break;
                case greatPush.SyncStatus.UPDATE_INSTALLED:
                    console.log("Update installed.");
                    break;
            }
        }

        greatPushDownloadDidProgress(progress) {
            console.log(progress.receivedBytes + " of " + progress.totalBytes + " received.");
        }
    }
    MyApp = greatPush(MyApp);
    export default MyApp;
    ```

##### GreatPushOptions

The `greatPush` decorator accepts an "options" object that allows you to customize numerous aspects of the default behavior mentioned above:

* __checkFrequency__ *(greatPush.CheckFrequency)* - Specifies when you would like to check for updates. Defaults to `greatPush.CheckFrequency.ON_APP_START`. Refer to the [`CheckFrequency`](#checkfrequency) enum reference for a description of the available options and what they do.

* __deploymentKey__ *(String)* - Specifies the deployment key you want to query for an update against. By default, this value is derived from the `Info.plist` file (iOS) and `MainActivity.java` file (Android), but this option allows you to override it from the script-side if you need to dynamically use a different deployment.

* __installMode__ *(greatPush.InstallMode)* - Specifies when you would like to install optional updates (i.e. those that aren't marked as mandatory). Defaults to `greatPush.InstallMode.ON_NEXT_RESTART`. Refer to the [`InstallMode`](#installmode) enum reference for a description of the available options and what they do.

* __mandatoryInstallMode__ *(greatPush.InstallMode)* - Specifies when you would like to install updates which are marked as mandatory. Defaults to `greatPush.InstallMode.IMMEDIATE`. Refer to the [`InstallMode`](#installmode) enum reference for a description of the available options and what they do.

* __minimumBackgroundDuration__ *(Number)* - Specifies the minimum number of seconds that the app needs to have been in the background before restarting the app. This property only applies to updates which are installed using `InstallMode.ON_NEXT_RESUME` or `InstallMode.ON_NEXT_SUSPEND`, and can be useful for getting your update in front of end users sooner, without being too obtrusive. Defaults to `0`, which has the effect of applying the update immediately after a resume or unless the app suspension is long enough to not matter, regardless how long it was in the background.

* __updateDialog__ *(UpdateDialogOptions)* - An "options" object used to determine whether a confirmation dialog should be displayed to the end user when an update is available, and if so, what strings to use. Defaults to `null`, which has the effect of disabling the dialog completely. Setting this to any truthy value will enable the dialog with the default strings, and passing an object to this parameter allows enabling the dialog as well as overriding one or more of the default strings. Before enabling this option within an App Store-distributed app, please refer to [this note](https://github.com/Microsoft/react-native-great-push#user-content-apple-note).

    The following list represents the available options and their defaults:

    * __appendReleaseDescription__ *(Boolean)* - Indicates whether you would like to append the description of an available release to the notification message which is displayed to the end user. Defaults to `false`.

    * __descriptionPrefix__ *(String)* - Indicates the string you would like to prefix the release description with, if any, when displaying the update notification to the end user. Defaults to `" Description: "`

    * __mandatoryContinueButtonLabel__ *(String)* - The text to use for the button the end user must press in order to install a mandatory update. Defaults to `"Continue"`.

    * __mandatoryUpdateMessage__ *(String)* - The text used as the body of an update notification, when the update is specified as mandatory. Defaults to `"An update is available that must be installed."`.

    * __optionalIgnoreButtonLabel__ *(String)* - The text to use for the button the end user can press in order to ignore an optional update that is available. Defaults to `"Ignore"`.

    * __optionalInstallButtonLabel__ *(String)* - The text to use for the button the end user can press in order to install an optional update. Defaults to `"Install"`.

    * __optionalUpdateMessage__ *(String)* - The text used as the body of an update notification, when the update is optional. Defaults to `"An update is available. Would you like to install it?"`.

    * __title__ *(String)* - The text used as the header of an update notification that is displayed to the end user. Defaults to `"Update available"`.

##### greatPushStatusDidChange (event hook)

Called when the sync process moves from one stage to another in the overall update process. The event hook is called with a status code which represents the current state, and can be any of the [`SyncStatus`](#syncstatus) values.

##### greatPushDownloadDidProgress (event hook)

Called periodically when an available update is being downloaded from the GreatPush server. The method is called with a `DownloadProgress` object, which contains the following two properties:

* __totalBytes__ *(Number)* - The total number of bytes expected to be received for this update (i.e. the size of the set of files which changed from the previous release).

* __receivedBytes__ *(Number)* - The number of bytes downloaded thus far, which can be used to track download progress.

#### greatPush.allowRestart

```javascript
greatPush.allowRestart(): void;
```

Re-allows programmatic restarts to occur, that would have otherwise been rejected due to a previous call to `disallowRestart`. If `disallowRestart` was never called in the first place, then calling this method will simply result in a no-op.

If a GreatPush update is currently pending, which attempted to restart the app (e.g. it used `InstallMode.IMMEDIATE`), but was blocked due to `disallowRestart` having been called, then calling `allowRestart` will result in an immediate restart. This allows the update to be applied as soon as possible, without interrupting the end user during critical workflows (e.g. an onboarding process).

For example, calling `allowRestart` would trigger an immediate restart if either of the three scenarios mentioned in the [`disallowRestart` docs](#greatpushdisallowrestart) occured after `disallowRestart` was called. However, calling `allowRestart` wouldn't trigger a restart if the following were true:

1. No GreatPush updates were installed since the last time `disallowRestart` was called, and therefore, there isn't any need to restart anyways.

2. There is currently a pending GreatPush update, but it was installed via `InstallMode.ON_NEXT_RESTART`, and therefore, doesn't require a programmatic restart.

3. There is currently a pending GreatPush update, but it was installed via `InstallMode.ON_NEXT_RESUME` and the app hasn't been put into the background yet, and therefore, there isn't a need to programmatically restart yet.

4. No calls to `restartApp` were made since the last time `disallowRestart` was called.

This behavior ensures that no restarts will be triggered as a result of calling `allowRestart` unless one was explictly requested during the disallowed period. In this way, `allowRestart` is somewhat similar to calling `restartApp(true)`, except the former will only trigger a restart if the currently pending update wanted to restart, whereas the later would restart as long as an update is pending.

See [disallowRestart](#greatpushdisallowrestart) for an example of how this method can be used.

#### greatPush.checkForUpdate

```javascript
greatPush.checkForUpdate(deploymentKey: String = null, handleBinaryVersionMismatchCallback: (update: RemotePackage) => void): Promise<RemotePackage>;
```

Queries the GreatPush service to see whether the configured app deployment has an update available. By default, it will use the deployment key that is configured in your `Info.plist` file (iOS), or `MainActivity.java` file (Android), but you can override that by specifying a value via the optional `deploymentKey` parameter. This can be useful when you want to dynamically "redirect" a user to a specific deployment, such as allowing "early access" via an easter egg or a user setting switch.

Second optional parameter `handleBinaryVersionMismatchCallback` is an optional callback function that can be used to notify user if there are any binary update.
E.g. consider a use-case where currently installed binary version is 1.0.1 with label(greatpush label) v1. Later native code was changed in the dev cycle and binary version was updated to 1.0.2. When great-push update check is triggered we ignore updates having binary version mismatch (because the update is not targeting to the binary version of currently installed app). In this case installed app (1.0.1) will ignore the update targeting version 1.0.2. You can use `handleBinaryVersionMismatchCallback` to provide a hook to handle such situations.

**NOTE:**
Be cautious to use Alerts within this callback if you are developing iOS application, due to [App Store](https://developer.apple.com/app-store/review/guidelines/) review process: 
> Apps must not force users to rate the app, review the app, download other apps, or other similar actions in order to access functionality, content, or use of the app.

This method returns a `Promise` which resolves to one of two possible values:

1. `null` if there is no update available. This can occur in the following scenarios:

    1. The configured deployment doesn't contain any releases, and therefore, nothing to update.
    2. The latest release within the configured deployment is targeting a different binary version than what you're currently running (either older or newer).
    3. The currently running app already has the latest release from the configured deployment, and therefore, doesn't need it again.
    4. The latest release within the configured deployment is currently marked as disabled, and therefore, isn't allowed to be downloaded.
    5. The latest release within the configured deployment is in an "active rollout" state, and the requesting device doesn't fall within the percentage of users who are eligible for it.

2. A [`RemotePackage`](#remotepackage) instance which represents an available update that can be inspected and/or subsequently downloaded.

Example Usage:

```javascript
greatPush.checkForUpdate()
.then((update) => {
    if (!update) {
        console.log("The app is up to date!");
    } else {
        console.log("An update is available! Should we download it?");
    }
});
```

#### greatPush.disallowRestart

```javascript
greatPush.disallowRestart(): void;
```

Temporarily disallows programmatic restarts to occur as a result of either of following scenarios:

1. A GreatPush update is installed using `InstallMode.IMMEDIATE`
2. A GreatPush update is installed using `InstallMode.ON_NEXT_RESUME` and the app is resumed from the background (optionally being throttled by the `minimumBackgroundDuration` property)
3. The `restartApp` method was called

*NOTE: #1 and #2 effectively work by calling `restartApp` for you, so you can think of `disallowRestart` as blocking any call to `restartApp`, regardless if your app calls it directly or indirectly.*

After calling this method, any calls to `sync` would still be allowed to check for an update, download it and install it, but an attempt to restart the app would be queued until `allowRestart` is called. This way, the restart request is captured and can be "flushed" whenever you want to allow it to occur.

This is an advanced API, and is primarily useful when individual components within your app (e.g. an onboarding process) need to ensure that no end-user interruptions can occur during their lifetime, while continuing to allow the app to keep syncing with the GreatPush server at its own pace and using whatever install modes are appropriate. This has the benefit of allowing the app to discover and download available updates as soon as possible, while also preventing any disruptions during key end-user experiences.

As an alternative, you could also choose to simply use `InstallMode.ON_NEXT_RESTART` whenever calling `sync` (which will never attempt to programmatically restart the app), and then explicity calling `restartApp` at points in your app that you know it is "safe" to do so. `disallowRestart` provides an alternative approach to this when the code that synchronizes with the GreatPush server is separate from the code/components that want to enforce a no-restart policy.

Example Usage:

```javascript
class OnboardingProcess extends Component {
    ...

    componentWillMount() {
        // Ensure that any GreatPush updates which are
        // synchronized in the background can't trigger
        // a restart while this component is mounted.
        greatPush.disallowRestart();
    }

    componentWillUnmount() {
        // Reallow restarts, and optionally trigger
        // a restart if one was currently pending.
        greatPush.allowRestart();
    }

    ...
}
```

#### greatPush.getCurrentPackage

*NOTE: This method is considered deprecated as of `v1.10.3-beta` of the GreatPush module. If you're running this version (or newer), we would recommend using the [`greatPush.getUpdateMetadata`](#greatpushgetupdatemetadata) instead, since it has more predictable behavior.*

```javascript
greatPush.getCurrentPackage(): Promise<LocalPackage>;
```

Retrieves the metadata about the currently installed "package" (e.g. description, installation time). This can be useful for scenarios such as displaying a "what's new?" dialog after an update has been applied or checking whether there is a pending update that is waiting to be applied via a resume or restart.

This method returns a `Promise` which resolves to one of two possible values:

1. `null` if the app is currently running the JS bundle from the binary and not a GreatPush update. This occurs in the following scenarios:

    1. The end-user installed the app binary and has yet to install a GreatPush update
    1. The end-user installed an update of the binary (e.g. from the store), which cleared away the old GreatPush updates, and gave precedence back to the JS binary in the binary.

2. A [`LocalPackage`](#localpackage) instance which represents the metadata for the currently running GreatPush update.

Example Usage:

```javascript
greatPush.getCurrentPackage()
.then((update) => {
    // If the current app "session" represents the first time
    // this update has run, and it had a description provided
    // with it upon release, let's show it to the end user
    if (update.isFirstRun && update.description) {
        // Display a "what's new?" modal
    }
});
```

#### greatPush.getUpdateMetadata

```javascript
greatPush.getUpdateMetadata(updateState: UpdateState = UpdateState.RUNNING): Promise<LocalPackage>;
```

Retrieves the metadata for an installed update (e.g. description, mandatory) whose state matches the specified `updateState` parameter. This can be useful for scenarios such as displaying a "what's new?" dialog after an update has been applied or checking whether there is a pending update that is waiting to be applied via a resume or restart. For more details about the possible update states, and what they represent, refer to the [UpdateState reference](#updatestate).

This method returns a `Promise` which resolves to one of two possible values:

1. `null` if an update with the specified state doesn't currently exist. This occurs in the following scenarios:

    1. The end-user hasn't installed any GreatPush updates yet, and therefore, no metadata is available for any updates, regardless what you specify as the `updateState` parameter.

    2. The end-user installed an update of the binary (e.g. from the store), which cleared away the old GreatPush updates, and gave precedence back to the JS binary in the binary. Therefore, it would exhibit the same behavior as #1

    3. The `updateState` parameter is set to `UpdateState.RUNNING`, but the app isn't currently running a GreatPush update. There may be a pending update, but the app hasn't been restarted yet in order to make it active.

    4. The `updateState` parameter is set to `UpdateState.PENDING`, but the app doesn't have any currently pending updates.

2. A [`LocalPackage`](#localpackage) instance which represents the metadata for the currently requested GreatPush update (either the running or pending).

Example Usage:

```javascript
// Check if there is currently a GreatPush update running, and if
// so, register it with the HockeyApp SDK (https://github.com/slowpath/react-native-hockeyapp)
// so that crash reports will correctly display the JS bundle version the user was running.
greatPush.getUpdateMetadata().then((update) => {
    if (update) {
        hockeyApp.addMetadata({ GreatPushRelease: update.label });
    }
});

// Check to see if there is still an update pending.
greatPush.getUpdateMetadata(UpdateState.PENDING).then((update) => {
    if (update) {
        // There's a pending update, do we want to force a restart?
    }
});
```

#### greatPush.notifyAppReady

```javascript
greatPush.notifyAppReady(): Promise<void>;
```

Notifies the GreatPush runtime that a freshly installed update should be considered successful, and therefore, an automatic client-side rollback isn't necessary. It is mandatory to call this function somewhere in the code of the updated bundle. Otherwise, when the app next restarts, the GreatPush runtime will assume that the installed update has failed and roll back to the previous version. This behavior exists to help ensure that your end users aren't blocked by a broken update.

If you are using the `sync` function, and doing your update check on app start, then you don't need to manually call `notifyAppReady` since `sync` will call it for you. This behavior exists due to the assumption that the point at which `sync` is called in your app represents a good approximation of a successful startup.

*NOTE: This method is also aliased as `notifyApplicationReady` (for backwards compatibility).*

#### greatPush.restartApp

```javascript
greatPush.restartApp(onlyIfUpdateIsPending: Boolean = false): void;
```

Immediately restarts the app. If a truthy value is provided to the `onlyIfUpdateIsPending` parameter, then the app will only restart if there is actually a pending update waiting to be applied.

This method is for advanced scenarios, and is primarily useful when the following conditions are true:

1. Your app is specifying an install mode value of `ON_NEXT_RESTART` or `ON_NEXT_RESUME` when calling the `sync` or `LocalPackage.install` methods. This has the effect of not applying your update until the app has been restarted (by either the end user or OS)	or resumed, and therefore, the update won't be immediately displayed to the end user.

2. You have an app-specific user event (e.g. the end user navigated back to the app's home route) that allows you to apply the update in an unobtrusive way, and potentially gets the update in front of the end user sooner then waiting until the next restart or resume.

#### greatPush.sync

```javascript
greatPush.sync(options: Object, syncStatusChangeCallback: function(syncStatus: Number), downloadProgressCallback: function(progress: DownloadProgress), handleBinaryVersionMismatchCallback: function(update: RemotePackage)): Promise<Number>;
```

Synchronizes your app's JavaScript bundle and image assets with the latest release to the configured deployment. Unlike the [checkForUpdate](#greatpushcheckforupdate) method, which simply checks for the presence of an update, and let's you control what to do next, `sync` handles the update check, download and installation experience for you.

This method provides support for two different (but customizable) "modes" to easily enable apps with different requirements:

1. **Silent mode** *(the default behavior)*, which automatically downloads available updates, and applies them the next time the app restarts (e.g. the OS or end user killed it, or the device was restarted). This way, the entire update experience is "silent" to the end user, since they don't see any update prompt and/or "synthetic" app restarts.

2. **Active mode**, which when an update is available, prompts the end user for permission before downloading it, and then immediately applies the update. If an update was released using the `mandatory` flag, the end user would still be notified about the update, but they wouldn't have the choice to ignore it.

Example Usage:

```javascript
// Fully silent update which keeps the app in
// sync with the server, without ever
// interrupting the end user
greatPush.sync();

// Active update, which lets the end user know
// about each update, and displays it to them
// immediately after downloading it
greatPush.sync({ updateDialog: true, installMode: greatPush.InstallMode.IMMEDIATE });
```

*Note: If you want to decide whether you check and/or download an available update based on the end user's device battery level, network conditions, etc. then simply wrap the call to `sync` in a condition that ensures you only call it when desired.*

##### SyncOptions

While the `sync` method tries to make it easy to perform silent and active updates with little configuration, it accepts an "options" object that allows you to customize numerous aspects of the default behavior mentioned above. The options available are identical to the [GreatPushOptions](#greatpushoptions), with the exception of the `checkFrequency` option:

* __deploymentKey__ *(String)* - Refer to [`GreatPushOptions`](#greatpushoptions).

* __installMode__ *(greatPush.InstallMode)* - Refer to [`GreatPushOptions`](#greatpushoptions).

* __mandatoryInstallMode__ *(greatPush.InstallMode)* - Refer to [`GreatPushOptions`](#greatpushoptions).

* __minimumBackgroundDuration__ *(Number)* - Refer to [`GreatPushOptions`](#greatpushoptions).

* __updateDialog__ *(UpdateDialogOptions)* - Refer to [`GreatPushOptions`](#greatpushoptions).

Example Usage:

```javascript
// Use a different deployment key for this
// specific call, instead of the one configured
// in the Info.plist file
greatPush.sync({ deploymentKey: "KEY" });

// Download the update silently, but install it on
// the next resume, as long as at least 5 minutes
// has passed since the app was put into the background.
greatPush.sync({ installMode: greatPush.InstallMode.ON_NEXT_RESUME, minimumBackgroundDuration: 60 * 5 });

// Download the update silently, and install optional updates
// on the next restart, but install mandatory updates on the next resume.
greatPush.sync({ mandatoryInstallMode: greatPush.InstallMode.ON_NEXT_RESUME });

// Changing the title displayed in the
// confirmation dialog of an "active" update
greatPush.sync({ updateDialog: { title: "An update is available!" } });

// Displaying an update prompt which includes the
// description associated with the GreatPush release
greatPush.sync({
   updateDialog: {
    appendReleaseDescription: true,
    descriptionPrefix: "\n\nChange log:\n"
   },
   installMode: greatPush.InstallMode.IMMEDIATE
});
```

In addition to the options, the `sync` method also accepts several optional function parameters which allow you to subscribe to the lifecycle of the `sync` "pipeline" in order to display additional UI as needed (e.g. a "checking for update modal or a download progress modal):

* __syncStatusChangedCallback__ *((syncStatus: Number) => void)* - Called when the sync process moves from one stage to another in the overall update process. The method is called with a status code which represents the current state, and can be any of the [`SyncStatus`](#syncstatus) values.

* __downloadProgressCallback__ *((progress: DownloadProgress) => void)* - Called periodically when an available update is being downloaded from the GreatPush server. The method is called with a `DownloadProgress` object, which contains the following two properties:

    * __totalBytes__ *(Number)* - The total number of bytes expected to be received for this update (i.e. the size of the set of files which changed from the previous release).

    * __receivedBytes__ *(Number)* - The number of bytes downloaded thus far, which can be used to track download progress.

* __handleBinaryVersionMismatchCallback__ *((update: RemotePackage) => void)* - 
Called when there are any binary update available. The method is called with a [`RemotePackage`](#remotepackage) object. Refer to [greatPush.checkForUpdate](#greatpushcheckforupdate) section for more details.

Example Usage:

```javascript
// Prompt the user when an update is available
// and then display a "downloading" modal
greatPush.sync({ updateDialog: true },
  (status) => {
      switch (status) {
          case greatPush.SyncStatus.DOWNLOADING_PACKAGE:
              // Show "downloading" modal
              break;
          case greatPush.SyncStatus.INSTALLING_UPDATE:
              // Hide "downloading" modal
              break;
      }
  },
  ({ receivedBytes, totalBytes, }) => {
    /* Update download modal progress */
  }
);
```

This method returns a `Promise` which is resolved to a `SyncStatus` code that indicates why the `sync` call succeeded. This code can be one of the following `SyncStatus` values:

* __greatPush.SyncStatus.UP_TO_DATE__ *(0)* - The app is up-to-date with the GreatPush server.

* __greatPush.SyncStatus.UPDATE_IGNORED__ *(2)* - The app had an optional update which the end user chose to ignore. (This is only applicable when the `updateDialog` is used)

* __greatPush.SyncStatus.UPDATE_INSTALLED__ *(1)* - The update has been installed and will be run either immediately after the `syncStatusChangedCallback` function returns or the next time the app resumes/restarts, depending on the `InstallMode` specified in `SyncOptions`.

* __greatPush.SyncStatus.SYNC_IN_PROGRESS__ *(4)* - There is an ongoing `sync` operation running which prevents the current call from being executed.

The `sync` method can be called anywhere you'd like to check for an update. That could be in the `componentWillMount` lifecycle event of your root component, the onPress handler of a `<TouchableHighlight>` component, in the callback of a periodic timer, or whatever else makes sense for your needs. Just like the `checkForUpdate` method, it will perform the network request to check for an update in the background, so it won't impact your UI thread and/or JavaScript thread's responsiveness.

#### Package objects

The `checkForUpdate` and `getUpdateMetadata` methods return `Promise` objects, that when resolved, provide acces to "package" objects. The package represents your code update as well as any extra metadata (e.g. description, mandatory?). The GreatPush API has the distinction between the following types of packages:

* [LocalPackage](#localpackage): Represents a downloaded update that is either already running, or has been installed and is pending an app restart.

* [RemotePackage](#remotepackage): Represents an available update on the GreatPush server that hasn't been downloaded yet.

##### LocalPackage

Contains details about an update that has been downloaded locally or already installed. You can get a reference to an instance of this object either by calling the module-level `getUpdateMetadata` method, or as the value of the promise returned by the `RemotePackage.download` method.

###### Properties
- __appVersion__: The app binary version that this update is dependent on. This is the value that was specified via the `appStoreVersion` parameter when calling the CLI's `release` command. *(String)*
- __deploymentKey__: The deployment key that was used to originally download this update. *(String)*
- __description__: The description of the update. This is the same value that you specified in the CLI when you released the update. *(String)*
- __failedInstall__: Indicates whether this update has been previously installed but was rolled back. The `sync` method will automatically ignore updates which have previously failed, so you only need to worry about this property if using `checkForUpdate`. *(Boolean)*
- __isFirstRun__: Indicates whether this is the first time the update has been run after being installed. This is useful for determining whether you would like to show a "What's New?" UI to the end user after installing an update. *(Boolean)*
- __isMandatory__: Indicates whether the update is considered mandatory.  This is the value that was specified in the CLI when the update was released. *(Boolean)*
- __isPending__: Indicates whether this update is in a "pending" state. When `true`, that means the update has been downloaded and installed, but the app restart needed to apply it hasn't occurred yet, and therefore, it's changes aren't currently visible to the end-user. *(Boolean)*
- __label__: The internal label automatically given to the update by the GreatPush server, such as `v5`. This value uniquely identifies the update within it's deployment. *(String)*
- __packageHash__: The SHA hash value of the update. *(String)*
- __packageSize__: The size of the code contained within the update, in bytes. *(Number)*

###### Methods

- __install(installMode: greatPush.InstallMode = greatPush.InstallMode.ON_NEXT_RESTART, minimumBackgroundDuration = 0): Promise&lt;void&gt;__: Installs the update by saving it to the location on disk where the runtime expects to find the latest version of the app. The `installMode` parameter controls when the changes are actually presented to the end user. The default value is to wait until the next app restart to display the changes, but you can refer to the [`InstallMode`](#installmode) enum reference for a description of the available options and what they do. If the `installMode` parameter is set to `InstallMode.ON_NEXT_RESUME`, then the `minimumBackgroundDuration` parameter allows you to control how long the app must have been in the background before forcing the install after it is resumed.

##### RemotePackage

Contains details about an update that is available for download from the GreatPush server. You get a reference to an instance of this object by calling the `checkForUpdate` method when an update is available. If you are using the `sync` API, you don't need to worry about the `RemotePackage`, since it will handle the download and installation process automatically for you.

###### Properties

The `RemotePackage` inherits all of the same properties as the `LocalPackage`, but includes one additional one:

- __downloadUrl__: The URL at which the package is available for download. This property is only needed for advanced usage, since the `download` method will automatically handle the acquisition of updates for you. *(String)*

###### Methods

- __download(downloadProgressCallback?: Function): Promise&lt;LocalPackage&gt;__: Downloads the available update from the GreatPush service. If a `downloadProgressCallback` is specified, it will be called periodically with a `DownloadProgress` object (`{ totalBytes: Number, receivedBytes: Number }`) that reports the progress of the download until it completes. Returns a Promise that resolves with the `LocalPackage`.

#### Enums

The GreatPush API includes the following enums which can be used to customize the update experience:

##### InstallMode

This enum specifies when you would like an installed update to actually be applied, and can be passed to either the `sync` or `LocalPackage.install` methods. It includes the following values:

* __greatPush.InstallMode.IMMEDIATE__ *(0)* - Indicates that you want to install the update and restart the app immediately. This value is appropriate for debugging scenarios as well as when displaying an update prompt to the user, since they would expect to see the changes immediately after accepting the installation. Additionally, this mode can be used to enforce mandatory updates, since it removes the potentially undesired latency between the update installation and the next time the end user restarts or resumes the app.

* __greatPush.InstallMode.ON_NEXT_RESTART__ *(1)* - Indicates that you want to install the update, but not forcibly restart the app. When the app is "naturally" restarted (due the OS or end user killing it), the update will be seamlessly picked up. This value is appropriate when performing silent updates, since it would likely be disruptive to the end user if the app suddenly restarted out of nowhere, since they wouldn't have realized an update was even downloaded. This is the default mode used for both the `sync` and `LocalPackage.install` methods.

* __greatPush.InstallMode.ON_NEXT_RESUME__ *(2)* - Indicates that you want to install the update, but don't want to restart the app until the next time the end user resumes it from the background. This way, you don't disrupt their current session, but you can get the update in front of them sooner then having to wait for the next natural restart. This value is appropriate for silent installs that can be applied on resume in a non-invasive way.

* __greatPush.InstallMode.ON_NEXT_SUSPEND__ *(3)* - Indicates that you want to install the update _while_ it is in the background, but only after it has been in the background for `minimumBackgroundDuration` seconds (0 by default), so that user context isn't lost unless the app suspension is long enough to not matter.

##### CheckFrequency

This enum specifies when you would like your app to sync with the server for updates, and can be passed to the `greatPushify` decorator. It includes the following values:

* __greatPush.CheckFrequency.ON_APP_START__ *(0)* - Indicates that you want to check for updates whenever the app's process is started.

* __greatPush.CheckFrequency.ON_APP_RESUME__ *(1)* - Indicates that you want to check for updates whenever the app is brought back to the foreground after being "backgrounded" (user pressed the home button, app launches a seperate payment process, etc).

* __greatPush.CheckFrequency.MANUAL__ *(2)* - Disable automatic checking for updates, but only check when [`greatPush.sync()`](#greatpushsync) is called in app code.

##### SyncStatus

This enum is provided to the `syncStatusChangedCallback` function that can be passed to the `sync` method, in order to hook into the overall update process. It includes the following values:

* __greatPush.SyncStatus.UP_TO_DATE__ *(0)* - The app is fully up-to-date with the configured deployment.
* __greatPush.SyncStatus.UPDATE_INSTALLED__ *(1)* - An available update has been installed and will be run either immediately after the `syncStatusChangedCallback` function returns or the next time the app resumes/restarts, depending on the `InstallMode` specified in `SyncOptions`.
* __greatPush.SyncStatus.UPDATE_IGNORED__ *(2)* - The app has an optional update, which the end user chose to ignore. (This is only applicable when the `updateDialog` is used)
* __greatPush.SyncStatus.UNKNOWN_ERROR__ *(3)* - The sync operation encountered an unknown error.
* __greatPush.SyncStatus.SYNC_IN_PROGRESS__ *(4)* - There is an ongoing `sync` operation running which prevents the current call from being executed.
* __greatPush.SyncStatus.CHECKING_FOR_UPDATE__ *(5)* - The GreatPush server is being queried for an update.
* __greatPush.SyncStatus.AWAITING_USER_ACTION__ *(6)* - An update is available, and a confirmation dialog was shown to the end user. (This is only applicable when the `updateDialog` is used)
* __greatPush.SyncStatus.DOWNLOADING_PACKAGE__ *(7)* - An available update is being downloaded from the GreatPush server.
* __greatPush.SyncStatus.INSTALLING_UPDATE__ *(8)* - An available update was downloaded and is about to be installed.

##### UpdateState

This enum specifies the state that an update is currently in, and can be specified when calling the `getUpdateMetadata` method. It includes the following values:

* __greatPush.UpdateState.RUNNING__ *(0)* - Indicates that an update represents the version of the app that is currently running. This can be useful for identifying attributes about the app, for scenarios such as displaying the release description in a "what's new?" dialog or reporting the latest version to an analytics and/or crash reporting service.

* __greatPush.UpdateState.PENDING__ *(1)* - Indicates than an update has been installed, but the app hasn't been restarted yet in order to apply it. This can be useful for determining whether there is a pending update, which you may want to force a programmatic restart (via `restartApp`) in order to apply.

* __greatPush.UpdateState.LATEST__ *(2)* - Indicates than an update represents the latest available release, and can be either currently running or pending.
