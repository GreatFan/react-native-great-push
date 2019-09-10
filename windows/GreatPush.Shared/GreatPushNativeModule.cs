﻿using Newtonsoft.Json.Linq;
using ReactNative;
using ReactNative.Bridge;
using ReactNative.Modules.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
#if WINDOWS_UWP
using Windows.Web.Http;
#else
using GreatPush.Net46.Adapters.Http;
#endif

namespace GreatPush.ReactNative
{
    internal class GreatPushNativeModule : ReactContextNativeModuleBase
    {
        private GreatPushReactPackage _greatPush;
        private MinimumBackgroundListener _minimumBackgroundListener;
        private ReactContext _reactContext;

        public GreatPushNativeModule(ReactContext reactContext, GreatPushReactPackage greatPush)
            : base(reactContext)
        {
            _reactContext = reactContext;
            _greatPush = greatPush;
        }

        public override string Name
        {
            get
            {
                return "GreatPush";
            }
        }

        public override IReadOnlyDictionary<string, object> Constants
        {
            get
            {
                return new Dictionary<string, object>
                {
                    { "greatPushInstallModeImmediate", InstallMode.Immediate },
                    { "greatPushInstallModeOnNextResume", InstallMode.OnNextResume },
                    { "greatPushInstallModeOnNextRestart", InstallMode.OnNextRestart },
                    { "greatPushUpdateStateRunning", UpdateState.Running },
                    { "greatPushUpdateStatePending", UpdateState.Pending },
                    { "greatPushUpdateStateLatest", UpdateState.Latest },
                };
            }
        }

        public override void Initialize()
        {
            _greatPush.InitializeUpdateAfterRestart();
        }

        [ReactMethod]
        public async void downloadUpdate(JObject updatePackage, bool notifyProgress, IPromise promise)
        {
            try
            {
                updatePackage[GreatPushConstants.BinaryModifiedTimeKey] = "" + await FileUtils.GetBinaryResourcesModifiedTimeAsync(_greatPush.AssetsBundleFileName).ConfigureAwait(false);
                await _greatPush.UpdateManager.DownloadPackageAsync(
                    updatePackage,
                    _greatPush.AssetsBundleFileName,
                    new Progress<HttpProgress>(
                        (HttpProgress progress) =>
                        {
                            if (!notifyProgress)
                            {
                                return;
                            }

                            var downloadProgress = new JObject()
                            {
                                { "totalBytes", progress.TotalBytesToReceive },
                                { "receivedBytes", progress.BytesReceived }
                            };

                            _reactContext
                                .GetJavaScriptModule<RCTDeviceEventEmitter>()
                                .emit(GreatPushConstants.DownloadProgressEventName, downloadProgress);
                        }
                    )
                ).ConfigureAwait(false);

                JObject newPackage = await _greatPush.UpdateManager.GetPackageAsync((string)updatePackage[GreatPushConstants.PackageHashKey]).ConfigureAwait(false);
                promise.Resolve(newPackage);
            }
            catch (InvalidDataException e)
            {
                GreatPushUtils.Log(e.ToString());
                SettingsManager.SaveFailedUpdate(updatePackage);
                promise.Reject(e);
            }
            catch (Exception e)
            {
                GreatPushUtils.Log(e.ToString());
                promise.Reject(e);
            }
        }

        [ReactMethod]
        public void getConfiguration(IPromise promise)
        {
            var config = new JObject
            {
                { "appVersion", _greatPush.AppVersion },
                { "clientUniqueId", GreatPushUtils.GetDeviceId() },
                { "deploymentKey", _greatPush.DeploymentKey },
                { "serverUrl", GreatPushConstants.GreatPushServerUrl }
            };

            // TODO generate binary hash
            // string binaryHash = GreatPushUpdateUtils.getHashForBinaryContents(mainActivity, isDebugMode);
            /*if (binaryHash != null)
            {
                configMap.putString(PACKAGE_HASH_KEY, binaryHash);
            }*/
            promise.Resolve(config);
        }

        [ReactMethod]
        public async void getUpdateMetadata(UpdateState updateState, IPromise promise)
        {
            JObject currentPackage = await _greatPush.UpdateManager.GetCurrentPackageAsync().ConfigureAwait(false);
            if (currentPackage == null)
            {
                promise.Resolve("");
                return;
            }

            var currentUpdateIsPending = false;

            if (currentPackage[GreatPushConstants.PackageHashKey] != null)
            {
                var currentHash = (string)currentPackage[GreatPushConstants.PackageHashKey];
                currentUpdateIsPending = SettingsManager.IsPendingUpdate(currentHash);
            }

            if (updateState == UpdateState.Pending && !currentUpdateIsPending)
            {
                // The caller wanted a pending update
                // but there isn't currently one.
                promise.Resolve("");
            }
            else if (updateState == UpdateState.Running && currentUpdateIsPending)
            {
                // The caller wants the running update, but the current
                // one is pending, so we need to grab the previous.
                promise.Resolve(await _greatPush.UpdateManager.GetPreviousPackageAsync().ConfigureAwait(false));
            }
            else
            {
                // The current package satisfies the request:
                // 1) Caller wanted a pending, and there is a pending update
                // 2) Caller wanted the running update, and there isn't a pending
                // 3) Caller wants the latest update, regardless if it's pending or not
                if (_greatPush.IsRunningBinaryVersion)
                {
                    // This only matters in Debug builds. Since we do not clear "outdated" updates,
                    // we need to indicate to the JS side that somehow we have a current update on
                    // disk that is not actually running.
                    currentPackage["_isDebugOnly"] = true;
                }

                // Enable differentiating pending vs. non-pending updates
                currentPackage["isPending"] = currentUpdateIsPending;
                promise.Resolve(currentPackage);
            }
        }


        [ReactMethod]
        public async void getNewStatusReport(IPromise promise)
        {
            await Task.Run(() =>
            {
                if (_greatPush.NeedToReportRollback)
                {
                    _greatPush.NeedToReportRollback = false;

                    var failedUpdates = SettingsManager.GetFailedUpdates();
                    if (failedUpdates != null && failedUpdates.Count > 0)
                    {
                        var lastFailedPackage = (JObject)failedUpdates[failedUpdates.Count - 1];
                        var failedStatusReport = TelemetryManager.GetRollbackReport(lastFailedPackage);
                        if (failedStatusReport != null)
                        {
                            promise.Resolve(failedStatusReport);
                            return;
                        }
                    }
                }
                else if (_greatPush.DidUpdate)
                {
                    var currentPackage = _greatPush.UpdateManager.GetCurrentPackageAsync().Result;
                    if (currentPackage != null)
                    {
                        var newPackageStatusReport = TelemetryManager.GetUpdateReport(currentPackage);
                        if (newPackageStatusReport != null)
                        {
                            promise.Resolve(newPackageStatusReport);
                            return;
                        }
                    }
                }
                else if (_greatPush.IsRunningBinaryVersion)
                {
                    var newAppVersionStatusReport = TelemetryManager.GetBinaryUpdateReport(_greatPush.AppVersion);
                    if (newAppVersionStatusReport != null)
                    {
                        promise.Resolve(newAppVersionStatusReport);
                        return;
                    }
                }
                else
                {
                    var retryStatusReport = TelemetryManager.GetRetryStatusReport();
                    if (retryStatusReport != null)
                    {
                        promise.Resolve(retryStatusReport);
                        return;
                    }
                }

                promise.Resolve("");
            }).ConfigureAwait(false);
        }

        [ReactMethod]
        public async void installUpdate(JObject updatePackage, InstallMode installMode, int minimumBackgroundDuration, IPromise promise)
        {
            await _greatPush.UpdateManager.InstallPackageAsync(updatePackage, SettingsManager.IsPendingUpdate(null)).ConfigureAwait(false);
            var pendingHash = (string)updatePackage[GreatPushConstants.PackageHashKey];
            SettingsManager.SavePendingUpdate(pendingHash, /* isLoading */false);
            if (installMode == InstallMode.OnNextResume)
            {
                if (_minimumBackgroundListener == null)
                {
                    // Ensure we do not add the listener twice.
                    Action loadBundleAction = () =>
                    {
                        Context.RunOnNativeModulesQueueThread(async () =>
                        {
                            await LoadBundleAsync().ConfigureAwait(false);
                        });
                    };

                    _minimumBackgroundListener = new MinimumBackgroundListener(loadBundleAction, minimumBackgroundDuration);
                    _reactContext.AddLifecycleEventListener(_minimumBackgroundListener);
                }
                else
                {
                    _minimumBackgroundListener.MinimumBackgroundDuration = minimumBackgroundDuration;
                }
            }

            promise.Resolve("");
        }

        [ReactMethod]
        public void isFailedUpdate(string packageHash, IPromise promise)
        {
            promise.Resolve(SettingsManager.IsFailedHash(packageHash));
        }

        [ReactMethod]
        public async void isFirstRun(string packageHash, IPromise promise)
        {
            bool isFirstRun = _greatPush.DidUpdate
                && packageHash != null
                && packageHash.Length > 0
                && packageHash.Equals(await _greatPush.UpdateManager.GetCurrentPackageHashAsync().ConfigureAwait(false));
            promise.Resolve(isFirstRun);
        }

        [ReactMethod]
        public void notifyApplicationReady(IPromise promise)
        {
            SettingsManager.RemovePendingUpdate();
            promise.Resolve("");
        }

        [ReactMethod]
        public async void restartApp(bool onlyIfUpdateIsPending)
        {
            // If this is an unconditional restart request, or there
            // is current pending update, then reload the app.
            if (!onlyIfUpdateIsPending || SettingsManager.IsPendingUpdate(null))
            {
                await LoadBundleAsync().ConfigureAwait(false);
            }
        }

        [ReactMethod]
        public async void recordStatusReported(JObject statusReport)
        {
            await Task.Run(() => TelemetryManager.RecordStatusReported(statusReport)).ConfigureAwait(false);
        }

        [ReactMethod]
        public async void saveStatusReportForRetry(JObject statusReport)
        {
            await Task.Run(() => TelemetryManager.SaveStatusReportForRetry(statusReport)).ConfigureAwait(false);
        }

        internal async Task LoadBundleAsync()
        {
            // #1) Get the private ReactInstanceManager, which is what includes
            //     the logic to reload the current React context.
            var reactInstanceManager = _greatPush.ReactInstanceManager;

            // #2) Update the locally stored JS bundle file path
            Type reactInstanceManagerType = typeof(ReactInstanceManager);
            string latestJSBundleFile = await _greatPush.GetJavaScriptBundleFileAsync(_greatPush.AssetsBundleFileName).ConfigureAwait(false);
            reactInstanceManagerType
                .GetField("_jsBundleFile", BindingFlags.NonPublic | BindingFlags.Instance)
                .SetValue(reactInstanceManager, latestJSBundleFile);

            // #3) Get the context creation method and fire it on the UI thread (which RN enforces)
            Context.RunOnDispatcherQueueThread(() => reactInstanceManager.RecreateReactContextAsync(CancellationToken.None));
        }
    }
}