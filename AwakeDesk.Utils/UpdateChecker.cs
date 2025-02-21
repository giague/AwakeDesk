using AwakeDesk.Utils.Models;
using System.Text.Json;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http.Headers;

namespace AwakeDesk.Utils
{
    public class UpdateChecker()
    {


        private const string REPO_OWNER = "giague";
        private const string REPO_NAME = "AwakeDesk";
        private static NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

        public string? Error { get; set; }

        public async Task<NewReleaseInfo?> RetrieveNewRelease(string currentVersion)
        {
            NewReleaseInfo? newRelease = new();
            string apiUrl = $"https://api.github.com/repos/{REPO_OWNER}/{REPO_NAME}/releases/latest";

            using HttpClient client = new();
            client.DefaultRequestHeaders.Add("User-Agent", "AwakeDesk-App");

            try
            {
                string response = await client.GetStringAsync(apiUrl);
                using JsonDocument doc = JsonDocument.Parse(response);

                JsonElement assets = doc.RootElement.GetProperty("assets");
                string? versionUrl = null;
                string? releaseNotesUrl = null;
                foreach (JsonElement asset in assets.EnumerateArray())
                {
                    string name = asset.GetProperty("name").GetString();
                    if (name.EndsWith("_Setup.exe"))
                    {
                        newRelease.InstallerUrl = asset.GetProperty("browser_download_url").GetString();
                    }
                    if (name == "version.txt")
                    {
                        versionUrl = asset.GetProperty("browser_download_url").GetString();
                    }
                    if (name == "release_notes.txt")
                    {
                        releaseNotesUrl = asset.GetProperty("browser_download_url").GetString();
                    }
                }

                if (!string.IsNullOrEmpty(versionUrl))
                {
                    newRelease.Version = await client.GetStringAsync(versionUrl);
                    if (!string.IsNullOrEmpty(releaseNotesUrl))
                    {
                        newRelease.ReleaseNotes = await client.GetStringAsync(releaseNotesUrl);
                    }
                    if (newRelease.IsNewVersion(currentVersion))
                    {
                        return newRelease;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                Error = ex.Message;
            }
            return null;
        }
    }

}
