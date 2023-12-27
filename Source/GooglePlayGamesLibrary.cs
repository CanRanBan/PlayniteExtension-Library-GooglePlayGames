using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Controls;

namespace GooglePlayGamesLibrary
{
    public class GooglePlayGamesLibrary : LibraryPlugin
    {
        private static readonly ILogger logger = LogManager.GetLogger();

        private GooglePlayGamesLibrarySettingsViewModel Settings { get; set; }

        public override Guid Id { get; } = Guid.Parse("fcd1bbc9-c3a3-499f-9a4c-8b7c9c8b9de8");

        // Addition of "on PC" for now because only games playable on PC and part of the emulator will be fetched.
        public override string Name => GooglePlayGames.ApplicationName + @" on PC";

        public override string LibraryIcon => GooglePlayGames.Icon;

        // Implementing Client adds ability to open it via special menu in Playnite.
        public override LibraryClient Client { get; } = new GooglePlayGamesLibraryClient(logger);

        public GooglePlayGamesLibrary(IPlayniteAPI api) : base(api)
        {
            Settings = new GooglePlayGamesLibrarySettingsViewModel(this);
            Properties = new LibraryPluginProperties
            {
                CanShutdownClient = true,
                HasSettings = true
            };
        }

        private static List<string> GetInstalledGamesIdentifiers()
        {
            var installedGamesImageCachePath = GooglePlayGames.ImageCachePath;

            var gameBackgroundIdentifier = GooglePlayGames.GameBackgroundIdentifierTypePNG;
            var searchPattern = @"*" + gameBackgroundIdentifier;

            var installedGamesBackgroundList = Directory.GetFiles(installedGamesImageCachePath, searchPattern);

            var installedGamesBackgroundListFileNames =
                installedGamesBackgroundList.Select(Path.GetFileName);

            var installedGamesIdentifierList = installedGamesBackgroundListFileNames.Select(x => x.TrimEndString(gameBackgroundIdentifier, StringComparison.OrdinalIgnoreCase)).ToList();

            return installedGamesIdentifierList;
        }

        internal static List<GameMetadata> GetInstalledGames()
        {
            var installedGames = new List<GameMetadata>();

            var installedGamesIdentifiers = GetInstalledGamesIdentifiers();

            var libraryMetadataProvider = new GooglePlayGamesLibraryMetadataProvider();

            foreach (var gameIdentifier in installedGamesIdentifiers)
            {
                var newGameMedia = libraryMetadataProvider.GetMetadata(gameIdentifier);

                var newGame = new GameMetadata
                {
                    GameId = gameIdentifier,
                    Name = gameIdentifier,
                    Icon = newGameMedia.Icon,
                    BackgroundImage = newGameMedia.BackgroundImage,
                    IsInstalled = true,
                    Platforms = new HashSet<MetadataProperty> { new MetadataSpecProperty("pc_windows") },
                    Source = new MetadataNameProperty(GooglePlayGames.ApplicationName),
                    InstallDirectory = GooglePlayGames.UserDataImageFolderPath
                };

                installedGames.Add(newGame);
            }

            return installedGames;
        }

        public override IEnumerable<GameMetadata> GetGames(LibraryGetGamesArgs args)
        {
            var games = new List<GameMetadata>();

            var applicationName = GooglePlayGames.ApplicationName;

            var importErrorIdentifier = "GooglePlayGamesImportError";

            if (!GooglePlayGames.IsInstalled)
            {
                var installationNotFound = applicationName + " installation not found.";
                PlayniteApi.Notifications.Add("GooglePlayGamesNotInstalled", installationNotFound, NotificationType.Error);
                logger.Error(installationNotFound);
                return games;
            }

            Exception importError = null;

            try
            {
                var installedGames = GetInstalledGames();
                if (installedGames.Any())
                {
                    games.AddRange(installedGames);
                }
            }
            catch (Exception e)
            {
                logger.Error(e, "Failed to import games from: " + applicationName);
                importError = e;
            }

            if (importError != null)
            {
                PlayniteApi.Notifications.Add(importErrorIdentifier,
                                              string.Format(PlayniteApi.Resources.GetString("LOCLibraryImportError"), applicationName) +
                                              Environment.NewLine +
                                              importError.Message,
                                              NotificationType.Error);
            }
            else
            {
                PlayniteApi.Notifications.Remove(importErrorIdentifier);
            }

            return games;
        }

        public override LibraryMetadataProvider GetMetadataDownloader()
        {
            return new GooglePlayGamesLibraryMetadataProvider();
        }

        public override ISettings GetSettings(bool firstRunSettings)
        {
            return Settings;
        }

        public override UserControl GetSettingsView(bool firstRunSettings)
        {
            return new GooglePlayGamesLibrarySettingsView();
        }
    }
}