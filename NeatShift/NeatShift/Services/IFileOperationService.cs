using System;
using System.Threading.Tasks;

namespace NeatShift.Services
{
    public interface IFileOperationService
    {
        event EventHandler<ProgressChangedEventArgs> ProgressChanged;
        Task<bool> MoveWithSymbolicLink(string sourcePath, string destinationPath);
    }

    public class ProgressChangedEventArgs : EventArgs
    {
        public int ProgressPercentage { get; set; }
        public string Message { get; set; } = string.Empty;

        /// <summary>是否为错误状态（用于本地化无关的状态判断）</summary>
        public bool IsError { get; set; }

        /// <summary>是否为成功完成状态</summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// 消息对应的资源键（可选）。非空表示 <see cref="Message"/> 由该键按当前语言生成，
        /// 语言切换后界面可据此重新翻译，而不是停留在切换前那一刻的字符串快照。
        /// </summary>
        public string? MessageKey { get; set; }

        /// <summary><see cref="MessageKey"/> 的格式化参数（可选，如异常消息、文件名）</summary>
        public object?[]? MessageArgs { get; set; }
    }
} 