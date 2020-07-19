
using myfoodapp.Core.Common;
using System;
using System.IO;
using System.Text.Json;

namespace myfoodapp.Core.Business
{
    public class UserSettingsManager
    {
        private static readonly AsyncLock asyncLock = new AsyncLock();
        private string FILE_NAME = "user.json";
        private static UserSettingsManager instance;
        private LogManager lg = LogManager.GetInstance;
        public static UserSettings CurrentUserSettings = new UserSettings();

        public static UserSettingsManager GetInstance
        {
            get
            {
                if (instance == null)
                {
                    instance = new UserSettingsManager();
                }
                return instance;
            }
        }

        private UserSettingsManager()
        {
            this.InitFileFolder();
        }

        public void InitFileFolder()
        {
            try
            {
                if (!File.Exists(FILE_NAME))
                {
#if DEBUG
                    var defaultUserSettings = new UserSettings()
                    {
                        isDebugLedEnable = true,
                        isSleepModeEnable = false,
                        isTempHumiditySensorEnable = true,
                        isDiagnosticModeEnable = false,
                        measureFrequency = 60000,
                        productionSiteId = "XXXXX",
                        hubMessageAPI = "https://hub.myfood.eu/api/Messages",
                        SSID = "MYFOODPI_AP",
                        ACCESS_POINT_PWD = "myfoodpi",
                        connectivityType = ConnectivityType.Sigfox,
                        sigfoxVersion = SigfoxVersion.v2
                    };
#endif

#if !DEBUG
                    var defaultUserSettings = new UserSettings()
                    {
                        isDebugLedEnable = false,
                        isSleepModeEnable = false,
                        isTempHumiditySensorEnable = true,
                        isDiagnosticModeEnable = false,
                        measureFrequency = 1800000,
                        productionSiteId = "XXXXX",
                        hubMessageAPI = "https://hub.myfood.eu/api/Messages",
                        SSID = "MYFOODPI_AP",
                        ACCESS_POINT_PWD = "myfoodpi"
                        connectivityType = ConnectivityType.Sigfox,
                        sigfoxVersion = SigfoxVersion.v2
                    };
#endif

                    string strSettings;
                    var options = new JsonSerializerOptions
                    {
                        WriteIndented = true
                    };
                    
                    strSettings = JsonSerializer.Serialize(defaultUserSettings, options);

                    File.WriteAllText(FILE_NAME, strSettings);              
                }
            }
            catch (Exception ex)
            {
                lg.AppendLog(Log.CreateErrorLog("Configuration File Creation", ex));
            }
        }
        public UserSettings GetUserSettings()
        {
                var file = File.OpenText(FILE_NAME);

                if (file != null)
                {
                    var read = File.OpenText(FILE_NAME);
                    UserSettings userSettings = JsonSerializer.Deserialize<UserSettings>(read.ReadToEnd());

                    CurrentUserSettings = userSettings;
                    return userSettings;
                }

                return null;
        }
        public void SyncUserSettings(UserSettings userSettings)
        {
            var file = File.OpenText(FILE_NAME);

            if (file != null)
            {
                string strSettings;
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                
                strSettings = JsonSerializer.Serialize(userSettings, options);

                File.WriteAllText(FILE_NAME, strSettings);
            }

            CurrentUserSettings = userSettings;
        }

    }
    public class UserSettings
    {
        public bool isDebugLedEnable { get; set; }
        public bool isSleepModeEnable { get; set; }
        public bool isDiagnosticModeEnable { get; set; }
        public bool isTempHumiditySensorEnable { get; set; }
        public int measureFrequency { get; set; }
        public string productionSiteId { get; set; }
        public string hubMessageAPI { get; set; }
        public string SSID { get; set; }
        public string ACCESS_POINT_PWD { get; set; }
        public ConnectivityType connectivityType { get; set; }
        public SigfoxVersion sigfoxVersion { get; set; }

    }

    public enum ConnectivityType
    {
        Wifi,
        Sigfox
    }

    public enum SigfoxVersion
    {
        v1,
        v2
    }
}
