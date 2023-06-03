using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GooglePlayGamesLibrary
{
    internal class GooglePlayGames
    {
        public static string DataPath
        {
            get
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"Software\Google\Play Games"))
                {
                    if (key?.GetValueNames().Contains("UserLocalAppDataRoot") == true)
                    {
                        return key.GetValue("UserLocalAppDataRoot")?.ToString() + @"\Google\Play Games";
                    }
                }

                return string.Empty;
            }
        }

        public static string InstallationPath
        {
            get
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"Software\Google\Play Games"))
                {
                    if (key?.GetValueNames().Contains("InstallFolder") == true)
                    {
                        return key.GetValue("InstallFolder")?.ToString();
                    }
                }

                return string.Empty;
            }
        }

        public static bool IsInstalled
        {
            get
            {
                var path = InstallationPath;
                return !string.IsNullOrEmpty(path) && Directory.Exists(path);
            }
        }
    }
}
