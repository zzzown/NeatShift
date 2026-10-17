using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Web;
using System.Windows;
using NeatShift.Localization;

namespace NeatShift.ViewModels
{
    public partial class ErrorDetailsDialogViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _bugDescription;

        [ObservableProperty]
        private string _systemInfo;

        [ObservableProperty]
        private string _additionalContext;

        [ObservableProperty]
        private string _issueTitle;

        public ErrorDetailsDialogViewModel(string errorMessage)
        {
            _issueTitle = TranslationSource.Instance.Format("Err_BugTitle", errorMessage.Split('\n')[0]);
            _bugDescription = errorMessage;
            _systemInfo = $"{TranslationSource.Instance["Err_LabelOS"]} {Environment.OSVersion}\n" +
                          $"{TranslationSource.Instance["Err_LabelAppVersion"]} {Version.Current}";
            _additionalContext = TranslationSource.Instance["Err_SeeLogs"];
        }

        [RelayCommand]
        private void OpenLogsFolder()
        {
            string logDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NeatShift", "logs");
            if (Directory.Exists(logDir))
            {
                Process.Start("explorer.exe", logDir);
            }
            else
            {
                MessageBox.Show(TranslationSource.Instance["Msg_NoLogs"], TranslationSource.Instance["Msg_Information"], MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private string GetFormattedIssueBody()
        {
            var sb = new StringBuilder();
            sb.AppendLine(TranslationSource.Instance["Err_Tpl_DescribeBug"]);
            sb.AppendLine(BugDescription);
            sb.AppendLine(TranslationSource.Instance["Err_Tpl_ToReproduce"]);
            sb.AppendLine(TranslationSource.Instance["Err_Tpl_Steps"]);
            sb.AppendLine(TranslationSource.Instance["Err_Tpl_Step1"]);
            sb.AppendLine(TranslationSource.Instance["Err_Tpl_Step2"]);
            sb.AppendLine(TranslationSource.Instance["Err_Tpl_Step3"]);
            sb.AppendLine(TranslationSource.Instance["Err_Tpl_Step4"]);
            sb.AppendLine(TranslationSource.Instance["Err_Tpl_Expected"]);
            sb.AppendLine(TranslationSource.Instance["Err_Tpl_ExpectedDesc"]);
            sb.AppendLine(TranslationSource.Instance["Err_Tpl_SystemInfo"]);
            sb.AppendLine("```");
            sb.AppendLine(SystemInfo);
            sb.AppendLine("```");
            sb.AppendLine(TranslationSource.Instance["Err_Tpl_AdditionalContext"]);
            sb.AppendLine(AdditionalContext);
            return sb.ToString();
        }

        [RelayCommand]
        private void CopyToClipboard()
        {
            Clipboard.SetText(GetFormattedIssueBody());
            MessageBox.Show(TranslationSource.Instance["Msg_CopiedToClipboard"], TranslationSource.Instance["Msg_Success"], MessageBoxButton.OK, MessageBoxImage.Information);
        }

        [RelayCommand]
        private void SubmitOnGitHub()
        {
            string baseUrl = "https://github.com/BytexGrid/NeatShift/issues/new";
            string title = Uri.EscapeDataString(IssueTitle);
            string body = Uri.EscapeDataString(GetFormattedIssueBody());
            string url = $"{baseUrl}?title={title}&body={body}";

            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
    }
} 