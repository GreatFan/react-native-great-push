﻿using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System.Profile;

namespace GreatPush.ReactNative
{
    internal partial class GreatPushUtils
    {
        internal static string GetFileBundlePrefix()
        {
            return GreatPushConstants.FileBundlePrefix;
        }

        internal async static Task<JObject> GetJObjectFromFileAsync(StorageFile file)
        {
            string jsonString = await FileIO.ReadTextAsync(file).AsTask().ConfigureAwait(false);
            if (jsonString.Length == 0)
            {
                return new JObject();
            }

            try
            {
                return JObject.Parse(jsonString);
            }
            catch (Exception)
            {
                return null;
            }
        }

        static string GetDeviceIdImpl()
        {
            HardwareToken token = HardwareIdentification.GetPackageSpecificToken(null);
            IBuffer hardwareId = token.Id;
            var dataReader = DataReader.FromBuffer(hardwareId);

            var bytes = new byte[hardwareId.Length];
            dataReader.ReadBytes(bytes);

            return BitConverter.ToString(bytes);
        }
    }
}
