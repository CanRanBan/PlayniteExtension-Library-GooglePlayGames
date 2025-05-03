// This file is part of Google Play Games on PC Library. A Playnite extension to import games available on PC from Google Play Games.
// Copyright CanRanBan, 2023-2025, Licensed under the EUPL-1.2 or later.

using System;
using System.Runtime.InteropServices;
using System.Text;

namespace GooglePlayGamesLibrary.Helper
{
    internal static class ProcessHelper
    {
        #region GetFullPathOfProcess

        internal static string GetFullPathOfProcessByID(uint processID)
        {
            var fullPath = string.Empty;

            var handle = OpenProcess(QueryLimitedInformation, false, processID);

            try
            {
                var size = 1024;
                var builder = new StringBuilder(size);
                var builderCapacity = (uint)builder.Capacity + 1;
                var result = QueryFullProcessImageName(handle, UseWin32PathFormat, builder, ref builderCapacity);

                if (result)
                {
                    fullPath = builder.ToString();
                }

                return fullPath;
            }
            finally
            {
                CloseHandle(handle);
            }
        }

        #endregion GetFullPathOfProcess

        #region PrivateImports

        private const string kernel32DLL = "kernel32.dll";

        private const uint QueryLimitedInformation = 0x1000;

        private const uint UseWin32PathFormat = 0;

        [DllImport(kernel32DLL, SetLastError = true)]
        private static extern IntPtr OpenProcess([In] uint dwDesiredAccess, [In] bool bInheritHandle, [In] uint dwProcessId);

        [DllImport(kernel32DLL, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool QueryFullProcessImageName([In] IntPtr hProcess, [In] uint dwFlags, [Out] StringBuilder lpExeName, [In, Out] ref uint lpdwSize);

        [DllImport(kernel32DLL, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle([In] IntPtr hObject);

        #endregion PrivateImports
    }
}
