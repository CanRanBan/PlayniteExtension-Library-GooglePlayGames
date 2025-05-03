// This file is part of Google Play Games on PC Library. A Playnite extension to import games available on PC from Google Play Games.
// Copyright CanRanBan, 2023-2025, Licensed under the EUPL-1.2 or later.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using GooglePlayGamesLibrary.Helper;
using Playnite.Common;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using static GooglePlayGamesLibrary.GooglePlayGamesLibrary;

namespace GooglePlayGamesLibrary
{
    internal class GooglePlayGamesLibraryPlayController : PlayController
    {
        private readonly ILogger logger;
        private readonly IPlayniteAPI playniteAPI;

        private readonly Dictionary<string, GameData> shortcutData = GetInstalledGamesShortcutData();

        private Stopwatch stopWatch;

        public GooglePlayGamesLibraryPlayController(ILogger logger, IPlayniteAPI playniteAPI, Game game) : base(game)
        {
            this.logger = logger;
            this.playniteAPI = playniteAPI;

            Name = GooglePlayGames.StartWithClient;
        }

        private string GetGameStartURL(string gameIdentifier)
        {
            var gameStartURL = string.Empty;

            if (shortcutData.ContainsKey(gameIdentifier))
            {
                gameStartURL = shortcutData[gameIdentifier].gameStartURL;
            }
            else
            {
                gameStartURL = string.Join(string.Empty,
                    "googleplaygames://launch/?id=",
                    gameIdentifier,
                    "&lid=1&pid=1");
            }

            return gameStartURL;
        }

        private string GetGameName(string gameIdentifier)
        {
            var gameName = string.Empty;

            if (shortcutData.ContainsKey(gameIdentifier))
            {
                gameName = shortcutData[gameIdentifier].gameName;
            }

            return gameName;
        }

        private bool GetUseTrackingFallback(string gameIdentifier)
        {
            bool useTrackingFallback = true;

            if (shortcutData.ContainsKey(gameIdentifier))
            {
                useTrackingFallback = shortcutData[gameIdentifier].useTrackingFallback;
            }

            return useTrackingFallback;
        }

        private async void StartGameAsync(string gameStartURL, string gameIdentifier)
        {
            var startGameAsyncErrorIdentifier = "GooglePlayGamesStartGameAsyncError";

            Exception startGameAsyncError = null;
            var startGameAsyncDetailedErrorMessage = "Failed to start game async. Start client succeeded: {0}. Start client skipped: {1}.";

            var startClientSucceeded = false;
            var startClientSkipped = false;

            try
            {
                if (!GooglePlayGames.IsClientOpen())
                {
                    GooglePlayGames.StartClient(true);

                    while (!GooglePlayGames.IsClientOpen())
                    {
                        await Task.Delay(1000);
                    }

                    startClientSucceeded = true;
                    await Task.Delay(5000);
                }
                else
                {
                    startClientSkipped = true;
                }

                ProcessStarter.StartUrl(gameStartURL);

                var gameName = GetGameName(gameIdentifier);
                var useTrackingFallback = GetUseTrackingFallback(gameIdentifier);

                var processNameMonitor = new ProcessNameMonitor(logger, playniteAPI);
                processNameMonitor.MonitoringStarted += ProcessNameMonitor_GameStarted;
                processNameMonitor.MonitoringStopped += ProcessNameMonitor_GameExited;

                // Use non empty window titles as tracking fallback.
                processNameMonitor.StartMonitoring(gameName, allowEmptyName: useTrackingFallback);
            }
            catch (Exception e)
            {
                logger.Error(e, string.Format(startGameAsyncDetailedErrorMessage, startClientSucceeded, startClientSkipped));
                startGameAsyncError = e;
            }

            if (startGameAsyncError != null)
            {
                var startGameAsyncErrorMessage = "Failed to start game. Additional details are depicted in 'extensions.log'.";
                playniteAPI.Notifications.Add(startGameAsyncErrorIdentifier, startGameAsyncErrorMessage, NotificationType.Error);
            }
            else
            {
                playniteAPI.Notifications.Remove(startGameAsyncErrorIdentifier);
            }
        }

        public override void Play(PlayActionArgs args)
        {
            Dispose();

            if (Game.IsInstalled)
            {
                var gameIdentifier = Game.GameId;

                var gameStartURL = GetGameStartURL(gameIdentifier);

                if (string.IsNullOrEmpty(gameStartURL))
                {
                    var missingGameStartURLError = @"No valid game start URL found for '" + Game.Name + @"' with game ID: '" + gameIdentifier + @"'.";

                    logger.Error(missingGameStartURLError);

                    var missingGameStartURLErrorMessage = missingGameStartURLError + "\n" +
                        "\n" +
                        @"If the game is properly installed restarting " + GooglePlayGames.ApplicationName + " might fix this issue.\n\n" +
                        GooglePlayGames.ApplicationName + " creates shortcuts with game start URL data inside:\n"
                        + @"%AppData%\Microsoft\Windows\Start Menu\Programs\Google Play Games";

                    playniteAPI.Dialogs.ShowErrorMessage(missingGameStartURLErrorMessage);

                    InvokeOnStopped(new GameStoppedEventArgs());

                    return;
                }

                StartGameAsync(gameStartURL, gameIdentifier);
            }
            else
            {
                InvokeOnStopped(new GameStoppedEventArgs());
            }
        }

        private void ProcessNameMonitor_GameStarted(object sender, ProcessNameMonitor.MonitoringStartedEventArgs args)
        {
            stopWatch = Stopwatch.StartNew();
            InvokeOnStarted(new GameStartedEventArgs() { StartedProcessId = args.GameProcessID });
        }

        private void ProcessNameMonitor_GameExited(object sender, EventArgs args)
        {
            stopWatch?.Stop();
            InvokeOnStopped(new GameStoppedEventArgs(Convert.ToUInt64(stopWatch?.Elapsed.TotalSeconds ?? 0)));
        }
    }
}
