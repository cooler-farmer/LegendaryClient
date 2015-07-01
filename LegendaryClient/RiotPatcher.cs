using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LegendaryClient.Logic;
using LegendaryClient.Logic.UpdateRegion;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace LegendaryClient
{
    public class RiotPatcher
    {
        private string _themeName;
        private readonly HttpClient _standardClient = new HttpClient();

        public RiotPatcher(BaseUpdateRegion region)
        {
            UpdateRegion = region;
        }

        public BaseUpdateRegion UpdateRegion { get; private set; }
        public String ExecutingDirectory { get; set; }

        #region Pre Patch

        #region Public Functions

        public async Task<UpdateReport> CheckUpdateRequirement()
        {
            var report = new UpdateReport();
            var ddragon = await CheckDataDragonVersion();
            report.DataDragonReport = ddragon;

            var airReport = await CheckAirVersion();
            report.AirReport = airReport;

            return report;
        }

        #endregion

        #region Private Functions

        #region Data Dragon

        /// <summary>
        ///     Checks if an update is required
        /// </summary>
        /// <returns>UpdateRequired, UpdateUri, UpdateSize</returns>
        /// <exception cref="PatcherException"></exception>
        private async Task<DataDragonPrePatchReport> CheckDataDragonVersion()
        {
            var dataDragonReleaseInformation = await GetLatestDataDragonUri();
            var latestDataDragonUri = dataDragonReleaseInformation.Item1;
            var latestDataDragonVersion = dataDragonReleaseInformation.Item2;
            var installedDataDragonVersion = GetLatestDataDragonVersionInstalled();

            if (latestDataDragonVersion <= installedDataDragonVersion)
            {
                var report = new DataDragonPrePatchReport {UpdateRequired = false, InstalledVersion = installedDataDragonVersion, NewestVersion = latestDataDragonVersion};
                return report;
            }
            if (latestDataDragonVersion > installedDataDragonVersion)
            {
                var report = new DataDragonPrePatchReport
                {
                    UpdateRequired = true,
                    UpdateUri = latestDataDragonUri,
                    InstalledVersion = installedDataDragonVersion,
                    NewestVersion = latestDataDragonVersion
                };

                //Get size from header
                try
                {
                    var response = await _standardClient.GetAsync(latestDataDragonUri, HttpCompletionOption.ResponseHeadersRead);
                    var size = response.Content.Headers.ContentLength;
                    if (size.HasValue)
                        report.UpdateSize = size.Value;
                    return report;
                }
                catch (Exception ex)
                {
                    ErrorKill(ex);
                }
            }

            return null;
        }

        /// <summary>
        ///     Checks for the latest installed DataDragon Version
        /// </summary>
        /// <returns>Latest installed DataDragon Version</returns>
        /// <exception cref="PatcherException"></exception>
        private Version GetLatestDataDragonVersionInstalled()
        {
            try
            {
                var fileName = GetAssetsDirectory("VERSION_DDRagon");
                if (!File.Exists(fileName))
                    return new Version();

                var fileContent = File.ReadAllText(fileName);
                var version = new Version(fileContent);
                return version;
            }
            catch (Exception ex)
            {
                ErrorKill(ex);
            }

            return new Version();
        }

        /// <summary>
        ///     Returns the latest DataDragon URI and the latest DataDragon Version
        /// </summary>
        /// <returns>Latest DataDragon URI and the latest DataDragon Version</returns>
        /// <exception cref="PatcherException"></exception>
        /// <exception cref="JsonException"></exception>
        private async Task<Tuple<Uri, Version>> GetLatestDataDragonUri()
        {
            var responseString = String.Empty;

            try
            {
                responseString = await _standardClient.GetStringAsync("http://ddragon.leagueoflegends.com/realms/na.json");
            }
            catch (Exception e)
            {
                ErrorKill(e);
            }

            var deserializedJson = JObject.Parse(responseString);
            var version = deserializedJson.GetValue("v").Value<string>();
            var cdn = deserializedJson.GetValue("cdn").Value<string>();

            if (String.IsNullOrEmpty(cdn) || String.IsNullOrEmpty(version))
            {
                ErrorKill(new Exception("JSON Data incorrect"));
            }

            var url = String.Format("{0}/dragontail-{1}.tgz", cdn, version);
            return new Tuple<Uri, Version>(new Uri(url), new Version(version));
        }

        #endregion

        #region Air

        /// <summary>
        ///     Checks if an update is required
        /// </summary>
        /// <returns>UpdateRequired, UpdateUri, UpdateSize</returns>
        /// <exception cref="PatcherException"></exception>
        private async Task<AirPrePatchReport> CheckAirVersion()
        {
            var airReleases = await GetAirVersions();
            var latestAirRelease = GetLatestAirVersion(airReleases);
            var installedAirRelease = GetLatestAirVersionInstalled();


            if (latestAirRelease <= installedAirRelease)
            {
                var report = new AirPrePatchReport {InstalledVersion = installedAirRelease, NewestVersion = latestAirRelease};
                return report;
            }
            if (latestAirRelease > installedAirRelease)
            {
                var report = new AirPrePatchReport {InstalledVersion = installedAirRelease, NewestVersion = latestAirRelease, UpdateRequired = true};
                var updateFiles = new List<AirPatcherFile>();

                //GetFiles
                var files = await GetManifest(latestAirRelease);

                //Check Theme
                var themeInformation = await CheckTheme(installedAirRelease, files.ToArray());
                var themeFiles = themeInformation.Item1;
                if (themeFiles != null && themeFiles.Length > 0)
                    updateFiles.AddRange(themeFiles);
                _themeName = themeInformation.Item2;

                //Check Individual Files
                var gameStatsFile = files.First(f => f.RelativePath.Contains("gameStats_en_US.sqlite"));
                var gameStatsInstalled = CheckIndividualAirFile(installedAirRelease, gameStatsFile, Path.Combine(ExecutingDirectory, "gameStats_en_US.sqlite"));
                if (!gameStatsInstalled)
                    updateFiles.Add(gameStatsFile);

                if (UpdateRegion.GetType().Name != "Garena")
                {
                    var clientLibFile = files.First(f => f.RelativePath.Contains("ClientLibCommon.dat"));
                    var clientLibInstalled = CheckIndividualAirFile(installedAirRelease, clientLibFile, Path.Combine(ExecutingDirectory, "ClientLibCommon.dat"));
                    if (!clientLibInstalled)
                        updateFiles.Add(clientLibFile);
                }

                //Media Files
                CreateMediaFileFolders();
                var mediaFiles =
                    files.Where(
                        f =>
                            f.Version > installedAirRelease && (f.SavePath.EndsWith(".jpg") || f.SavePath.EndsWith(".png") || f.SavePath.EndsWith(".mp3")) &&
                            (f.SavePath.Contains("assets/images/champions/") || f.SavePath.Contains("assets/images/abilities/") ||
                             f.SavePath.Contains("assets/storeImages/content/summoner_icon/") || (f.SavePath.Contains("assets/sounds/") && f.SavePath.Contains("en_US/champions/")) ||
                             (f.SavePath.Contains("assets/sounds/") && f.SavePath.Contains("assets/sounds/ambient")) ||
                             (f.SavePath.Contains("assets/sounds/") && f.SavePath.Contains("assets/sounds/matchmakingqueued.mp3")))).ToArray();
                updateFiles.AddRange(mediaFiles);

                report.FilesToUpdate = updateFiles.ToArray();
                return report;
            }

            return null;
        }

        private void CreateMediaFileFolders()
        {
            if (!Directory.Exists(GetAssetsDirectory("champions")))
                Directory.CreateDirectory(GetAssetsDirectory("champions"));

            if (!Directory.Exists(GetAssetsDirectory("sounds")))
                Directory.CreateDirectory(GetAssetsDirectory("sounds"));

            if (!Directory.Exists(GetAssetsDirectory("sounds", "champions")))
                Directory.CreateDirectory(GetAssetsDirectory("sounds", "champions"));

            if (!Directory.Exists(GetAssetsDirectory("sounds", "ambient")))
                Directory.CreateDirectory(GetAssetsDirectory("Assets", "sounds", "ambient"));

            if (!Directory.Exists(GetAssetsDirectory("profileicon")))
                Directory.CreateDirectory(GetAssetsDirectory("profileicon"));
        }

        private bool CheckIndividualAirFile(Version installedVersion, AirPatcherFile file, string localpath)
        {
            if (file == null || file.Version > installedVersion)
            {
                return false;
            }

            if (!File.Exists(localpath))
                return false;
            if (new FileInfo(localpath).Length != file.FileSize)
                return false;
            return true;
        }

        private async Task<Tuple<AirPatcherFile[], string>> CheckTheme(Version installedVersion, AirPatcherFile[] files)
        {
            var themePropertiesFile = files.First(f => f.RelativePath.Contains("theme.properties"));
            if (themePropertiesFile == null)
                return new Tuple<AirPatcherFile[], string>(null, "");

            var fileUpdated = CheckIndividualAirFile(installedVersion, themePropertiesFile, GetAssetsDirectory("themes", "theme.properties"));

            string theme;
            if (fileUpdated)
            {
                theme = GetInstalledThemeName();
            }
            else
            {
                var themeBytes = await DownloadThemePropertyFile(themePropertiesFile);
                theme = GetInstalledThemeName(themeBytes);
            }

            var themePatcherFiles = files.Where(l => l.RelativePath.ToLower().Contains("loop") && l.RelativePath.Contains(theme)).ToArray();
            var themeFullyInstalled = CheckThemeFilesInstalled(themePatcherFiles, theme);

            if (themeFullyInstalled)
                return new Tuple<AirPatcherFile[], string>(null, theme);
            return new Tuple<AirPatcherFile[], string>(themePatcherFiles, theme);
        }

        private bool CheckThemeFilesInstalled(AirPatcherFile[] files, string theme)
        {
            if (Directory.Exists(GetAssetsDirectory("themes", theme)))
            {
                var installedThemeFiles = Directory.GetFiles(GetAssetsDirectory("themes", theme)).Select(s => new FileInfo(s)).ToArray();
                var equal =
                    files.All(
                        patcherFile =>
                            installedThemeFiles.Any(f => f.Name.Equals(patcherFile.FileName, StringComparison.CurrentCultureIgnoreCase) && f.Length == patcherFile.FileSize));
                return equal;
            }
            Directory.CreateDirectory(GetAssetsDirectory("themes", theme));
            return false;
        }

        private async Task<byte[]> DownloadThemePropertyFile(AirPatcherFile themePropertiesFile)
        {
            if (!Directory.Exists(GetAssetsDirectory("themes")))
                Directory.CreateDirectory(GetAssetsDirectory("themes"));

            var themePropertiesBytes = await _standardClient.GetByteArrayAsync(themePropertiesFile.AbsolutePath);
            File.WriteAllBytes(GetAssetsDirectory("themes", "theme.properties"), themePropertiesBytes);
            return themePropertiesBytes;
        }

        private string GetInstalledThemeName()
        {
            if (!File.Exists(GetAssetsDirectory("themes", "theme.properties")))
                return "";

            try
            {
                var name = File.ReadAllLines(GetAssetsDirectory("themes", "theme.properties")).First(s => s.StartsWith("themeConfig=")).Split('=')[1].Split(',')[0];
                return name;
            }
            catch
            {
                return "";
            }
        }

        private string GetInstalledThemeName(byte[] fileBytes)
        {
            try
            {
                var name = Regex.Split(Encoding.Default.GetString(fileBytes), Environment.NewLine).First(s => s.StartsWith("themeConfig=")).Split('=')[1].Split(',')[0];
                return name;
            }
            catch
            {
                return "";
            }
        }

        /// <summary>
        ///     Checks for the latest installed Air Assets Version
        /// </summary>
        /// <returns>Latest installed Air Assets Version</returns>
        /// <exception cref="PatcherException"></exception>
        private Version GetLatestAirVersionInstalled()
        {
            try
            {
                var fileName = GetAssetsDirectory("VERSION_AIR");
                if (!File.Exists(fileName))
                    return new Version();

                var fileContent = File.ReadAllText(fileName);
                var version = new Version(fileContent);
                return version;
            }
            catch (Exception ex)
            {
                ErrorKill(ex);
            }

            return new Version();
        }

        /// <summary>
        ///     Returns all Air Releases
        /// </summary>
        /// <returns>Array of Air Releases</returns>
        /// <exception cref="PatcherException"></exception>
        private async Task<Version[]> GetAirVersions()
        {
            try
            {
                var responseString = await _standardClient.GetStringAsync(UpdateRegion.AirListing);
                return Regex.Split(responseString, Environment.NewLine).Where(s => !String.IsNullOrEmpty(s)).Select(s => new Version(s)).ToArray();
            }
            catch (Exception ex)
            {
                ErrorKill(ex);
            }
            return null;
        }

        /// <summary>
        ///     Selects the latest Version from an Array of Versions
        /// </summary>
        /// <param name="versions">All Air Versions</param>
        /// <returns>Newest Version</returns>
        private Version GetLatestAirVersion(Version[] versions)
        {
            if (versions.Length <= 0)
                ErrorKill(new Exception("Versions Length <= 0"));

            return versions.OrderByDescending(v => v).First();
        }

        /// <summary>
        ///     Gets a filelist of all Air files
        /// </summary>
        /// <param name="v">Air Release Version</param>
        /// <returns>Array of Files</returns>
        private async Task<AirPatcherFile[]> GetManifest(Version v)
        {
            var manifestLink = String.Format("{0}releases/{1}/packages/files/packagemanifest", UpdateRegion.AirManifest, v);
            try
            {
                var responseString = await _standardClient.GetStringAsync(manifestLink);
                var manifestLines = Regex.Split(responseString, Environment.NewLine);
                var files = manifestLines.Where(m => m.Contains(',')).Select(m => new AirPatcherFile(UpdateRegion, m));
                return files.ToArray();
            }
            catch (Exception ex)
            {
                ErrorKill(ex);
            }
            return null;
        }

        #endregion

        #endregion

        #endregion

        #region Helper Functions

        private void ErrorKill(Exception ex, bool fatalError = true)
        {
            var r = new UpdateReport {ReportType = UpdateReportType.Error, Exception = ex, FatalError = fatalError};
            throw new PatcherException(r);
        }

        private string GetAssetsDirectory(params string[] sub)
        {
            var parts = new List<string> {ExecutingDirectory, "Assets"};
            parts.AddRange(sub);
            return Path.Combine(parts.ToArray());
        }

        #endregion
    }

    public class PatcherException : Exception
    {
        public PatcherException(UpdateReport report)
        {
            Report = report;
        }

        public UpdateReport Report { get; set; }
    }

    public class AirPatcherFile
    {
        public AirPatcherFile(BaseUpdateRegion r, string path, Version version, long size)
        {
            RelativePath = path;
            Version = version;
            FileSize = size;
            Region = r;
        }

        public AirPatcherFile(BaseUpdateRegion r, string path, string version, string size) : this(r, path, new Version(version), Convert.ToInt64(size))
        {
        }

        public AirPatcherFile(BaseUpdateRegion r, string path, string size) : this(r, path, path.Substring(34).Substring(0, path.Substring(34).IndexOf('/')), size)
        {
        }

        public AirPatcherFile(BaseUpdateRegion r, string line) : this(r, line.Split(',')[0], line.Split(',')[3])
        {
        }

        public BaseUpdateRegion Region { get; set; }
        public string RelativePath { get; set; }
        public Version Version { get; set; }
        public long FileSize { get; set; }

        public string AbsolutePath
        {
            get { return String.Format("{0}{1}", Region.BaseLink, RelativePath); }
        }

        public string SavePath
        {
            get { return Regex.Split(RelativePath, "/files/")[1]; }
        }

        public string FileName
        {
            get { return Path.GetFileName(SavePath); }
        }
    }

    public class UpdateReport
    {
        public UpdateReportType ReportType { get; set; }
        //Error
        public Exception Exception { get; set; }
        public bool FatalError { get; set; }
        //No Error
        public Boolean UpdateRequired
        {
            get { return DataDragonReport.UpdateRequired || AirReport.UpdateRequired; }
        }

        public long TotalUpdateSize
        {
            get { return DataDragonReport.UpdateSize + AirReport.UpdateSize; }
        }

        public DataDragonPrePatchReport DataDragonReport { get; set; }
        public AirPrePatchReport AirReport { get; set; }
        public string Theme { get; set; }
    }

    public class AirPrePatchReport
    {
        public Boolean UpdateRequired { get; set; }
        public Version InstalledVersion { get; set; }
        public Version NewestVersion { get; set; }
        public long UpdateSize { get { return FilesToUpdate.Sum(f => f.FileSize); }}
        public AirPatcherFile[] FilesToUpdate { get; set; }
    }

    public class DataDragonPrePatchReport
    {
        public Boolean UpdateRequired { get; set; }
        public Version InstalledVersion { get; set; }
        public Version NewestVersion { get; set; }
        public long UpdateSize { get; set; }
        public Uri UpdateUri { get; set; }
    }

    public enum UpdateReportType
    {
        PrePatchReport,
        PostPatchReport,
        Error
    }
}