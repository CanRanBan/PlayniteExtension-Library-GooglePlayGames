using Playnite.Common;
using Playnite.SDK;
using System.Diagnostics;
using System.Linq;

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
            var serviceExecutableName = GooglePlayGames.ServiceExecutableName;

            var serviceProcessList = Process.GetProcessesByName(serviceExecutableName);

            if (!serviceProcessList.Any())
            {
                var applicationName = GooglePlayGames.ApplicationName;

                logger.Info(applicationName + @"is no longer running, not necessary to exit client.");
            }
            else
            {
                var servicePath = GooglePlayGames.ServiceExecutablePath;

                foreach (var serviceProcess in serviceProcessList)
                {
                    var processPath = serviceProcess.MainModule?.FileName;
                    if (Paths.AreEqual(servicePath, processPath))
                    {
                        GooglePlayGames.ExitClient();
                        return;
                    }
                }

                logger.Info(@"Other application(s) named '" + serviceExecutableName + @"' is/are running, not necessary to exit client.");
            }
        }
    }
}