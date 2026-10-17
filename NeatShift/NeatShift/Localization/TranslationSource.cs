using System;
using System.ComponentModel;
using System.Globalization;
using System.Resources;
using System.Windows;
using System.Windows.Markup;

namespace NeatShift.Localization
{
    /// <summary>
    /// 运行时本地化字符串查找与语言切换中心。
    /// XAML 通过 <see cref="TranslateExtension"/> 绑定到索引器 <c>this[key]</c>，
    /// 切换语言时广播属性变更，所有绑定自动刷新，无需重启应用。
    /// </summary>
    public sealed class TranslationSource : INotifyPropertyChanged
    {
        /// <summary>中性（默认）文化：英文</summary>
        public const string NeutralCulture = "en";

        /// <summary>简体中文文化</summary>
        public const string ChineseCulture = "zh-CN";

        /// <summary>已支持的文化列表</summary>
        public static readonly string[] SupportedCultures = { NeutralCulture, ChineseCulture };

        private static readonly Lazy<TranslationSource> LazyInstance = new(() => new TranslationSource());
        public static TranslationSource Instance => LazyInstance.Value;

        private static readonly ResourceManager ResourceManager =
            new("NeatShift.Properties.Resources", typeof(TranslationSource).Assembly);

        private CultureInfo _currentCulture = new(NeutralCulture);

        public event PropertyChangedEventHandler? PropertyChanged;

        private TranslationSource() { }

        /// <summary>当前界面文化</summary>
        public CultureInfo CurrentCulture
        {
            get => _currentCulture;
            private set
            {
                if (!Equals(_currentCulture, value))
                {
                    _currentCulture = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentCulture)));
                }
            }
        }

        /// <summary>
        /// 按当前语言获取资源字符串。
        /// 缺失时回退英文（中性资源），仍缺失返回 [!key] 便于排查漏翻。
        /// </summary>
        public string this[string key]
        {
            get
            {
                var value = ResourceManager.GetString(key, _currentCulture);
                if (string.IsNullOrEmpty(value))
                    value = ResourceManager.GetString(key, CultureInfo.InvariantCulture);
                return string.IsNullOrEmpty(value) ? $"[!{key}]" : value;
            }
        }

        /// <summary>获取资源字符串并按当前语言格式化参数</summary>
        public string Format(string key, params object?[] args)
        {
            var format = this[key];
            return args is { Length: > 0 } ? string.Format(format, args) : format;
        }

        /// <summary>
        /// 应用语言。cultureName 为 null / 空 / "auto" 时跟随操作系统 UI 语言
        /// （中文系统 → 简体中文，其他 → 英文）。不受支持的文化统一回退英文。
        /// </summary>
        public void SetLanguage(string? cultureName)
        {
            CultureInfo culture;
            if (string.IsNullOrWhiteSpace(cultureName) || cultureName == "auto")
            {
                var system = CultureInfo.InstalledUICulture;
                culture = system.TwoLetterISOLanguageName.Equals("zh", StringComparison.OrdinalIgnoreCase)
                    ? new CultureInfo(ChineseCulture)
                    : new CultureInfo(NeutralCulture);
            }
            else
            {
                try
                {
                    culture = new CultureInfo(cultureName);
                }
                catch (CultureNotFoundException)
                {
                    culture = new CultureInfo(NeutralCulture);
                }

                // 不受支持的文化回退英文（简体中文语系的其他区域也按简体中文处理）
                if (culture.TwoLetterISOLanguageName.Equals("zh", StringComparison.OrdinalIgnoreCase))
                    culture = new CultureInfo(ChineseCulture);
                else if (!culture.Name.Equals(NeutralCulture, StringComparison.OrdinalIgnoreCase))
                    culture = new CultureInfo(NeutralCulture);
            }

            ApplyLanguage(culture);
        }

        /// <summary>直接以 <see cref="CultureInfo"/> 应用语言</summary>
        public void SetLanguage(CultureInfo culture) => ApplyLanguage(culture);

        private void ApplyLanguage(CultureInfo culture)
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;

            // WPF 绑定中的日期/数字格式化依赖 FrameworkElement.Language
            if (Application.Current != null)
            {
                var language = XmlLanguage.GetLanguage(culture.IetfLanguageTag);
                foreach (Window window in Application.Current.Windows)
                    window.Language = language;
            }

            CurrentCulture = culture;
            // 广播全部属性变更，令所有 {loc:Translate} 绑定刷新
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }
    }
}
