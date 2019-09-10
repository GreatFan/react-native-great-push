using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
#if WINDOWS_UWP
using Windows.Storage;
#else
using GreatPush.Net46.Adapters.Storage;
using System.IO;
#endif

namespace GreatPush.ReactNative
{
    internal class SettingsManager
    {
        private static ApplicationDataContainer Settings = null;

        static SettingsManager ()
        {

#if WINDOWS_UWP
            Settings = ApplicationData.Current.LocalSettings.CreateContainer(GreatPushConstants.GreatPushPreferences, ApplicationDataCreateDisposition.Always);
#else
            var folder = UpdateUtils.GetGreatPushFolderAsync().Result;
            Settings = new ApplicationDataContainer(Path.Combine(folder.Path, GreatPushConstants.GreatPushPreferences));
#endif
        }

        public static JArray GetFailedUpdates()
        {
            var failedUpdatesString = (string)Settings.Values[GreatPushConstants.FailedUpdatesKey];
            if (failedUpdatesString == null)
            {
                return new JArray();
            }

            try
            {
                return JArray.Parse(failedUpdatesString);
            }
            catch (Exception)
            {
                var emptyArray = new JArray();
                Settings.Values[GreatPushConstants.FailedUpdatesKey] = JsonConvert.SerializeObject(emptyArray);
                return emptyArray;
            }
        }

        internal static JObject GetPendingUpdate()
        {
            var pendingUpdateString = (string)Settings.Values[GreatPushConstants.PendingUpdateKey];
            if (pendingUpdateString == null)
            {
                return null;
            }

            try
            {
                return JObject.Parse(pendingUpdateString);
            }
            catch (Exception)
            {
                // Should not happen.
                GreatPushUtils.Log("Unable to parse pending update metadata " + pendingUpdateString +
                        " stored in SharedPreferences");
                return null;
            }
        }

        internal static bool IsFailedHash(string packageHash)
        {
            JArray failedUpdates = SettingsManager.GetFailedUpdates();
            if (packageHash != null)
            {
                foreach (var failedPackage in failedUpdates)
                {
                    var failedPackageHash = (string)failedPackage[GreatPushConstants.PackageHashKey];
                    if (packageHash.Equals(failedPackageHash))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal static bool IsPendingUpdate(string packageHash)
        {
            JObject pendingUpdate = SettingsManager.GetPendingUpdate();
            return pendingUpdate != null &&
                    !(bool)pendingUpdate[GreatPushConstants.PendingUpdateIsLoadingKey] &&
                    (packageHash == null || ((string)pendingUpdate[GreatPushConstants.PendingUpdateHashKey]).Equals(packageHash));
        }

        internal static void RemoveFailedUpdates()
        {
            Settings.Values.Remove(GreatPushConstants.FailedUpdatesKey);
        }

        internal static void RemovePendingUpdate()
        {
            Settings.Values.Remove(GreatPushConstants.PendingUpdateKey);
        }

        internal static void SaveFailedUpdate(JObject failedPackage)
        {
            var failedUpdatesString = (string)Settings.Values[GreatPushConstants.FailedUpdatesKey];
            JArray failedUpdates;
            if (failedUpdatesString == null)
            {
                failedUpdates = new JArray();
            }
            else
            {
                failedUpdates = JArray.Parse(failedUpdatesString);
            }

            failedUpdates.Add(failedPackage);
            Settings.Values[GreatPushConstants.FailedUpdatesKey] = JsonConvert.SerializeObject(failedUpdates);
        }

        internal static void SavePendingUpdate(string packageHash, bool isLoading)
        {
            var pendingUpdate = new JObject()
            {
                { GreatPushConstants.PendingUpdateHashKey, packageHash },
                { GreatPushConstants.PendingUpdateIsLoadingKey, isLoading }
            };

            Settings.Values[GreatPushConstants.PendingUpdateKey] = JsonConvert.SerializeObject(pendingUpdate);
        }

        internal static void SetString(string key, string value)
        {
            Settings.Values[key] = value;
        }

        internal static string GetString(string key)
        {
            return (string)Settings.Values[key];
        }

        internal static void RemoveString(string key)
        {
            Settings.Values.Remove(key);
        }
    }
}