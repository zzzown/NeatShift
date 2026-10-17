using System;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace NeatShift.Localization
{
    /// <summary>
    /// XAML 本地化标记扩展，用法：<c>Text="{loc:Translate Main_MoveButton}"</c>
    /// 或对象元素形式 <c>&lt;Prop&gt;&lt;loc:Translate Key="Main_X"/&gt;&lt;/Prop&gt;</c>。
    /// 依赖属性（DependencyObject + DependencyProperty）返回指向
    /// <see cref="TranslationSource"/> 索引器的绑定，语言切换时自动刷新；
    /// 普通 CLR 属性（无法承载 Binding）直接返回当前语言的字符串。
    /// </summary>
    [MarkupExtensionReturnType(typeof(object))]
    public class TranslateExtension : MarkupExtension
    {
        public TranslateExtension()
        {
        }

        public TranslateExtension(string key)
        {
            Key = key;
        }

        /// <summary>资源键，对应 Resources.resx / Resources.zh-CN.resx 中的 data name</summary>
        public string Key { get; set; } = string.Empty;

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (serviceProvider.GetService(typeof(IProvideValueTarget)) is IProvideValueTarget target &&
                target.TargetObject is DependencyObject &&
                target.TargetProperty is DependencyProperty)
            {
                var binding = new Binding($"[{Key}]")
                {
                    Source = TranslationSource.Instance,
                    Mode = BindingMode.OneWay
                };
                return binding.ProvideValue(serviceProvider);
            }

            // 普通 CLR 属性：直接取当前语言文本（静态值）
            return TranslationSource.Instance[Key];
        }
    }
}
