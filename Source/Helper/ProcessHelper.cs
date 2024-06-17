namespace GooglePlayGamesLibrary.Helper
{
    internal class ProcessHelper
    {
        #region FindWindowByTitle

        // Source: https://stackoverflow.com/questions/19867402/how-can-i-use-enumwindows-to-find-windows-with-a-specific-caption-title

        /// <summary> Find all windows matching the given title text </summary>
        /// <param name="titleText"> The text that the window title must match. </param>
        internal static IEnumerable<IntPtr> FindWindowsMatchingText(string titleText)
        {
            return FindWindows(delegate (IntPtr hWnd, IntPtr lParam)
            {
                return GetWindowText(hWnd).Equals(titleText);
            });
        }

        /// <summary> Find all windows that match the given filter </summary>
        /// <param name="filter"> A delegate that returns true for windows
        ///    that should be returned and false for windows that should
        ///    not be returned </param>
        private static IEnumerable<IntPtr> FindWindows(EnumWindowsProc filter)
        {
            List<IntPtr> windows = new List<IntPtr>();

            EnumWindows(delegate (IntPtr hWnd, IntPtr lParam)
            {
                if (filter(hWnd, lParam))
                {
                    // Only add the windows that pass the filter
                    windows.Add(hWnd);
                }

                // But return true here so that we iterate all windows
                return true;
            }, IntPtr.Zero);

            return windows;
        }

        /// <summary> Get the text for the window pointed to by hWnd </summary>
        private static string GetWindowText(IntPtr hWnd)
        {
            int size = GetWindowTextLength(hWnd);
            if (size > 0)
            {
                var builder = new StringBuilder(size + 1);
                GetWindowText(hWnd, builder, builder.Capacity);
                return builder.ToString();
            }

            return string.Empty;
        }

        #endregion FindWindowByTitle

        #region PrivateImports

        private const string user32DLL = "user32.dll";

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport(user32DLL)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport(user32DLL, CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport(user32DLL, CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport(user32DLL, SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        #endregion PrivateImports
    }
}
