using Playnite.SDK;

namespace GooglePlayGamesLibrary
{
    public class GooglePlayGamesLibraryClient : LibraryClient
    {
        private readonly ILogger logger;

        public GooglePlayGamesLibraryClient(ILogger logger)
        {
            this.logger = logger;
        }

        public override bool IsInstalled => GooglePlayGames.IsInstalled;

        public override string Icon => GooglePlayGames.Icon;

        public override void Open()
        {
            GooglePlayGames.StartClient(false);
        }

        public override void Shutdown()
        {
            if (!GooglePlayGames.IsClientOpen())
            {
                var applicationName = GooglePlayGames.ApplicationName;

                logger.Info(applicationName + @" is no longer running, not necessary to exit client.");
            }
            else
            {
                GooglePlayGames.ExitClient();
            }
        }
    }
}
