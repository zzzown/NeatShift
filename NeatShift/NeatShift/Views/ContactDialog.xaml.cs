using System.Diagnostics;
using System.Windows;
using ModernWpf.Controls;
using NeatShift.Localization;

namespace NeatShift.Views
{
    public partial class ContactDialog : ContentDialog
    {
        // These will be replaced with actual links later
        private const string REDDIT_LINK = ""; // Temporarily removed as community is being set up
        private const string TELEGRAM_LINK = "https://t.me/NeatShift";
        private const string DISCORD_LINK = "https://discord.gg/tc3AjBRQq9";

        public ContactDialog()
        {
            InitializeComponent();
        }

        private void Reddit_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(TranslationSource.Instance["Contact_RedditComingSoon"],
                          TranslationSource.Instance["Contact_ComingSoon"],
                          MessageBoxButton.OK,
                          MessageBoxImage.Information);
        }

        private void Telegram_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo { FileName = TELEGRAM_LINK, UseShellExecute = true });
        }

        private void Discord_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo { FileName = DISCORD_LINK, UseShellExecute = true });
        }
    }
} 