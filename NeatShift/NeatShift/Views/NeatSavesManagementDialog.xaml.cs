using System.Windows;
using ModernWpf.Controls;
using NeatShift.Localization;
using NeatShift.Services;
using NeatShift.Models;
using System.Runtime.Versioning;

namespace NeatShift.Views
{
    [SupportedOSPlatform("windows7.0")]
    public partial class NeatSavesManagementDialog : ContentDialog
    {
        private readonly INeatSavesService _neatSavesService;

        public NeatSavesManagementDialog(INeatSavesService neatSavesService)
        {
            InitializeComponent();
            _neatSavesService = neatSavesService;
            SavesListView.ItemsSource = _neatSavesService.GetNeatSaves();
        }

        private async void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (System.Windows.Controls.Button)sender;
            var operation = (NeatSavesOperation)button.DataContext;

            var result = MessageBox.Show(
                TranslationSource.Instance["Saves_ConfirmRestore"],
                TranslationSource.Instance["Saves_ConfirmRestoreTitle"],
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result == MessageBoxResult.Yes)
            {
                var (success, operationId) = await _neatSavesService.RestoreNeatSave(operation.Id);
                if (success)
                {
                    MessageBox.Show(
                        TranslationSource.Instance["Saves_RestoredSuccess"],
                        TranslationSource.Instance["Msg_Success"],
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );

                    // Prompt to delete the NeatSave
                    var deleteResult = MessageBox.Show(
                        TranslationSource.Instance["Saves_AskDeleteAfterRestore"],
                        TranslationSource.Instance["Saves_DeleteNeatSaveTitle"],
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question
                    );

                    if (deleteResult == MessageBoxResult.Yes)
                    {
                        bool deleteSuccess = await _neatSavesService.DeleteNeatSave(operationId);
                        if (deleteSuccess)
                        {
                            // Refresh the list view
                            SavesListView.ItemsSource = _neatSavesService.GetNeatSaves();
                        }
                    }
                }
                else
                {
                    MessageBox.Show(
                        TranslationSource.Instance["Saves_RestoreFailed"],
                        TranslationSource.Instance["Msg_Error"],
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
            }
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            var button = (System.Windows.Controls.Button)sender;
            var operation = (NeatSavesOperation)button.DataContext;

            var result = MessageBox.Show(
                TranslationSource.Instance["Saves_ConfirmDelete"],
                TranslationSource.Instance["Msg_ConfirmDeleteTitle"],
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result == MessageBoxResult.Yes)
            {
                bool success = await _neatSavesService.DeleteNeatSave(operation.Id);
                if (success)
                {
                    MessageBox.Show(
                        TranslationSource.Instance["Saves_BackupDeletedSuccess"],
                        TranslationSource.Instance["Msg_Success"],
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                    SavesListView.ItemsSource = _neatSavesService.GetNeatSaves(); // Refresh the list
                }
                else
                {
                    MessageBox.Show(
                        TranslationSource.Instance["Saves_DeleteFailed"],
                        TranslationSource.Instance["Msg_Error"],
                        MessageBoxButton.OK,
                        MessageBoxImage.Error
                    );
                }
            }
        }
    }
} 