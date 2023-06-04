using Playnite.Common;
using Playnite.SDK;
using System.Diagnostics;

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
            GooglePlayGames.StartClient();
        }

        public override void Shutdown()
        {
            var serviceProcessList = Process.GetProcessesByName("Service");
            if (serviceProcessList == null)
            {
                logger.Info("Google Play Games is no longer running, not necessary to exit client.");
                return;
            }
            else
            {
                var servicePath = GooglePlayGames.ServiceExecutablePath;

                foreach (var process in serviceProcessList)
                {
                    var processPath = process.MainModule.FileName;
                    if (Paths.AreEqual(servicePath, processPath))
                    {
                        GooglePlayGames.ExitClient();
                        return;
                    }
                }

                logger.Info("Other application(s) named 'Service' is/are running, not necessary to exit client.");
            }
        }
    }
}