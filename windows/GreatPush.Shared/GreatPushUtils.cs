using System;
using System.Diagnostics;
using System.Linq;
using System.IO;
#if WINDOWS_UWP
using Windows.ApplicationModel;
using Windows.Storage;
#endif

namespace GreatPush.ReactNative
{
    internal partial class GreatPushUtils
    {
        internal static void Log(string message)
        {
            Debug.WriteLine("[GreatPush] " + message, GreatPushConstants.ReactNativeLogCategory);
        }

        internal static void LogBundleUrl(string path)
        {
            Log("Loading JS bundle from \"" + path + "\"");
        }

        static string _deviceId = String.Empty;

        internal static string GetDeviceId()
        {
            //It's quite long operation, cache it
            if (!String.IsNullOrEmpty(_deviceId))
                return _deviceId;

            _deviceId = GetDeviceIdImpl();
            return _deviceId;
        }

        internal static string GetAppVersion()
        {
#if WINDOWS_UWP
            return Package.Current.Id.Version.Major + "." + Package.Current.Id.Version.Minor + "." + Package.Current.Id.Version.Build;
#else
            return applicationInfo.Version;
#endif
        }

        internal static string GetAppFolder()
        {
#if WINDOWS_UWP
            return ApplicationData.Current.LocalFolder.Path;
#else
            return AppDomain.CurrentDomain.BaseDirectory;
#endif
        }

        internal static string GetAssetsBundlePrefix()
        {
#if WINDOWS_UWP
            return GreatPushConstants.AssetsBundlePrefix;
#else
            return Path.Combine(GetAppFolder(), GreatPushConstants.AssetsBundlePrefix);
#endif
        }

        internal static string ExtractSubFolder(string fullPath)
        {
            var greatPushSubPathArray = fullPath.Split(Path.DirectorySeparatorChar);
            return String.Join("/", greatPushSubPathArray.SkipWhile((value, index) => greatPushSubPathArray.Length - index > 4).ToArray());
        }

    }
}
