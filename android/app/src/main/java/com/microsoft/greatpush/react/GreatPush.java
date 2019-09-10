package com.microsoft.greatpush.react;

import android.content.Context;
import android.content.pm.PackageInfo;
import android.content.pm.PackageManager;
import android.content.res.Resources;
import android.support.annotation.NonNull;

import com.facebook.react.ReactInstanceManager;
import com.facebook.react.ReactPackage;
import com.facebook.react.bridge.JavaScriptModule;
import com.facebook.react.bridge.NativeModule;
import com.facebook.react.bridge.ReactApplicationContext;
import com.facebook.react.devsupport.DevInternalSettings;
import com.facebook.react.devsupport.interfaces.DevSupportManager;
import com.facebook.react.uimanager.ViewManager;

import org.json.JSONException;
import org.json.JSONObject;

import java.io.File;
import java.util.ArrayList;
import java.util.List;

public class GreatPush implements ReactPackage {

    private static boolean sIsRunningBinaryVersion = false;
    private static boolean sNeedToReportRollback = false;
    private static boolean sTestConfigurationFlag = false;
    private static String sAppVersion = null;

    private boolean mDidUpdate = false;

    private String mAssetsBundleFileName;

    // Helper classes.
    private GreatPushUpdateManager mUpdateManager;
    private GreatPushTelemetryManager mTelemetryManager;
    private SettingsManager mSettingsManager;

    // Config properties.
    private String mDeploymentKey;
    private static String mServerUrl = "https://greatpush.azurewebsites.net/";

    private Context mContext;
    private final boolean mIsDebugMode;

    private static String mPublicKey;

    private static ReactInstanceHolder mReactInstanceHolder;
    private static GreatPush mCurrentInstance;

    public GreatPush(String deploymentKey, Context context) {
        this(deploymentKey, context, false);
    }

    public static String getServiceUrl() {
        return mServerUrl;
    }

    public GreatPush(String deploymentKey, Context context, boolean isDebugMode) {
        mContext = context.getApplicationContext();

        mUpdateManager = new GreatPushUpdateManager(context.getFilesDir().getAbsolutePath());
        mTelemetryManager = new GreatPushTelemetryManager(mContext);
        mDeploymentKey = deploymentKey;
        mIsDebugMode = isDebugMode;
        mSettingsManager = new SettingsManager(mContext);

        if (sAppVersion == null) {
            try {
                PackageInfo pInfo = mContext.getPackageManager().getPackageInfo(mContext.getPackageName(), 0);
                sAppVersion = pInfo.versionName;
            } catch (PackageManager.NameNotFoundException e) {
                throw new GreatPushUnknownException("Unable to get package info for " + mContext.getPackageName(), e);
            }
        }

        mCurrentInstance = this;

        clearDebugCacheIfNeeded(null);
        initializeUpdateAfterRestart();
    }

    public GreatPush(String deploymentKey, Context context, boolean isDebugMode, @NonNull String serverUrl) {
        this(deploymentKey, context, isDebugMode);
        mServerUrl = serverUrl;
    }

    public GreatPush(String deploymentKey, Context context, boolean isDebugMode, int publicKeyResourceDescriptor) {
        this(deploymentKey, context, isDebugMode);

        mPublicKey = getPublicKeyByResourceDescriptor(publicKeyResourceDescriptor);
    }

    public GreatPush(String deploymentKey, Context context, boolean isDebugMode, @NonNull String serverUrl, Integer publicKeyResourceDescriptor) {
        this(deploymentKey, context, isDebugMode);

        if (publicKeyResourceDescriptor != null) {
            mPublicKey = getPublicKeyByResourceDescriptor(publicKeyResourceDescriptor);
        }

        mServerUrl = serverUrl;
    }

    private String getPublicKeyByResourceDescriptor(int publicKeyResourceDescriptor){
        String publicKey;
        try {
            publicKey = mContext.getString(publicKeyResourceDescriptor);
        } catch (Resources.NotFoundException e) {
            throw new GreatPushInvalidPublicKeyException(
                    "Unable to get public key, related resource descriptor " +
                            publicKeyResourceDescriptor +
                            " can not be found", e
            );
        }

        if (publicKey.isEmpty()) {
            throw new GreatPushInvalidPublicKeyException("Specified public key is empty");
        }
        return publicKey;
    }

    public void clearDebugCacheIfNeeded(ReactInstanceManager instanceManager) {
        boolean isLiveReloadEnabled = false;

        // Use instanceManager for checking if we use LiveRelaod mode. In this case we should not remove ReactNativeDevBundle.js file
        // because we get error with trying to get this after reloading. Issue: https://github.com/Microsoft/react-native-great-push/issues/1272
        if (instanceManager != null) {
            DevSupportManager devSupportManager = instanceManager.getDevSupportManager();
            if (devSupportManager != null) {
                DevInternalSettings devInternalSettings = (DevInternalSettings)devSupportManager.getDevSettings();
                isLiveReloadEnabled = devInternalSettings.isReloadOnJSChangeEnabled();
            }
        }

        if (mIsDebugMode && mSettingsManager.isPendingUpdate(null) && !isLiveReloadEnabled) {
            // This needs to be kept in sync with https://github.com/facebook/react-native/blob/master/ReactAndroid/src/main/java/com/facebook/react/devsupport/DevSupportManager.java#L78
            File cachedDevBundle = new File(mContext.getFilesDir(), "ReactNativeDevBundle.js");
            if (cachedDevBundle.exists()) {
                cachedDevBundle.delete();
            }
        }
    }

    public boolean didUpdate() {
        return mDidUpdate;
    }

    public String getAppVersion() {
        return sAppVersion;
    }

    public String getAssetsBundleFileName() {
        return mAssetsBundleFileName;
    }

    public String getPublicKey() {
        return mPublicKey;
    }

    long getBinaryResourcesModifiedTime() {
        try {
            String packageName = this.mContext.getPackageName();
            int greatPushApkBuildTimeId = this.mContext.getResources().getIdentifier(GreatPushConstants.GREAT_PUSH_APK_BUILD_TIME_KEY, "string", packageName);
            // replace double quotes needed for correct restoration of long value from strings.xml
            // https://github.com/Microsoft/cordova-plugin-great-push/issues/264
            String greatPushApkBuildTime = this.mContext.getResources().getString(greatPushApkBuildTimeId).replaceAll("\"","");
            return Long.parseLong(greatPushApkBuildTime);
        } catch (Exception e) {
            throw new GreatPushUnknownException("Error in getting binary resources modified time", e);
        }
    }

    public String getPackageFolder() {
        JSONObject greatPushLocalPackage = mUpdateManager.getCurrentPackage();
        if (greatPushLocalPackage == null) {
            return null;
        }
        return mUpdateManager.getPackageFolderPath(greatPushLocalPackage.optString("packageHash"));
    }

    @Deprecated
    public static String getBundleUrl() {
        return getJSBundleFile();
    }

    @Deprecated
    public static String getBundleUrl(String assetsBundleFileName) {
        return getJSBundleFile(assetsBundleFileName);
    }

    public Context getContext() {
        return mContext;
    }

    public String getDeploymentKey() {
        return mDeploymentKey;
    }

    public static String getJSBundleFile() {
        return GreatPush.getJSBundleFile(GreatPushConstants.DEFAULT_JS_BUNDLE_NAME);
    }

    public static String getJSBundleFile(String assetsBundleFileName) {
        if (mCurrentInstance == null) {
            throw new GreatPushNotInitializedException("A GreatPush instance has not been created yet. Have you added it to your app's list of ReactPackages?");
        }

        return mCurrentInstance.getJSBundleFileInternal(assetsBundleFileName);
    }

    public String getJSBundleFileInternal(String assetsBundleFileName) {
        this.mAssetsBundleFileName = assetsBundleFileName;
        String binaryJsBundleUrl = GreatPushConstants.ASSETS_BUNDLE_PREFIX + assetsBundleFileName;

        String packageFilePath = mUpdateManager.getCurrentPackageBundlePath(this.mAssetsBundleFileName);
        if (packageFilePath == null) {
            // There has not been any downloaded updates.
            GreatPushUtils.logBundleUrl(binaryJsBundleUrl);
            sIsRunningBinaryVersion = true;
            return binaryJsBundleUrl;
        }

        JSONObject packageMetadata = this.mUpdateManager.getCurrentPackage();
        if (isPackageBundleLatest(packageMetadata)) {
            GreatPushUtils.logBundleUrl(packageFilePath);
            sIsRunningBinaryVersion = false;
            return packageFilePath;
        } else {
            // The binary version is newer.
            this.mDidUpdate = false;
            if (!this.mIsDebugMode || hasBinaryVersionChanged(packageMetadata)) {
                this.clearUpdates();
            }

            GreatPushUtils.logBundleUrl(binaryJsBundleUrl);
            sIsRunningBinaryVersion = true;
            return binaryJsBundleUrl;
        }
    }

    public String getServerUrl() {
        return mServerUrl;
    }

    void initializeUpdateAfterRestart() {
        // Reset the state which indicates that
        // the app was just freshly updated.
        mDidUpdate = false;

        JSONObject pendingUpdate = mSettingsManager.getPendingUpdate();
        if (pendingUpdate != null) {
            JSONObject packageMetadata = this.mUpdateManager.getCurrentPackage();
            if (packageMetadata == null || !isPackageBundleLatest(packageMetadata) && hasBinaryVersionChanged(packageMetadata)) {
                GreatPushUtils.log("Skipping initializeUpdateAfterRestart(), binary version is newer");
                return;
            }

            try {
                boolean updateIsLoading = pendingUpdate.getBoolean(GreatPushConstants.PENDING_UPDATE_IS_LOADING_KEY);
                if (updateIsLoading) {
                    // Pending update was initialized, but notifyApplicationReady was not called.
                    // Therefore, deduce that it is a broken update and rollback.
                    GreatPushUtils.log("Update did not finish loading the last time, rolling back to a previous version.");
                    sNeedToReportRollback = true;
                    rollbackPackage();
                } else {
                    // There is in fact a new update running for the first
                    // time, so update the local state to ensure the client knows.
                    mDidUpdate = true;

                    // Mark that we tried to initialize the new update, so that if it crashes,
                    // we will know that we need to rollback when the app next starts.
                    mSettingsManager.savePendingUpdate(pendingUpdate.getString(GreatPushConstants.PENDING_UPDATE_HASH_KEY),
                            /* isLoading */true);
                }
            } catch (JSONException e) {
                // Should not happen.
                throw new GreatPushUnknownException("Unable to read pending update metadata stored in SharedPreferences", e);
            }
        }
    }

    void invalidateCurrentInstance() {
        mCurrentInstance = null;
    }

    boolean isDebugMode() {
        return mIsDebugMode;
    }

    boolean isRunningBinaryVersion() {
        return sIsRunningBinaryVersion;
    }

    private boolean isPackageBundleLatest(JSONObject packageMetadata) {
        try {
            Long binaryModifiedDateDuringPackageInstall = null;
            String binaryModifiedDateDuringPackageInstallString = packageMetadata.optString(GreatPushConstants.BINARY_MODIFIED_TIME_KEY, null);
            if (binaryModifiedDateDuringPackageInstallString != null) {
                binaryModifiedDateDuringPackageInstall = Long.parseLong(binaryModifiedDateDuringPackageInstallString);
            }
            String packageAppVersion = packageMetadata.optString("appVersion", null);
            long binaryResourcesModifiedTime = this.getBinaryResourcesModifiedTime();
            return binaryModifiedDateDuringPackageInstall != null &&
                    binaryModifiedDateDuringPackageInstall == binaryResourcesModifiedTime &&
                    (isUsingTestConfiguration() || sAppVersion.equals(packageAppVersion));
        } catch (NumberFormatException e) {
            throw new GreatPushUnknownException("Error in reading binary modified date from package metadata", e);
        }
    }

    private boolean hasBinaryVersionChanged(JSONObject packageMetadata) {
        String packageAppVersion = packageMetadata.optString("appVersion", null);
        return !sAppVersion.equals(packageAppVersion);
    }

    boolean needToReportRollback() {
        return sNeedToReportRollback;
    }

    public static void overrideAppVersion(String appVersionOverride) {
        sAppVersion = appVersionOverride;
    }

    private void rollbackPackage() {
        JSONObject failedPackage = mUpdateManager.getCurrentPackage();
        mSettingsManager.saveFailedUpdate(failedPackage);
        mUpdateManager.rollbackPackage();
        mSettingsManager.removePendingUpdate();
    }

    public void setNeedToReportRollback(boolean needToReportRollback) {
        GreatPush.sNeedToReportRollback = needToReportRollback;
    }

    /* The below 3 methods are used for running tests.*/
    public static boolean isUsingTestConfiguration() {
        return sTestConfigurationFlag;
    }

    public void setDeploymentKey(String deploymentKey) {
        mDeploymentKey = deploymentKey;
    }

    public static void setUsingTestConfiguration(boolean shouldUseTestConfiguration) {
        sTestConfigurationFlag = shouldUseTestConfiguration;
    }

    public void clearUpdates() {
        mUpdateManager.clearUpdates();
        mSettingsManager.removePendingUpdate();
        mSettingsManager.removeFailedUpdates();
    }

    public static void setReactInstanceHolder(ReactInstanceHolder reactInstanceHolder) {
        mReactInstanceHolder = reactInstanceHolder;
    }

    static ReactInstanceManager getReactInstanceManager() {
        if (mReactInstanceHolder == null) {
            return null;
        }
        return mReactInstanceHolder.getReactInstanceManager();
    }

    @Override
    public List<NativeModule> createNativeModules(ReactApplicationContext reactApplicationContext) {
        GreatPushNativeModule greatPushModule = new GreatPushNativeModule(reactApplicationContext, this, mUpdateManager, mTelemetryManager, mSettingsManager);
        GreatPushDialog dialogModule = new GreatPushDialog(reactApplicationContext);

        List<NativeModule> nativeModules = new ArrayList<>();
        nativeModules.add(greatPushModule);
        nativeModules.add(dialogModule);
        return nativeModules;
    }

    // Deprecated in RN v0.47.
    public List<Class<? extends JavaScriptModule>> createJSModules() {
        return new ArrayList<>();
    }

    @Override
    public List<ViewManager> createViewManagers(ReactApplicationContext reactApplicationContext) {
        return new ArrayList<>();
    }
}
