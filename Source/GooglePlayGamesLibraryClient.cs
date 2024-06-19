using GooglePlayGamesLibrary.Helper;
using Playnite.Common;
using Playnite.SDK;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace GooglePlayGamesLibrary
{
    public class GooglePlayGamesLibraryClient : LibraryClient
    {
        private readonly ILogger logger;

        // Workaround for 32-bit Playnite
        private readonly bool is32BitPlaynite = Assembly.GetEntryAssembly().GetName().ProcessorArchitecture.Equals(ProcessorArchitecture.X86);

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

                logger.Info(applicationName + @" is no longer running, not necessary to exit client.");
            }
            else
            {
                var servicePath = GooglePlayGames.ServiceExecutablePath;

                foreach (var serviceProcess in serviceProcessList)
                {
                    string processPath;

                    if (is32BitPlaynite)
                    {
                        processPath = ProcessHelper.GetFullPathOfProcessByID((uint)serviceProcess.Id);
                    }
                    else
                    {
                        processPath = serviceProcess.MainModule?.FileName;
                    }

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