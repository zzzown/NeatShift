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
                OnLocalizedProgressChanged(0, "Op_Preparing", new object?[] { Path.GetFileName(sourcePath) });

                // Check if the destination already exists
                if (File.Exists(destinationPath) || Directory.Exists(destinationPath))
                {
                    errorMessage = TranslationSource.Instance["Op_ErrorExists"];
                    OnLocalizedProgressChanged(0, "Op_ErrorExists", isError: true);
                    AppLogger.Log(errorMessage);
                    return false;
                }

                // Check if source exists
                if (!File.Exists(sourcePath) && !Directory.Exists(sourcePath))
                {
                    errorMessage = TranslationSource.Instance["Op_ErrorNotExist"];
                    OnLocalizedProgressChanged(0, "Op_ErrorNotExist", isError: true);
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
                        OnLocalizedProgressChanged(0, "Op_ErrorNoPermissionDir", isError: true);
                        AppLogger.Log(errorMessage);
                        return false;
                    }
                }

                if (_settingsService.GetCreateRestorePoint())
                {
                    OnLocalizedProgressChanged(10, "Op_CreatingRestorePoint");
                    bool restorePointCreated = await _systemRestoreService.CreateRestorePoint(TranslationSource.Instance["Restore_AutoDescription"]);
                    if (!restorePointCreated)
                    {
                        Debug.WriteLine("Failed to create restore point");
                        OnLocalizedProgressChanged(10, "Op_WarnRestorePointFailed");
                        // Continue with the operation even if restore point creation fails
                    }
                    else
                    {
                        OnLocalizedProgressChanged(20, "Op_RestorePointCreated");
                    }
                }

                OnLocalizedProgressChanged(20, "Op_Moving");
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
                    OnLocalizedProgressChanged(0, "Op_ErrorExists", isError: true);
                    AppLogger.Log(errorMessage);
                    return false;
                }
                catch (UnauthorizedAccessException)
                {
                    errorMessage = TranslationSource.Instance["Op_ErrorNoPermissionMove"];
                    OnLocalizedProgressChanged(0, "Op_ErrorNoPermissionMove", isError: true);
                    AppLogger.Log(errorMessage);
                    return false;
                }
                catch (Exception ex)
                {
                    OnLocalizedProgressChanged(0, "Op_ErrorMoving", new object?[] { ex.Message }, isError: true);
                    AppLogger.Log(ex);
                    return false;
                }

                OnLocalizedProgressChanged(60, "Op_CreatingLink");
                bool hideLink = _settingsService.GetHideSymbolicLinks();
                var (success, linkErrorMessage) = await Task.Run(() => 
                    IOHelper.CreateSymbolicLink(sourcePath, destinationPath, Directory.Exists(destinationPath), hideLink));

                if (success)
                {
                    OnLocalizedProgressChanged(100, "Op_Completed", isSuccess: true);
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
                    OnLocalizedProgressChanged(0, "Op_ErrorRestoreFailed", new object?[] { errorMessage, ex.Message }, isError: true);
                    AppLogger.Log(ex);
                    AppLogger.Log(TranslationSource.Instance.Format("Op_ErrorLinkFailedNoRestore", errorMessage));
                    return false;
                }

                OnLocalizedProgressChanged(0, "Op_ErrorGeneric", new object?[] { errorMessage }, isError: true);
                AppLogger.Log(TranslationSource.Instance.Format("Op_ErrorLinkFailed", errorMessage));
                return false;
            }
            catch (Exception ex)
            {
                OnLocalizedProgressChanged(0, "Op_ErrorUnexpected", new object?[] { ex.Message }, isError: true);
                AppLogger.Log(ex);
                return false;
            }
        }

        /// <summary>
        /// 发送按资源键存储的进度消息。与 <see cref="OnProgressChanged"/> 的区别：
        /// 事件参数额外携带资源键与格式化参数，界面在语言切换后可据此重新翻译，
        /// 而不是停留在消息产生那一刻的字符串快照。
        /// </summary>
        protected virtual void OnLocalizedProgressChanged(int progressPercentage, string messageKey, bool isError = false, bool isSuccess = false)
            => OnLocalizedProgressChanged(progressPercentage, messageKey, null, isError, isSuccess);

        /// <inheritdoc cref="OnLocalizedProgressChanged(int, string, bool, bool)"/>
        protected virtual void OnLocalizedProgressChanged(int progressPercentage, string messageKey, object?[]? messageArgs, bool isError = false, bool isSuccess = false)
        {
            var args = messageArgs ?? Array.Empty<object?>();
            ProgressChanged?.Invoke(this, new ProgressChangedEventArgs
            {
                ProgressPercentage = progressPercentage,
                Message = TranslationSource.Instance.Format(messageKey, args),
                MessageKey = messageKey,
                MessageArgs = args,
                IsError = isError,
                IsSuccess = isSuccess
            });
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