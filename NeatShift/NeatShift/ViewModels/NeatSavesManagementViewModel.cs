using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeatShift.Localization;
using NeatShift.Services;
using System.Windows;

namespace NeatShift.ViewModels
{
    public partial class NeatSavesManagementViewModel : ObservableObject
    {
        private readonly INeatSavesService _neatSavesService;
        private readonly string _operationId;

        [ObservableProperty]
        private string _sourcePath;

        [ObservableProperty]
        private string _destinationPath;

        public NeatSavesManagementViewModel(INeatSavesService neatSavesService, string operationId, string sourcePath, string destinationPath)
        {
            _neatSavesService = neatSavesService;
            _operationId = operationId;
            _sourcePath = sourcePath;
            _destinationPath = destinationPath;
        }

        [RelayCommand]
        private async Task RestoreFiles()
        {
            var result = MessageBox.Show(
                TranslationSource.Instance["Saves_ConfirmRestore"],
                TranslationSource.Instance["Saves_ConfirmRestoreTitle"],
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result == MessageBoxResult.Yes)
            {
                var (success, operationId) = await _neatSavesService.RestoreNeatSave(_operationId);
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
                            MessageBox.Show(
                                TranslationSource.Instance["Saves_DeletedSuccess"],
                                TranslationSource.Instance["Msg_Success"],
                                MessageBoxButton.OK,
                                MessageBoxImage.Information
                            );
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

        [RelayCommand]
        private async Task DeleteBackup()
        {
            var result = MessageBox.Show(
                TranslationSource.Instance["Saves_ConfirmDelete"],
                TranslationSource.Instance["Msg_ConfirmDeleteTitle"],
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result == MessageBoxResult.Yes)
            {
                bool success = await _neatSavesService.DeleteNeatSave(_operationId);
                if (success)
                {
                    MessageBox.Show(
                        TranslationSource.Instance["Saves_BackupDeletedSuccess"],
                        TranslationSource.Instance["Msg_Success"],
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
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

        [RelayCommand]
        private void ManageNeatSaves()
        {
            // TODO: Implement NeatSaves management dialog
            MessageBox.Show(
                TranslationSource.Instance["Saves_ComingSoon"],
                TranslationSource.Instance["Msg_Information"],
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }
    }
}
