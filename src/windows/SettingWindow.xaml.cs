using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace LiveCaptionsTranslator
{
    public partial class SettingWindow : FluentWindow
    {
        public SettingWindow()
        {
            InitializeComponent();
            ApplicationThemeManager.ApplySystemTheme();
            DataContext = Translator.Setting;

            Loaded += (sender, args) =>
            {
                SystemThemeWatcher.Watch(
                    this,
                    WindowBackdropType.Mica,
                    true
                );
            };
        }
    }
}

