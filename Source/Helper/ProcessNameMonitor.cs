using Playnite.Common;
using Playnite.SDK;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

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

        internal async void StartMonitoring(string gameName, int trackingDelay = 2000, int trackingStartDelay = 0)
        {
            #region RequiredParameterCheck
            var gameNameMissingIdentifier = "GooglePlayGamesGameNameMissing";

            if (string.IsNullOrEmpty(gameName))
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

                var gameProcessID = GetProcessID(gameName, is32BitPlaynite);

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

        private int GetProcessID(string gameName, bool use32BitWorkaround)
        {
            // PowerShell: Get-Process | Where-Object { $_.MainWindowTitle -Match "$gameName" }
            // processID >0 = wanted ID found, 0 = no emulator process open, -1 = game not found despite emulator process(es) open, -2 = other application(s) with same name open
            var processID = 0;

            var emulatorPath = GooglePlayGames.EmulatorExecutablePath;
            var emulatorExecutableName = GooglePlayGames.EmulatorExecutableName;

            if (use32BitWorkaround)
            {
                var gameWindowList = ProcessHelper.FindWindowsMatchingText(gameName);

                if (gameWindowList.Any())
                {
                    foreach (var gameWindow in gameWindowList)
                    {
                        ProcessHelper.GetWindowThreadProcessId(gameWindow, out uint gameWindowProcessID);

                        var processPath = ProcessHelper.GetFullPathOfProcessByID(gameWindowProcessID);

                        if (Paths.AreEqual(emulatorPath, processPath))
                        {
                            processID = (int)gameWindowProcessID;

                            logger.Trace(emulatorExecutableName + @" with window title matching '" + gameName + @"' found. Process ID = '" + processID + @"'.");

                            return processID;
                        }
                        else
                        {
                            processID = -2;
                        }
                    }
                }
            }
            else
            {
                var emulatorProcessList = Process.GetProcessesByName(emulatorExecutableName);

                if (emulatorProcessList.Any())
                {
                    foreach (var emulatorProcess in emulatorProcessList)
                    {
                        var processPath = emulatorProcess.MainModule?.FileName;
                        if (Paths.AreEqual(emulatorPath, processPath))
                        {
                            processID = -1;

                            if (string.Equals(emulatorProcess.MainWindowTitle, gameName))
                            {
                                processID = emulatorProcess.Id;

                                logger.Trace(emulatorExecutableName + @" with window title matching '" + gameName + @"' found. Process ID = '" + processID + @"'.");

                                return processID;
                            }
                        }
                        else
                        {
                            processID = -2;
                        }
                    }
                }
            }

            switch (processID)
            {
                case 0:
                    logger.Trace(emulatorExecutableName + @" is currently not running.");
                    break;
                case -1:
                    logger.Trace(emulatorExecutableName + @" with window title matching '" + gameName + @"' not found.");
                    break;
                case -2:
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
