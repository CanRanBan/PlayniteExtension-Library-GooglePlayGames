using Microsoft.Win32;
using Playnite.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace GooglePlayGamesLibrary
{
    internal class GooglePlayGames
    {
        public static string DataPath
        {
            get
            {
                string dataPath;

                using (var key = Registry.LocalMachine.OpenSubKey(@"Software\Google\Play Games"))
                {
                    if (key?.GetValueNames().Contains("UserLocalAppDataRoot") == true)
                    {
                        var rootPath = key.GetValue("UserLocalAppDataRoot")?.ToString();
                        dataPath = Path.Combine(rootPath, "Google", "Play Games");
                        if (Directory.Exists(dataPath))
                        {
                            return dataPath;
                        }
                    }
                }

                // Fallback to default location if registry key is missing.
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                dataPath = Path.Combine(localAppData, "Google", "Play Games");
                if (Directory.Exists(dataPath))
                {
                    return dataPath;
                }

                return string.Empty;
            }
        }

        public static string InstallationPath
        {
            get
            {
                string installationPath;

                using (var key = Registry.LocalMachine.OpenSubKey(@"Software\Google\Play Games"))
                {
                    if (key?.GetValueNames().Contains("InstallFolder") == true)
                    {
                        installationPath = key.GetValue("InstallFolder")?.ToString();
                        if (Directory.Exists(installationPath))
                        {
                            return installationPath;
                        }
                    }
                }

                // Fallback to default location if registry key is missing.
                var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                installationPath = Path.Combine(programFiles, "Google", "Play Games");
                if (Directory.Exists(installationPath))
                {
                    return installationPath;
                }

                return string.Empty;
            }
        }

        public static string MainExecutablePath
        {
            get
            {
                var installPath = InstallationPath;
                return string.IsNullOrEmpty(installPath) ? string.Empty : Path.Combine(installPath, "Bootstrapper.exe");
            }
        }

        public static string ServiceExecutablePath
        {
            get
            {
                var installPath = InstallationPath;
                return string.IsNullOrEmpty(installPath) ? string.Empty : Path.Combine(installPath, "current", "service", "Service.exe");
            }
        }

        public static bool IsInstalled
        {
            get
            {
                var mainPath = MainExecutablePath;
                var servicePath = ServiceExecutablePath;
                return !string.IsNullOrEmpty(mainPath) && !string.IsNullOrEmpty(servicePath) && File.Exists(mainPath) && File.Exists(servicePath);
            }
        }

        public static string Icon => Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "icon.png");

        public static void StartClient()
        {
            ProcessStarter.StartProcess(MainExecutablePath, string.Empty, InstallationPath);
        }

        public static void ExitClient()
        {
            ProcessStarter.StartProcess(MainExecutablePath, "/exit", InstallationPath);
        }
    }
}
