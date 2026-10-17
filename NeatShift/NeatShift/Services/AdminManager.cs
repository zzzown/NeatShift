using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Threading.Tasks;
using ModernWpf.Controls;
using NeatShift.Localization;
using NeatShift.Views;

namespace NeatShift.Services
{
    public static class AdminManager
    {
        private static bool _isAdminGranted = false;

        public static bool IsAdminGranted
        {
            get
            {
                if (_isAdminGranted) return true;
                
                // Double check if we're actually running as admin
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                _isAdminGranted = principal.IsInRole(WindowsBuiltInRole.Administrator);
                
                return _isAdminGranted;
            }
        }

        public static async Task<bool> EnsureAdmin(string reason, string actions)
        {
            if (IsAdminGranted) return true;

            // Show our custom prompt first
            var dialog = new AdminPromptDialog(reason, actions);
            if (await dialog.ShowAsync() != ContentDialogResult.Primary)
                return false;

            try
            {
                // Try to restart with admin rights
                var processInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = Process.GetCurrentProcess().MainModule?.FileName,
                    Arguments = "--resume",
                    Verb = "runas"
                };

                Process.Start(processInfo);
                // Shutdown the current instance
                System.Windows.Application.Current.Shutdown();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static class Messages
        {
            public static (string Reason, string Actions) SymbolicLink => (
                TranslationSource.Instance["Admin_ReasonSymbolicLink"],
                TranslationSource.Instance["Admin_ActionsSymbolicLink"]
            );

            public static (string Reason, string Actions) SystemRestore => (
                TranslationSource.Instance["Admin_ReasonSystemRestore"],
                TranslationSource.Instance["Admin_ActionsSystemRestore"]
            );

            public static (string Reason, string Actions) ViewLinks => (
                TranslationSource.Instance["Admin_ReasonViewLinks"],
                TranslationSource.Instance["Admin_ActionsViewLinks"]
            );

            public static (string Reason, string Actions) Backup => (
                TranslationSource.Instance["Admin_ReasonBackup"],
                TranslationSource.Instance["Admin_ActionsBackup"]
            );
        }
    }
} 