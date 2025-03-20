using System.Configuration;
using System.IO;

namespace AwakeDesk.Models
{
    public class AwakeDeskSettings
    {
        public const string SOFTWARE_NAME = "AwakeDesk";
        public string CurrentVersion { get; set; }
        public string UpdateUrl { get; set; }
        public int AlarmDelayMinutes { get; set; }
        public string Preset1 { get; set; }
        public string Preset2 { get; set; }
        public string ActiveAlarmRingtone { get; set; }

        public List<RingtoneItem> AvailableAlarmRingtones { get; set; }

        public AwakeDeskSettings()
        {
            CurrentVersion = "1.0";
            UpdateUrl = "https://github.com/giague/AwakeDesk/blob/main/AwakeDesk/App.config";
            AlarmDelayMinutes = 5;
            Preset1 = "";
            Preset2 = "";
            ActiveAlarmRingtone = "";
            AvailableAlarmRingtones = new();
        }

        public string CurrentVersionLabel
        {
            get
            {
                return string.Concat("v. ", CurrentVersion);
            }
        }


        internal void LoadFromConfiguration()
        {
            CurrentVersion = ConfigurationManager.AppSettings["CurrentVersion"] ?? "1.0";
            UpdateUrl = ConfigurationManager.AppSettings["UpdateUrl"] ?? "https://github.com/giague/AwakeDesk/blob/main/AwakeDesk/App.config";
            var configAlarmDelayMinutes = ConfigurationManager.AppSettings["AlarmDelayMinutes"] ?? "5";
            AlarmDelayMinutes = 5;
            if (int.TryParse(configAlarmDelayMinutes, out int alarmDelayMinutes))
            {
                AlarmDelayMinutes = alarmDelayMinutes;
            }

            Preset1 = ConfigurationManager.AppSettings["Preset1"] ?? "13:30";
            Preset2 = ConfigurationManager.AppSettings["Preset2"] ?? "17:05";

            ActiveAlarmRingtone = ConfigurationManager.AppSettings["ActiveAlarmRingtone"] ?? "Media/mp3/Alarm1.mp3";

            var ringtoneKey = ConfigurationManager.AppSettings["AvailableAlarmRingtones"];
            if (!string.IsNullOrEmpty(ringtoneKey))
            {
                var ringtones = ringtoneKey.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                AvailableAlarmRingtones = new();
                foreach (var ringtonePath in ringtones)
                {
                    var ringtoneItem = new RingtoneItem
                    {
                        Path = ringtonePath,
                        Name = Path.GetFileName(ringtonePath)
                    };
                    AvailableAlarmRingtones.Add(ringtoneItem);

                }
            }
        }

        public void SaveConfiguration()
        {
            Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            config.AppSettings.Settings["AlarmDelayMinutes"].Value = AlarmDelayMinutes.ToString();
            config.AppSettings.Settings["Preset1"].Value = Preset1;
            config.AppSettings.Settings["Preset2"].Value = Preset2;
            config.AppSettings.Settings["ActiveAlarmRingtone"].Value = ActiveAlarmRingtone;
            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }
    }
}
