using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace Filebin_Upload
{
    public static class SettingsManager
    {
        private static ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;

        public static void SetValue(string key, object value)
        {
            roamingSettings.Values[key] = value;
        }

        public static T GetValue<T>(string key, T defaultValue = default(T))
        {
            object value = roamingSettings.Values[key];
            if (value == null)
            {
                return defaultValue;
            }
            else
            {
                return (T)value;
            }
        }
    }
}
