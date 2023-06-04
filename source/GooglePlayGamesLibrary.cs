using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace GooglePlayGamesLibrary
{
    public class GooglePlayGamesLibrary : LibraryPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private GooglePlayGamesLibrarySettingsViewModel settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("fcd1bbc9-c3a3-499f-9a4c-8b7c9c8b9de8");

        // Addition of "on PC" for now because only games playable on PC and part of the emulator will be fetched.
        public override string Name => "Google Play Games on PC Library";

        // Implementing Client adds ability to open it via special menu in playnite.
        public override LibraryClient Client { get; } = new GooglePlayGamesLibraryClient(logger);

        public GooglePlayGamesLibrary(IPlayniteAPI api) : base(api)
        {
            settings = new GooglePlayGamesLibrarySettingsViewModel(this);
            Properties = new LibraryPluginProperties
            {
                CanShutdownClient = true,
                HasSettings = true
            };
        }

        public override IEnumerable<GameMetadata> GetGames(LibraryGetGamesArgs args)
        {
            // Return list of user's games.
            return new List<GameMetadata>()
            {
                new GameMetadata()
                {
                    Name = "Notepad",
                    GameId = "notepad",
                    GameActions = new List<GameAction>
                    {
                        new GameAction()
                        {
                            Type = GameActionType.File,
                            Path = "notepad.exe",
                            IsPlayAction = true
                        }
                    },
                    IsInstalled = true,
                    Icon = new MetadataFile(@"c:\Windows\notepad.exe")
                },
                new GameMetadata()
                {
                    Name = "Calculator",
                    GameId = "calc",
                    GameActions = new List<GameAction>
                    {
                        new GameAction()
                        {
                            Type = GameActionType.File,
                            Path = "calc.exe",
                            IsPlayAction = true
                        }
                    },
                    IsInstalled = true,
                    Icon = new MetadataFile(@"https://playnite.link/applogo.png"),
                    BackgroundImage = new MetadataFile(@"https://playnite.link/applogo.png")
                }
            };
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new GooglePlayGamesLibrarySettingsView();
        }
    }
}