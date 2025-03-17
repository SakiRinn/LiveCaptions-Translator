using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;
using Wpf.Ui.Appearance;

namespace LiveCaptionsTranslator
{
    public partial class InfoPage : Page
    {
        public InfoPage()
        {
            InitializeComponent();
            ApplicationThemeManager.ApplySystemTheme();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}
