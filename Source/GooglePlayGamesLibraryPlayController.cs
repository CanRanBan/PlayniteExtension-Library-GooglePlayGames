using GooglePlayGamesLibrary.Helper;
using Playnite.Common;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

                ProcessStarter.StartUrl(gameStartURL);

                var gameName = GetGameName(gameIdentifier);

                var processNameMonitor = new ProcessNameMonitor(logger, playniteAPI);
                processNameMonitor.MonitoringStarted += ProcessNameMonitor_GameStarted;
                processNameMonitor.MonitoringStopped += ProcessNameMonitor_GameExited;

                if (!string.IsNullOrEmpty(gameName))
                {
                    processNameMonitor.StartMonitoring(gameName);
                }
                else
                {
                    // Use non empty window titles as tracking fallback.
                    processNameMonitor.StartMonitoring(gameName, allowEmptyName: true);
                }
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