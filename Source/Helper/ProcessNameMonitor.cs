// This file is part of Google Play Games on PC Library. A Playnite extension to import games available on PC from Google Play Games.
// Copyright CanRanBan, 2023-2025, Licensed under the EUPL-1.2 or later.

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Playnite.Common;
using Playnite.SDK;

namespace GooglePlayGamesLibrary.Helper
{
    // Modified version of "ProcessMonitor" by JosefNemec
    // Source: https://github.com/JosefNemec/Playnite/blob/790b6b1d8c7b703b156288c3a0edfb88037b5ca4/source/Playnite/Common/ProcessMonitor.cs
    internal class ProcessNameMonitor : IDisposable
    {
        // Shared resources
        private readonly ILogger logger;
        private readonly IPlayniteAPI playniteAPI;

        // Workaround for 32-bit Playnite
        private readonly bool is32BitPlaynite = Assembly.GetEntryAssembly().GetName().ProcessorArchitecture.Equals(ProcessorArchitecture.X86);

        #region ThreadingVariables
        private readonly SynchronizationContext processNameMonitorContext;
        private CancellationTokenSource processNameMonitorToken;
        #endregion ThreadingVariables

        #region EventHandling
        internal class MonitoringStartedEventArgs
        {
            internal int GameProcessID { get; set; }
        }

        internal event EventHandler<MonitoringStartedEventArgs> MonitoringStarted;
        internal event EventHandler MonitoringStopped;
        #endregion EventHandling

        internal ProcessNameMonitor(ILogger logger, IPlayniteAPI playniteAPI)
        {
            this.logger = logger;
            this.playniteAPI = playniteAPI;

            processNameMonitorContext = SynchronizationContext.Current;
        }

        // Cleanup
        public void Dispose()
        {
            StopMonitoring();
        }

        #region Monitoring
        internal void StopMonitoring()
        {
            processNameMonitorToken?.Cancel();
            processNameMonitorToken?.Dispose();
        }

        internal async void StartMonitoring(string gameName, int trackingDelay = 2000, int trackingStartDelay = 0, bool allowEmptyName = false)
        {
            #region RequiredParameterCheck
            var gameNameMissingIdentifier = "GooglePlayGamesGameNameMissing";

            if (!allowEmptyName && string.IsNullOrEmpty(gameName))
            {
                var gameNameMissing = "Required game name for ProcessNameMonitor is missing.";
                playniteAPI.Notifications.Add(gameNameMissingIdentifier, gameNameMissing, NotificationType.Error);
                logger.Error(gameNameMissing);

                return;
            }

            playniteAPI.Notifications.Remove(gameNameMissingIdentifier);
            #endregion RequiredParameterCheck

            processNameMonitorToken = new CancellationTokenSource();

            var gameStarted = false;

            if (trackingStartDelay > 0)
            {
                // Ignore TaskCanceledException
                await Task.WhenAny(Task.Delay(trackingStartDelay, processNameMonitorToken.Token));
            }

            while (true)
            {
                if (processNameMonitorToken.IsCancellationRequested)
                {
                    return;
                }

                var gameProcessID = GetProcessID(gameName, allowEmptyName);

                if (!gameStarted && gameProcessID > 0)
                {
                    OnMonitoringStarted(gameProcessID);

                    gameStarted = true;
                }

                if (gameStarted && gameProcessID <= 0)
                {
                    OnMonitoringStopped();

                    return;
                }

                await Task.Delay(trackingDelay);
            }
        }
        #endregion Monitoring

        private int GetProcessID(string gameName, bool allowEmptyName)
        {
            /**
             * Explanation how GetProcessID works using PowerShell examples:
             *
             * List all IDs and window titles of emulator processes:
             * PowerShell: Get-Process -Name crosvm | ForEach-Object { $_.Id, $_.MainWindowTitle }
             *
             * Retrieve process ID of emulator process with window title matching gameName:
             * PowerShell: Get-Process -Name crosvm | Where-Object { $_.MainWindowTitle -Match "$gameName" }
             *
             * Alternative method not depending on knowledge of game names, retrieves emulator process with non empty window title:
             * PowerShell: Get-Process -Name crosvm | Where-Object { ![string]::IsNullOrEmpty($_.MainWindowTitle) }
             *
             * Possible Results:
             * processID | meaning
             * >0 = wanted ID found,
             *  0 = no emulator process open,
             * -1 = game not found despite emulator process(es) open & empty names NOT allowed,
             * -2 = game not found despite emulator process(es) open & empty names allowed,
             * -3 = other application(s) with same name open
             */
            var processID = 0;

            // Get emulator process ID of running game for time tracking purposes
            var emulatorExecutableName = GooglePlayGames.EmulatorExecutableName;
            var emulatorProcessList = Process.GetProcessesByName(emulatorExecutableName);
            var trackingFallbackWindowTitle = string.Empty;

            if (emulatorProcessList.Any())
            {
                var emulatorPath = GooglePlayGames.EmulatorExecutablePath;

                foreach (var emulatorProcess in emulatorProcessList)
                {
                    string processPath;

                    if (is32BitPlaynite)
                    {
                        processPath = ProcessHelper.GetFullPathOfProcessByID((uint)emulatorProcess.Id);
                    }
                    else
                    {
                        processPath = emulatorProcess.MainModule?.FileName;
                    }

                    if (Paths.AreEqual(emulatorPath, processPath))
                    {
                        processID = -1;

                        if (allowEmptyName)
                        {
                            processID = -2;
                        }

                        if (!string.IsNullOrEmpty(gameName) && string.Equals(emulatorProcess.MainWindowTitle, gameName))
                        {
                            processID = emulatorProcess.Id;

                            logger.Trace(emulatorExecutableName + @" with window title matching '" + gameName + @"' found. Process ID = '" + processID + @"'.");

                            return processID;
                        }

                        if (allowEmptyName && !string.Equals(emulatorProcess.MainWindowTitle, string.Empty))
                        {
                            trackingFallbackWindowTitle = emulatorProcess.MainWindowTitle;
                            processID = emulatorProcess.Id;

                            logger.Trace(emulatorExecutableName + @" with window title matching '" + trackingFallbackWindowTitle + @"' found. Process ID = '" + processID + @"'. Tracking fallback was used.");

                            return processID;
                        }
                    }
                    else
                    {
                        processID = -3;
                    }
                }
            }

            switch (processID)
            {
                case 0:
                    logger.Trace(emulatorExecutableName + @" is currently not running.");
                    break;
                case -1:
                    logger.Trace(emulatorExecutableName + @" with window title matching '" + gameName + @"' not found. Empty names are allowed: " + allowEmptyName);
                    break;
                case -2:
                    logger.Trace(emulatorExecutableName + @" with window title matching '" + trackingFallbackWindowTitle + @"' not found. Empty names are allowed: " + allowEmptyName);
                    break;
                case -3:
                    logger.Trace(@"Other application(s) named '" + emulatorExecutableName + @"' is/are running.");
                    break;
                default:
                    logger.Error(@"Returned ID is outside of expected range. Game Name = '" + gameName + @"', Process ID = '" + processID + @"'.");
                    break;
            }

            return processID;
        }

        private void OnMonitoringStarted(int processId)
        {
            processNameMonitorContext.Post((a) => MonitoringStarted?.Invoke(this, new MonitoringStartedEventArgs { GameProcessID = processId }), null);
        }

        private void OnMonitoringStopped()
        {
            processNameMonitorContext.Post((a) => MonitoringStopped?.Invoke(this, EventArgs.Empty), null);
        }
    }
}
