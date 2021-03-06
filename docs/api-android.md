### Java API Reference (Android)

The Java API is made available by importing the `com.microsoft.greatpush.react.GreatPush` class into your `MainActivity.java` file, and consists of a single public class named `GreatPush`.

#### GreatPush

Constructs the GreatPush client runtime and represents the `ReactPackage` instance that you add to you app's list of packages.

##### Constructors

- __GreatPush(String deploymentKey, Activity mainActivity)__ - Creates a new instance of the GreatPush runtime, that will be used to query the service for updates via the provided deployment key. The `mainActivity` parameter should always be set to `this` when configuring your React packages list inside the `MainActivity` class. This constructor puts the GreatPush runtime into "release mode", so if you want to enable debugging behavior, use the following constructor instead.

- __GreatPush(String deploymentKey, Activity mainActivity, bool isDebugMode)__ - Equivalent to the previous constructor but allows you to specify whether you want the GreatPush runtime to be in debug mode or not. When using this constructor, the `isDebugMode` parameter should always be set to `BuildConfig.DEBUG` in order to stay synchronized with your build type. When putting GreatPush into debug mode, the following behaviors are enabled:

    1. Old GreatPush updates aren't deleted from storage whenever a new binary is deployed to the emulator/device. This behavior enables you to deploy new binaries, without bumping the version during development, and without continuously getting the same update every time your app calls `sync`.

    2. The local cache that the React Native runtime maintains in debug mode is deleted whenever a GreatPush update is installed. This ensures that when the app is restarted after an update is applied, you will see the expected changes. As soon as [this PR](https://github.com/facebook/react-native/pull/4738) is merged, we won't need to do this anymore.

- __GreatPush(String deploymentKey, Context context, boolean isDebugMode, Integer publicKeyResourceDescriptor)__ - Equivalent to the previous constructor, but allows you to specify the public key resource descriptor needed to read public key content. Please refer to [Code Signing](setup-android.md#code-signing) section for more details about Code Signing Feature.

- __GreatPush(String deploymentKey, Context context, boolean isDebugMode, String serverUrl)__ Constructor allows you to specify GreatPush Server Url. The Default value: `"https://greatpush.azurewebsites.net/"` is overridden by value specfied in `serverUrl`. 

##### Builder

As an alternative to constructors *you can also use `GreatPushBuilder`* to setup a GreatPush instance configured with *only parameters you want*.

```java
    @Override
    protected List<ReactPackage> getPackages() {
      return Arrays.<ReactPackage>asList(
            new MainReactPackage(),
            new GreatPushBuilder("deployment-key-here",getApplicationContext())
                .setIsDebugMode(BuildConfig.DEBUG)
                .setPublicKeyResourceDescriptor(R.string.publicKey)
                .setServerUrl("https://yourgreatpush.server.com")
                .build() //return configured GreatPush instance
      );
    }
```

`GreatPushBuilder` methods:

* __public GreatPushBuilder(String deploymentKey, Context context)__ - setup same parameters as via __GreatPush(String deploymentKey, Activity mainActivity)__

* __public GreatPushBuilder setIsDebugMode(boolean isDebugMode)__ - allows you to specify whether you want the GreatPush runtime to be in debug mode or not. Default value: `false`.

* __public GreatPushBuilder setServerUrl(String serverUrl)__ - allows you to specify GreatPush Server Url. Default value: `"https://greatpush.azurewebsites.net/"`.

* __public GreatPushBuilder setPublicKeyResourceDescriptor(int publicKeyResourceDescriptor)__ - allows you to specify Public Key resource descriptor which will be used for reading Public Key content for `strings.xml` file. Please refer to [Code Signing](#code-signing) section for more detailed information about purpose of this parameter.

* __public GreatPush build()__ - return configured `GreatPush` instance.

##### Public Methods

- __setDeploymentKey(String deploymentKey)__ - Sets the deployment key that the app should use when querying for updates. This is a dynamic alternative to setting the deployment key in Greatpush constructor/builder and/or specifying a deployment key in JS when calling `checkForUpdate` or `sync`.

##### Static Methods

- __getBundleUrl()__ - Returns the path to the most recent version of your app's JS bundle file, assuming that the resource name is `index.android.bundle`. If your app is using a different bundle name, then use the overloaded version of this method which allows specifying it. This method has the same resolution behavior as the Objective-C equivalent described above.

- __getBundleUrl(String bundleName)__ - Returns the path to the most recent version of your app's JS bundle file, using the specified resource name (e.g. `index.android.bundle`). This method has the same resolution behavior as the Objective-C equivalent described above.

- __getPackageFolder()__ - Returns the path to the current update folder.

- __overrideAppVersion(String appVersionOverride)__ - Sets the version of the application's binary interface, which would otherwise default to the Play Store version specified as the `versionName` in the `build.gradle`. This should be called a single time, before the GreatPush instance is constructed.
