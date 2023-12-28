﻿using Microsoft.Win32;
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

        private const string registryFolder = @"SOFTWARE\" + companyName + @"\" + productName;

        private const string dataPathKey = @"UserLocalAppDataRoot";
        private const string installPathKey = @"InstallFolder";

        private const string imageCacheFolder = @"image_cache";

        private const string userDataFolderSearchPattern = @"userdata_*";
        private const string userDataImageFolder = @"avd";
        private const string userDataImageFile = @"userdata.img";

        private const string gameIconIdentifier = @".appicon";
        private const string gameBackgroundIdentifier = @".background";
        private const string gameLogoIdentifier = @".logo";

        internal const string gameIconIdentifierTypeIcon = gameIconIdentifier + imageTypeIconExtension;
        internal const string gameIconIdentifierTypePNG = gameIconIdentifier + imageTypePNGExtension;
        internal const string GameBackgroundIdentifierTypePNG = gameBackgroundIdentifier + imageTypePNGExtension;
        internal const string gameLogoIdentifierTypePNG = gameLogoIdentifier + imageTypePNGExtension;

        private const string mainExecutableName = @"Bootstrapper";
        internal const string ServiceExecutableName = @"Service";

        private const string executableExtension = @".exe";

        private const string imageTypeIconExtension = @".ico";
        private const string imageTypePNGExtension = @".png";

        private const string exitCommandLineArgument = @"/exit";

        public static string DataPath
        {
            get
            {
                string dataPath;

                // Retrieve registry view matching operating system architecture (64-Bit or 32-Bit).
                if (Environment.Is64BitOperatingSystem)
                {
                    using (var registryKeyLocalMachine =
                           RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                    {
                        using (var key = registryKeyLocalMachine.OpenSubKey(registryFolder))
                        {
                            if (key?.GetValueNames().Contains(dataPathKey) == true)
                            {
                                var rootPath = key.GetValue(dataPathKey)?.ToString();
                                if (!string.IsNullOrEmpty(rootPath))
                                {
                                    dataPath = Path.Combine(rootPath, companyName, productName);
                                    if (Directory.Exists(dataPath))
                                    {
                                        return dataPath;
                                    }
                                }
                            }
                        }
                    }
                }

                // Additionally check 32-Bit view on 64-Bit OS if not found in 64-Bit part.
                using (var registryKeyLocalMachine =
                       RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
                {
                    using (var key = registryKeyLocalMachine.OpenSubKey(registryFolder))
                    {
                        if (key?.GetValueNames().Contains(dataPathKey) == true)
                        {
                            var rootPath = key.GetValue(dataPathKey)?.ToString();
                            if (!string.IsNullOrEmpty(rootPath))
                            {
                                dataPath = Path.Combine(rootPath, companyName, productName);
                                if (Directory.Exists(dataPath))
                                {
                                    return dataPath;
                                }
                            }
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

                // Retrieve registry view matching operating system architecture (64-Bit or 32-Bit).
                if (Environment.Is64BitOperatingSystem)
                {
                    using (var registryKeyLocalMachine =
                           RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                    {
                        using (var key = registryKeyLocalMachine.OpenSubKey(registryFolder))
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
                    }
                }

                // Additionally check 32-Bit view on 64-Bit OS if not found in 64-Bit part.
                using (var registryKeyLocalMachine =
                       RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
                {
                    using (var key = registryKeyLocalMachine.OpenSubKey(registryFolder))
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
                }

                // Fallback to default location if registry key is missing.
                var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                installationPath = Path.Combine(programFiles, companyName, productName);
                if (Directory.Exists(installationPath))
                {
                    return installationPath;
                }

                // Additionally check 32-Bit folder on 64-Bit OS if not found in 64-Bit part.
                if (Environment.Is64BitOperatingSystem)
                {
                    programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                    installationPath = Path.Combine(programFiles, companyName, productName);

                    if (Directory.Exists(installationPath))
                    {
                        return installationPath;
                    }
                }

                return string.Empty;
            }
        }

        public static string ImageCachePath
        {
            get
            {
                var dataPath = DataPath;
                if (!string.IsNullOrEmpty(dataPath))
                {
                    string imageCachePath = Path.Combine(dataPath, imageCacheFolder);
                    if (Directory.Exists(imageCachePath))
                    {
                        return imageCachePath;
                    }
                }

                return string.Empty;
            }
        }

        public static string UserDataImageFolderPath
        {
            get
            {
                var dataPath = DataPath;
                if (!string.IsNullOrEmpty(dataPath))
                {
                    var userDataDirectory = Directory.GetDirectories(dataPath, userDataFolderSearchPattern, SearchOption.TopDirectoryOnly);
                    if (userDataDirectory.Any())
                    {
                        var userDataFolder = userDataDirectory.FirstOrDefault();
                        if (Directory.Exists(userDataFolder))
                        {
                            string userDataImageFolderPath = Path.Combine(userDataFolder, userDataImageFolder);
                            if (Directory.Exists(userDataImageFolderPath))
                            {
                                return userDataImageFolderPath;
                            }
                        }
                    }
                }

                return string.Empty;
            }
        }

        public static string UserDataImagePath
        {
            get
            {
                var dataPath = DataPath;
                if (!string.IsNullOrEmpty(dataPath))
                {
                    var userDataDirectory = Directory.GetDirectories(dataPath, userDataFolderSearchPattern, SearchOption.TopDirectoryOnly);
                    if (userDataDirectory.Any())
                    {
                        var userDataFolder = userDataDirectory.FirstOrDefault();
                        if (Directory.Exists(userDataFolder))
                        {
                            string userDataImagePath = Path.Combine(userDataFolder, userDataImageFolder, userDataImageFile);
                            if (File.Exists(userDataImagePath))
                            {
                                return userDataImagePath;
                            }
                        }
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

        public static string Icon => Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"Resources", @"GooglePlayGamesLibraryIcon.ico");

        public static void StartClient()
        {
            ProcessStarter.StartProcess(MainExecutablePath, string.Empty, InstallationPath);
        }

        public static void ExitClient()
        {
            ProcessStarter.StartProcessWait(MainExecutablePath, exitCommandLineArgument, InstallationPath);
        }
    }
}
