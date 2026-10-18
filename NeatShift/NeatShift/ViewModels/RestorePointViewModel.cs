//    NeatShift - Easily move files while keeping them accessible
//    Copyright (C) 2024 BytexGrid
//
//    This program is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    This program is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NeatShift.Localization;
using NeatShift.Models;

namespace NeatShift.ViewModels
{
    public partial class RestorePointViewModel : ObservableObject
    {
        private readonly Services.ISystemRestoreService _systemRestoreService;
        
        [ObservableProperty]
        private ObservableCollection<RestorePoint> _restorePoints = new();

        /// <summary>当前状态消息对应的资源键；非空表示消息可随语言切换重新翻译</summary>
        private string _statusMessageKey = string.Empty;

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (SetProperty(ref _statusMessage, value))
                {
                    OnPropertyChanged(nameof(HasStatusMessage));
                }
            }
        }

        [ObservableProperty]
        private bool _isLoading = false;

        public bool HasStatusMessage => !string.IsNullOrEmpty(StatusMessage);

        public bool HasRestorePoints => RestorePoints.Count > 0;

        /// <summary>设置状态消息并记下资源键，使其可随语言切换重新翻译</summary>
        private void SetLocalizedStatus(string resourceKey)
        {
            _statusMessageKey = resourceKey ?? string.Empty;
            StatusMessage = string.IsNullOrEmpty(_statusMessageKey)
                ? string.Empty
                : TranslationSource.Instance[_statusMessageKey];
        }

        /// <summary>清除状态消息</summary>
        private void ClearStatus() => SetLocalizedStatus(string.Empty);

        /// <summary>语言切换后按已记录的资源键重新生成状态消息</summary>
        private void RefreshLocalizedStatus()
        {
            if (string.IsNullOrEmpty(_statusMessageKey))
                return;
            StatusMessage = TranslationSource.Instance[_statusMessageKey];
        }

        public RestorePointViewModel()
        {
            _systemRestoreService = new Services.SystemRestoreService();
            TranslationSource.Instance.PropertyChanged += (_, _) => RefreshLocalizedStatus();
            LoadRestorePointsAsync().ConfigureAwait(false);
        }

        [RelayCommand]
        private async Task Refresh()
        {
            IsLoading = true;
            try
            {
                await LoadRestorePointsAsync();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task LoadRestorePointsAsync()
        {
            try
            {
                var points = await _systemRestoreService.GetRestorePoints();
                RestorePoints.Clear();
                foreach (var point in points.OrderByDescending(p => p.CreationTime))
                {
                    RestorePoints.Add(point);
                }

                if (RestorePoints.Count == 0)
                {
                    SetLocalizedStatus("Restore_NoneFound");
                }
                else
                {
                    ClearStatus();
                }

                OnPropertyChanged(nameof(HasRestorePoints));
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading restore points: {ex.Message}");
                SetLocalizedStatus("Restore_LoadFailed");
            }
        }

        [RelayCommand]
        private async Task CreateRestorePoint()
        {
            IsLoading = true;
            ClearStatus();

            try
            {
                // Get current points before trying to create
                var beforePoints = await _systemRestoreService.GetRestorePoints();
                var success = await _systemRestoreService.CreateRestorePoint(TranslationSource.Instance["Restore_ManualDescription"]);
                
                if (!success)
                {
                    // Get points again to check if any were created in the last 3 minutes
                    var afterPoints = await _systemRestoreService.GetRestorePoints();
                    var recentPoint = afterPoints.FirstOrDefault();
                    var timeSinceLastPoint = recentPoint != null ? 
                        (DateTime.Now - recentPoint.CreationTime).TotalMinutes : 
                        double.MaxValue;

                    if (timeSinceLastPoint < 3)
                    {
                        SetLocalizedStatus("Restore_Wait3Min");
                    }
                    else
                    {
                        SetLocalizedStatus("Restore_CreateFailed");
                    }
                }
                else
                {
                    await Refresh();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error creating restore point: {ex.Message}");
                SetLocalizedStatus("Restore_UnexpectedError");
            }
            finally
            {
                IsLoading = false;
            }
        }

        [RelayCommand]
        private void OpenSystemProtection()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "SystemPropertiesProtection.exe",
                    UseShellExecute = true,
                    Verb = "runas" // Request admin privileges
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening System Protection: {ex.Message}");
                MessageBox.Show(
                    TranslationSource.Instance["Restore_OpenProtectionFailed"],
                    TranslationSource.Instance["Msg_Error"],
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private async Task RestoreToPoint(RestorePoint point)
        {
            var result = MessageBox.Show(
                TranslationSource.Instance["Restore_ConfirmRestore"],
                TranslationSource.Instance["Restore_ConfirmRestoreTitle"],
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    await Task.Run(() =>
                    {
                        // Launch the system restore UI with admin privileges
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = "rstrui.exe",
                            UseShellExecute = true,
                            Verb = "runas"
                        });
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error starting system restore: {ex.Message}");
                    MessageBox.Show(
                        TranslationSource.Instance["Restore_StartFailed"],
                        TranslationSource.Instance["Msg_Error"],
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        }

        [RelayCommand]
        private async Task DeleteRestorePoint(RestorePoint point)
        {
            var result = MessageBox.Show(
                TranslationSource.Instance["Restore_ConfirmDelete"],
                TranslationSource.Instance["Msg_ConfirmDeleteTitle"],
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                IsLoading = true;
                try
                {
                    bool success = await _systemRestoreService.DeleteRestorePoint(point.Id);
                    if (success)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            RestorePoints.Remove(point);
                        });

                        MessageBox.Show(
                            TranslationSource.Instance["Restore_DeletedSuccess"],
                            TranslationSource.Instance["Msg_Success"],
                            MessageBoxButton.OK,
                            MessageBoxImage.Information);

                        await Refresh(); // Refresh the list to ensure it's up to date
                    }
                    else
                    {
                        MessageBox.Show(
                            TranslationSource.Instance["Restore_DeleteFailed"],
                            TranslationSource.Instance["Msg_Error"],
                            MessageBoxButton.OK,
                            MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error deleting restore point: {ex.Message}");
                    Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                    MessageBox.Show(
                        TranslationSource.Instance["Restore_DeleteFailed"],
                        TranslationSource.Instance["Msg_Error"],
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
                finally
                {
                    IsLoading = false;
                }
            }
        }
    }
} 