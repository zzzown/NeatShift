using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using NeatShift.Localization;

namespace NeatShift.Views
{
    /// <summary>
    /// 指南步骤。标题/描述通过资源键从 <see cref="TranslationSource"/> 动态获取，
    /// 语言切换时经 INotifyPropertyChanged 自动刷新。
    /// </summary>
    public class GuideStep : INotifyPropertyChanged
    {
        public GuideStep()
        {
            // 语言切换广播（PropertyChanged(null)）时刷新绑定
            TranslationSource.Instance.PropertyChanged += (_, _) =>
            {
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(Description));
            };
        }

        public int Number { get; set; }

        /// <summary>标题资源键</summary>
        public string TitleKey { get; set; } = string.Empty;

        /// <summary>描述资源键</summary>
        public string DescriptionKey { get; set; } = string.Empty;

        /// <summary>本地化标题</summary>
        public string Title => TranslationSource.Instance[TitleKey];

        /// <summary>本地化描述</summary>
        public string Description => TranslationSource.Instance[DescriptionKey];

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));
        }
    }
}
