using ModernWpf.Controls;
using NeatShift.Localization;
using NeatShift.Models;
using NeatShift.Services;
using System.Runtime.Versioning;
using System.Windows.Forms;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;

namespace NeatShift.Views
{
    [SupportedOSPlatform("windows7.0")]
    public partial class SettingsDialog : ContentDialog, INotifyPropertyChanged
    {
        private readonly Settings _settings;
        private readonly ISettingsService _settingsService;
        public ICommand BrowseNeatSavesLocationCommand { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));
        }

        public SettingsDialog(ISettingsService settingsService)
        {
            InitializeComponent();
            _settingsService = settingsService;
            _settings = settingsService.LoadSettings();
            DataContext = _settings;

            BrowseNeatSavesLocationCommand = new RelayCommand(BrowseNeatSavesLocation);
            this.PrimaryButtonClick += SettingsDialog_PrimaryButtonClick;

            // 语言选择
            LanguageComboBox.ItemsSource = LanguageService.Options;
            LanguageComboBox.DisplayMemberPath = nameof(LanguageOption.DisplayName);
            LanguageComboBox.SelectedItem = LanguageService.Find(_settings.Language);
        }

        private void BrowseNeatSavesLocation()
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = TranslationSource.Instance["Dlg_SelectSavesLocation"],
                UseDescriptionForTitle = true,
                SelectedPath = _settings.NeatSavesLocation
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _settings.NeatSavesLocation = dialog.SelectedPath;
                OnPropertyChanged(nameof(_settings.NeatSavesLocation));
            }
        }

        private void SettingsDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // 先记录切换前的语言（SaveSettings 会刷新服务缓存，不能放之后比较）
            string previousLanguage = _settingsService.GetLanguage();

            // 语言切换：保存设置并即时生效
            if (LanguageComboBox.SelectedItem is LanguageOption option)
            {
                _settings.Language = option.Code;
            }
            _settingsService.SaveSettings(_settings);

            if (LanguageComboBox.SelectedItem is LanguageOption selected && selected.Code != previousLanguage)
            {
                TranslationSource.Instance.SetLanguage(selected.Code);
            }
        }
    }
} 