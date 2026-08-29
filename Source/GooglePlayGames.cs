// This file is part of Google Play Games on PC Library. A Playnite extension to import games available on PC from Google Play Games.
// Copyright CanRanBan, 2023-2026, Licensed under the EUPL-1.2 or later.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using GooglePlayGamesLibrary.Helper;
using Microsoft.Win32;
using Playnite.Common;

namespace GooglePlayGamesLibrary
{
    internal static class GooglePlayGames
    {
        private const string companyName = @"Google";
        private const string productName = @"Play Games";

        public const string ApplicationName = companyName + @" " + productName;

        private const string registryFolder = @"SOFTWARE\" + companyName + @"\" + productName;

        private const string customDataPathKey = @"CustomInstallLocationUserAppDataFolder";
        private const string customInstallPathKey = @"CustomInstallLocationProgramFilesFolder";
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

        private const string mainExecutableName = @"Bootstrapper"; // Always in default install path.
        internal const string ServiceExecutableName = @"Service"; // Either in custom install path or in default install path.
        internal const string EmulatorExecutableName = @"crosvm"; // Either in custom install path or in default install path.

        private const string executableExtension = @".exe";

        private const string imageTypeIconExtension = @".ico";
        private const string imageTypePNGExtension = @".png";

        public const string StartWithClient = @"Start game with " + ApplicationName + @".";

        internal const string shortcutRemoveNullCharactersRegex = @"\0";
        internal const string shortcutRemoveControlCharactersAndUnicodeRegex = @"[^\u0020-\u007E]";
        internal const string shortcutMatchGameStartURLRegex = @"(?:.+)(googleplaygames://launch/\?id=)(.+)(&lid=\d+&pid=\d+)(?:.+)";
        internal const string shortcutMatchGameNameRegex = @"(?:\S)(.+)(?:\S\,\s.+)";

        private const string backgroundCommandLineArgument = @"/bg";
        private const string exitCommandLineArgument = @"/exit";

        // Workaround for 32-bit Playnite
        private static readonly bool is32BitPlaynite = Assembly.GetEntryAssembly().GetName().ProcessorArchitecture.Equals(ProcessorArchitecture.X86);

        public static string DataPath
        {
            get
            {
                string dataPath;

                #region CustomDataPath
                // Check for customized installation first (higher priority).
                // Retrieve registry view matching operating system architecture (64-Bit or 32-Bit).
                if (Environment.Is64BitOperatingSystem)
                {
                    using (var registryKeyLocalMachine =
                           RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                    {
                        using (var key = registryKeyLocalMachine.OpenSubKey(registryFolder))
                        {
                            if (key?.GetValueNames().Contains(customDataPathKey) == true)
                            {
                                dataPath = key.GetValue(customDataPathKey)?.ToString();
                                if (Directory.Exists(dataPath))
                                {
                                    return dataPath;
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
                        if (key?.GetValueNames().Contains(customDataPathKey) == true)
                        {
                            dataPath = key.GetValue(customDataPathKey)?.ToString();
                            if (Directory.Exists(dataPath))
                            {
                                return dataPath;
                            }
                        }
                    }
                }
                #endregion CustomDataPath

                #region DefaultDataPath
                // Check for default installation second (lower priority).
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
                #endregion DefaultDataPath

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

                #region CustomInstallationPath
                // Check for customized installation first (higher priority).
                // Retrieve registry view matching operating system architecture (64-Bit or 32-Bit).
                if (Environment.Is64BitOperatingSystem)
                {
                    using (var registryKeyLocalMachine =
                           RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                    {
                        using (var key = registryKeyLocalMachine.OpenSubKey(registryFolder))
                        {
                            if (key?.GetValueNames().Contains(customInstallPathKey) == true)
                            {
                                installationPath = key.GetValue(customInstallPathKey)?.ToString();
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
                        if (key?.GetValueNames().Contains(customInstallPathKey) == true)
                        {
                            installationPath = key.GetValue(customInstallPathKey)?.ToString();
                            if (Directory.Exists(installationPath))
                            {
                                return installationPath;
                            }
                        }
                    }
                }
                #endregion CustomInstallationPath

                #region DefaultInstallationPath
                // Check for default installation second (lower priority).
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
                #endregion DefaultInstallationPath

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

        public static string DefaultInstallationPath
        {
            get
            {
                string defaultInstallationPath;

                // Default location for MainExecutablePath.
                var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                defaultInstallationPath = Path.Combine(programFiles, companyName, productName);
                if (Directory.Exists(defaultInstallationPath))
                {
                    return defaultInstallationPath;
                }

                // Additionally check 32-Bit folder on 64-Bit OS if not found in 64-Bit part.
                if (Environment.Is64BitOperatingSystem)
                {
                    programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                    defaultInstallationPath = Path.Combine(programFiles, companyName, productName);

                    if (Directory.Exists(defaultInstallationPath))
                    {
                        return defaultInstallationPath;
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
                    string imageCachePath;

                    if (IsCustomInstallation)
                    {
                        var currentUserSecurityIdentifier = WindowsIdentity.GetCurrent().User;
                        if (currentUserSecurityIdentifier != null)
                        {
                            imageCachePath = Path.Combine(dataPath, currentUserSecurityIdentifier.ToString(), imageCacheFolder);
                        }
                        else
                        {
                            return string.Empty;
                        }
                    }
                    else
                    {
                        imageCachePath = Path.Combine(dataPath, imageCacheFolder);
                    }

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
                string installPath;

                if (IsCustomInstallation)
                {
                    installPath = DefaultInstallationPath;
                }
                else
                {
                    installPath = InstallationPath;
                }
                
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

        public static string EmulatorExecutablePath
        {
            get
            {
                var installPath = InstallationPath;
                return string.IsNullOrEmpty(installPath) ? string.Empty : Path.Combine(installPath, @"current", @"emulator", EmulatorExecutableName + executableExtension);
            }
        }

        public static string ShortcutsPath
        {
            get
            {
                string shortcutsPath;

                var roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                shortcutsPath = Path.Combine(roaming, @"Microsoft", @"Windows", @"Start Menu", @"Programs", ApplicationName);
                if (Directory.Exists(shortcutsPath))
                {
                    return shortcutsPath;
                }

                return string.Empty;
            }
        }

        public static bool IsCustomInstallation
        {
            get
            {
                string dataPath;
                string installationPath;

                bool customDataPathExists = false;
                bool customInstallationPathExists = false;


                #region CustomDataPath
                // Retrieve registry view matching operating system architecture (64-Bit or 32-Bit).
                if (Environment.Is64BitOperatingSystem)
                {
                    using (var registryKeyLocalMachine =
                           RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                    {
                        using (var key = registryKeyLocalMachine.OpenSubKey(registryFolder))
                        {
                            if (key?.GetValueNames().Contains(customDataPathKey) == true)
                            {
                                dataPath = key.GetValue(customDataPathKey)?.ToString();
                                if (Directory.Exists(dataPath))
                                {
                                    customDataPathExists = true;
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
                        if (key?.GetValueNames().Contains(customDataPathKey) == true)
                        {
                            dataPath = key.GetValue(customDataPathKey)?.ToString();
                            if (Directory.Exists(dataPath))
                            {
                                customDataPathExists = true;
                            }
                        }
                    }
                }
                #endregion CustomDataPath

                #region CustomInstallationPath
                // Retrieve registry view matching operating system architecture (64-Bit or 32-Bit).
                if (Environment.Is64BitOperatingSystem)
                {
                    using (var registryKeyLocalMachine =
                           RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                    {
                        using (var key = registryKeyLocalMachine.OpenSubKey(registryFolder))
                        {
                            if (key?.GetValueNames().Contains(customInstallPathKey) == true)
                            {
                                installationPath = key.GetValue(customInstallPathKey)?.ToString();
                                if (Directory.Exists(installationPath))
                                {
                                    customInstallationPathExists = true;
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
                        if (key?.GetValueNames().Contains(customInstallPathKey) == true)
                        {
                            installationPath = key.GetValue(customInstallPathKey)?.ToString();
                            if (Directory.Exists(installationPath))
                            {
                                customInstallationPathExists = true;
                            }
                        }
                    }
                }
                #endregion CustomInstallationPath

                return customDataPathExists && customInstallationPathExists;
            }
        }

        public static bool IsInstalled
        {
            get
            {
                var mainPath = MainExecutablePath;
                var servicePath = ServiceExecutablePath;
                var emulatorPath = EmulatorExecutablePath;
                return !string.IsNullOrEmpty(mainPath) && !string.IsNullOrEmpty(servicePath) && !string.IsNullOrEmpty(emulatorPath)
                       && File.Exists(mainPath) && File.Exists(servicePath) && File.Exists(emulatorPath);
            }
        }

        public static string Icon => Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), @"Resources", @"GooglePlayGamesLibraryIcon.ico");

        public static void StartClient(bool background)
        {
            if (background)
            {
                ProcessStarter.StartProcess(MainExecutablePath, backgroundCommandLineArgument, InstallationPath);
            }
            else
            {
                ProcessStarter.StartProcess(MainExecutablePath, string.Empty, InstallationPath);
            }
        }

        public static void ExitClient()
        {
            ProcessStarter.StartProcessWait(MainExecutablePath, exitCommandLineArgument, InstallationPath);
        }

        public static bool IsClientOpen()
        {
            var serviceExecutableName = ServiceExecutableName;

            var serviceProcessList = Process.GetProcessesByName(serviceExecutableName);

            if (!serviceProcessList.Any())
            {
                return false;
            }

            var servicePath = ServiceExecutablePath;

            foreach (var serviceProcess in serviceProcessList)
            {
                string processPath;

                if (is32BitPlaynite)
                {
                    processPath = ProcessHelper.GetFullPathOfProcessByID((uint)serviceProcess.Id);
                }
                else
                {
                    processPath = serviceProcess.MainModule?.FileName;
                }

                if (Paths.AreEqual(servicePath, processPath))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
