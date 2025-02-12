using System.IO;
using Playnite.SDK;
using Playnite.SDK.Models;

namespace GooglePlayGamesLibrary
{
    internal class GooglePlayGamesLibraryMetadataProvider : LibraryMetadataProvider
    {
        private readonly struct GameMetadataFileNames
        {
            internal GameMetadataFileNames(string gameIdentifier)
            {
                gameIconTypeIcon = gameIdentifier + GooglePlayGames.gameIconIdentifierTypeIcon;
                gameIconTypePNG = gameIdentifier + GooglePlayGames.gameIconIdentifierTypePNG;
                gameBackgroundTypePNG = gameIdentifier + GooglePlayGames.GameBackgroundIdentifierTypePNG;
                gameLogoTypePNG = gameIdentifier + GooglePlayGames.gameLogoIdentifierTypePNG;
            }

            internal readonly string gameIconTypeIcon;
            internal readonly string gameIconTypePNG;
            internal readonly string gameBackgroundTypePNG;
            internal readonly string gameLogoTypePNG;
        }

        internal GameMetadata GetMetadata(string gameIdentifier)
        {
            var gameMetadata = new GameMetadata();

            var gameMetadataFileNames = new GameMetadataFileNames(gameIdentifier);

            var iconPath = Path.Combine(GooglePlayGames.ImageCachePath, gameMetadataFileNames.gameIconTypeIcon);
            if (File.Exists(iconPath))
            {
                gameMetadata.Icon = new MetadataFile(iconPath);
            }

            var backgroundPath = Path.Combine(GooglePlayGames.ImageCachePath, gameMetadataFileNames.gameBackgroundTypePNG);
            if (File.Exists(backgroundPath))
            {
                gameMetadata.BackgroundImage = new MetadataFile(backgroundPath);
            }

            return gameMetadata;
        }

        public override GameMetadata GetMetadata(Game game)
        {
            return GetMetadata(game.GameId);
        }
    }
}
