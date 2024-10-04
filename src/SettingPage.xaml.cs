using System.Windows.Controls;

namespace LiveCaptionsTranslator
{
    public partial class SettingPage : Page
    {
        public SettingPage()
        {
            InitializeComponent();
            DataContext = App.Settings;
        }
    }
}