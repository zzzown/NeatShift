using System;
using System.ComponentModel;
using System.Linq;

namespace NeatShift.Localization
{
    /// <summary>界面语言选项</summary>
    public sealed class LanguageOption : INotifyPropertyChanged
    {
        public LanguageOption(string code, string nameKey)
        {
            Code = code;
            NameKey = nameKey;
            // 语言切换后刷新下拉框显示文本
            TranslationSource.Instance.PropertyChanged += (_, _) =>
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayName)));
        }

        /// <summary>设置值（auto / en / zh-CN）</summary>
        public string Code { get; }

        /// <summary>显示名称对应的资源键</summary>
        public string NameKey { get; }

        /// <summary>本地化后的显示名称</summary>
        public string DisplayName => TranslationSource.Instance[NameKey];

        public event PropertyChangedEventHandler? PropertyChanged;
    }

    /// <summary>可用语言列表服务</summary>
    public static class LanguageService
    {
        /// <summary>全部语言选项</summary>
        public static LanguageOption[] Options { get; } =
        {
            new("auto", "Lang_OptionAuto"),
            new("en", "Lang_OptionEnglish"),
            new("zh-CN", "Lang_OptionChinese"),
        };

        /// <summary>按设置值取选项；无法识别时返回“跟随系统”</summary>
        public static LanguageOption Find(string? code) =>
            Options.FirstOrDefault(o => o.Code == code) ?? Options[0];
    }
}
