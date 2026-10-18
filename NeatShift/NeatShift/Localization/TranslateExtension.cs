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
    /// 模板 / Setter 上下文返回 Binding 本身，由 WPF 在模板实例化时应用为实时绑定
    /// （与原生 <c>{Binding}</c> 在模板中的延迟求值一致）；
    /// 其他无法承载 Binding 的场景（普通 CLR 属性）返回当前语言的字符串。
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
            var binding = new Binding($"[{Key}]")
            {
                Source = TranslationSource.Instance,
                Mode = BindingMode.OneWay
            };

            if (serviceProvider?.GetService(typeof(IProvideValueTarget)) is IProvideValueTarget target &&
                target.TargetObject is DependencyObject &&
                target.TargetProperty is DependencyProperty)
            {
                return binding.ProvideValue(serviceProvider);
            }

            // 模板 / Setter 上下文：返回 Binding 本身（延迟求值，模板实例化时成为实时绑定）
            // 其他无目标信息的场景由 WPF 按绑定求值或回退静态文本
            return binding;
        }
    }
}
