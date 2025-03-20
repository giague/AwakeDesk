namespace AwakeDesk.Utils.Models
{
    public class NewReleaseInfo
    {
        public NewReleaseInfo()
        {
            Version = string.Empty;
            InstallerUrl = string.Empty;
            ReleaseNotes = string.Empty;
        }

        public string Version { get; set; }
        public string InstallerUrl { get; set; }
        public string ReleaseNotes { get; set; }

        public bool IsNewVersion(string currentVersion)
        {
            Version actualVersion = new Version(currentVersion);
            Version availableVesion = new Version(Version);
            return availableVesion > actualVersion;
        }
    }
}
