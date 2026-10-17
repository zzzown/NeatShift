using System;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using NeatShift.Localization;

namespace NeatShift.Services
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public class FileOperationService : IFileOperationService
    {
        private readonly ISystemRestoreService _systemRestoreService;
        private readonly ISettingsService _settingsService;

        public event EventHandler<ProgressChangedEventArgs>? ProgressChanged;

        public FileOperationService(ISystemRestoreService systemRestoreService, ISettingsService settingsService)
        {
            _systemRestoreService = systemRestoreService;
            _settingsService = settingsService;
        }

        public async Task<bool> MoveWithSymbolicLink(string sourcePath, string destinationPath)
        {
            string errorMessage;
            try
            {
                OnProgressChanged(0, TranslationSource.Instance.Format("Op_Preparing", Path.GetFileName(sourcePath)));

                // Check if the destination already exists
                if (File.Exists(destinationPath) || Directory.Exists(destinationPath))
                {
                    errorMessage = TranslationSource.Instance["Op_ErrorExists"];
                    OnProgressChanged(0, errorMessage, isError: true);
                    AppLogger.Log(errorMessage);
                    return false;
                }

                // Check if source exists
                if (!File.Exists(sourcePath) && !Directory.Exists(sourcePath))
                {
                    errorMessage = TranslationSource.Instance["Op_ErrorNotExist"];
                    OnProgressChanged(0, errorMessage, isError: true);
                    AppLogger.Log(errorMessage);
                    return false;
                }

                // Check if we have write permission to the destination
                string destDirectory = Path.GetDirectoryName(destinationPath) ?? string.Empty;
                if (!string.IsNullOrEmpty(destDirectory))
                {
                    try
                    {
                        if (!Directory.Exists(destDirectory))
                        {
                            Directory.CreateDirectory(destDirectory);
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        errorMessage = TranslationSource.Instance["Op_ErrorNoPermissionDir"];
                        OnProgressChanged(0, errorMessage, isError: true);
                        AppLogger.Log(errorMessage);
                        return false;
                    }
                }

                if (_settingsService.GetCreateRestorePoint())
                {
                    OnProgressChanged(10, TranslationSource.Instance["Op_CreatingRestorePoint"]);
                    bool restorePointCreated = await _systemRestoreService.CreateRestorePoint(TranslationSource.Instance["Restore_AutoDescription"]);
                    if (!restorePointCreated)
                    {
                        Debug.WriteLine("Failed to create restore point");
                        OnProgressChanged(10, TranslationSource.Instance["Op_WarnRestorePointFailed"]);
                        // Continue with the operation even if restore point creation fails
                    }
                    else
                    {
                        OnProgressChanged(20, TranslationSource.Instance["Op_RestorePointCreated"]);
                    }
                }

                OnProgressChanged(20, TranslationSource.Instance["Op_Moving"]);
                try
                {
                    // Use Task.Run for potentially long-running file operations
                    await Task.Run(() =>
                    {
                        bool sameRoot = string.Equals(Path.GetPathRoot(sourcePath), Path.GetPathRoot(destinationPath), StringComparison.OrdinalIgnoreCase);
                        if (Directory.Exists(sourcePath))
                        {
                            if (sameRoot)
                            {
                                Directory.Move(sourcePath, destinationPath);
                            }
                            else
                            {
                                IOHelper.CopyDirectoryRecursive(sourcePath, destinationPath);
                                Directory.Delete(sourcePath, true);
                            }
                        }
                        else
                        {
                            if (sameRoot)
                            {
                                File.Move(sourcePath, destinationPath);
                            }
                            else
                            {
                                File.Copy(sourcePath, destinationPath);
                                File.Delete(sourcePath);
                            }
                        }
                    });
                }
                catch (IOException ex) when ((ex.HResult & 0x0000FFFF) == 183) // File already exists
                {
                    errorMessage = TranslationSource.Instance["Op_ErrorExists"];
                    OnProgressChanged(0, errorMessage, isError: true);
                    AppLogger.Log(errorMessage);
                    return false;
                }
                catch (UnauthorizedAccessException)
                {
                    errorMessage = TranslationSource.Instance["Op_ErrorNoPermissionMove"];
                    OnProgressChanged(0, errorMessage, isError: true);
                    AppLogger.Log(errorMessage);
                    return false;
                }
                catch (Exception ex)
                {
                    OnProgressChanged(0, TranslationSource.Instance.Format("Op_ErrorMoving", ex.Message), isError: true);
                    AppLogger.Log(ex);
                    return false;
                }

                OnProgressChanged(60, TranslationSource.Instance["Op_CreatingLink"]);
                bool hideLink = _settingsService.GetHideSymbolicLinks();
                var (success, linkErrorMessage) = await Task.Run(() => 
                    IOHelper.CreateSymbolicLink(sourcePath, destinationPath, Directory.Exists(destinationPath), hideLink));

                if (success)
                {
                    OnProgressChanged(100, TranslationSource.Instance["Op_Completed"], isSuccess: true);
                    return true;
                }

                errorMessage = linkErrorMessage;

                // If symbolic link creation failed, try to move the file back
                try
                {
                    await Task.Run(() =>
                    {
                        if (Directory.Exists(destinationPath))
                        {
                            Directory.Move(destinationPath, sourcePath);
                        }
                        else
                        {
                            File.Move(destinationPath, sourcePath);
                        }
                    });
                }
                catch (Exception ex)
                {
                    OnProgressChanged(0, TranslationSource.Instance.Format("Op_ErrorRestoreFailed", errorMessage, ex.Message), isError: true);
                    AppLogger.Log(ex);
                    AppLogger.Log(TranslationSource.Instance.Format("Op_ErrorLinkFailedNoRestore", errorMessage));
                    return false;
                }

                OnProgressChanged(0, TranslationSource.Instance.Format("Op_ErrorGeneric", errorMessage), isError: true);
                AppLogger.Log(TranslationSource.Instance.Format("Op_ErrorLinkFailed", errorMessage));
                return false;
            }
            catch (Exception ex)
            {
                OnProgressChanged(0, TranslationSource.Instance.Format("Op_ErrorUnexpected", ex.Message), isError: true);
                AppLogger.Log(ex);
                return false;
            }
        }

        protected virtual void OnProgressChanged(int progressPercentage, string message, bool isError = false, bool isSuccess = false)
        {
            ProgressChanged?.Invoke(this, new ProgressChangedEventArgs 
            { 
                ProgressPercentage = progressPercentage, 
                Message = message,
                IsError = isError,
                IsSuccess = isSuccess
            });
        }
    }
} 