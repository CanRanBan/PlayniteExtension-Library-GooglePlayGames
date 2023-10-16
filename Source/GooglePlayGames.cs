using Microsoft.Win32;
using Playnite.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace GooglePlayGamesLibrary
{
    internal class GooglePlayGames
    {
        private const string companyName = @"Google";
        private const string productName = @"Play Games";

        public const string ApplicationName = companyName + @" " + productName;

        private const string registryFolder = @"Software\" + companyName + @"\" + productName;

        private const string dataPathKey = @"UserLocalAppDataRoot";
        private const string installPathKey = @"InstallFolder";

        private const string imageCacheFolder = @"image_cache";

        private const string mainExecutableName = @"Bootstrapper";
        public const string ServiceExecutableName = @"Service";

        private const string executableExtension = @".exe";

        private const string exitCommandLineArgument = @"/exit";

        public static string DataPath
        {
            get
            {
                string dataPath;

                using (var key = Registry.LocalMachine.OpenSubKey(registryFolder))
                {
                    if (key?.GetValueNames().Contains(dataPathKey) == true)
                    {
                        var rootPath = key.GetValue(dataPathKey)?.ToString();
                        dataPath = Path.Combine(rootPath, companyName, productName);
                        if (Directory.Exists(dataPath))
                        {
                            return dataPath;
                        }
                    }
                }

                // Fallback to default location if registry key is missing.
                var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                dataPath = Path.Combine(localAppData, companyName, productName);
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

                using (var key = Registry.LocalMachine.OpenSubKey(registryFolder))
                {
                    if (key?.GetValueNames().Contains(installPathKey) == true)
                    {
                        installationPath = key.GetValue(installPathKey)?.ToString();
                        if (Directory.Exists(installationPath))
                        {
                            return installationPath;
                        }
                    }
                }

                // Fallback to default location if registry key is missing.
                var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                installationPath = Path.Combine(programFiles, companyName, productName);
                if (Directory.Exists(installationPath))
                {
                    return installationPath;
                }

                return string.Empty;
            }
        }

        public static string ImageCachePath
        {
            get
            {
                string imageCachePath;

                var dataPath = DataPath;
                if (!string.IsNullOrEmpty(dataPath))
                {
                    imageCachePath = Path.Combine(dataPath, imageCacheFolder);
                    if (Directory.Exists(imageCachePath))
                    {
                        return imageCachePath;
                    }
                }

                return string.Empty;
            }
        }

        public static string MainExecutablePath
        {
            get
            {
                var installPath = InstallationPath;
                return string.IsNullOrEmpty(installPath) ? string.Empty : Path.Combine(installPath, mainExecutableName + executableExtension);
            }
        }

        public static string ServiceExecutablePath
        {
            get
            {
                var installPath = InstallationPath;
                return string.IsNullOrEmpty(installPath) ? string.Empty : Path.Combine(installPath, @"current", @"service", ServiceExecutableName + executableExtension);
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

        public static string Icon => Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"icon.png");

        public static void StartClient()
        {
            ProcessStarter.StartProcess(MainExecutablePath, string.Empty, InstallationPath);
        }

        public static void ExitClient()
        {
            ProcessStarter.StartProcessWait(MainExecutablePath, exitCommandLineArgument , InstallationPath);
        }
    }
}
