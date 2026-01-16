using System.Windows;
using System.Diagnostics;
using System.Windows.Navigation;
using Wpf.Ui.Controls;
using Wpf.Ui.Appearance;

namespace LiveCaptionsTranslator
{
    public partial class WelcomeWindow : FluentWindow
    {
        public WelcomeWindow()
        {
            InitializeComponent();
            ApplicationThemeManager.ApplySystemTheme();

            Loaded += (s, e) =>
            {
                SystemThemeWatcher.Watch(
                    this,
                    WindowBackdropType.Mica,
                    true
                );
            };
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}

