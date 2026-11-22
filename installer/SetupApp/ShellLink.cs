using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ProjectionSetup
{
    /// <summary>
    /// 用 IShellLink + IPersistFile 直接建 .lnk（不依赖 PowerShell）。
    /// 失败时调用方会回退到 release/shortcut.ps1 的同一份脚本。
    /// </summary>
    internal static class ShellLink
    {
        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        private class ShellLinkCoClass
        {
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        private interface IShellLinkW
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cch, IntPtr pfd, uint fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cch);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cch);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cch);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out short pwHotkey);
            void SetHotkey(short wHotkey);
            void GetShowCmd(out int piShowCmd);
            void SetShowCmd(int iShowCmd);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cch, out int piIcon);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
            void Resolve(IntPtr hwnd, uint fFlags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("0000010b-0000-0000-C000-000000000046")]
        private interface IPersistFile
        {
            void GetClassID(out Guid pClassID);
            [PreserveSig]
            int IsDirty();
            void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
            void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);
            void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
            void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
        }

        /// <summary>创建/覆盖快捷方式。出错直接抛异常，由调用方决定是否回退。</summary>
        public static void Create(string lnkPath, string targetPath, string workingDirectory, string iconPath, string description)
        {
            IShellLinkW link = (IShellLinkW)new ShellLinkCoClass();
            link.SetPath(targetPath);
            link.SetWorkingDirectory(workingDirectory);
            if (!string.IsNullOrEmpty(description)) link.SetDescription(description);
            if (!string.IsNullOrEmpty(iconPath)) link.SetIconLocation(iconPath, 0);
            ((IPersistFile)link).Save(lnkPath, true);
            Marshal.FinalReleaseComObject(link);
        }

        /// <summary>读已有 .lnk 的目标路径（只用于日志/校验，读不到就抛异常）。</summary>
        public static string ReadTarget(string lnkPath)
        {
            IShellLinkW link = (IShellLinkW)new ShellLinkCoClass();
            try
            {
                ((IPersistFile)link).Load(lnkPath, 0);
                StringBuilder sb = new StringBuilder(1024);
                link.GetPath(sb, sb.Capacity, IntPtr.Zero, 0);
                return sb.ToString();
            }
            finally
            {
                try { Marshal.FinalReleaseComObject(link); } catch { }
            }
        }
    }
}
