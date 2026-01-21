using System.Windows;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;
using Wpf.Ui.Controls;
using Wpf.Ui.Appearance;
using LiveCaptionsTranslator.utils;

namespace LiveCaptionsTranslator
{
    public partial class WelcomeWindow : FluentWindow
    {
        private bool _suppressLanguageSelectionChanged;

        public WelcomeWindow()
        {
            InitializeComponent();
            ApplicationThemeManager.ApplySystemTheme();

            Loaded += (s, e) =>
            {
                SystemThemeWatcher.Watch(this, WindowBackdropType.Mica, true);
                InitializeLanguageSelection();

                RequestAutoFitHeight();
            };

            Closed += (_, __) =>
            {
                // No-op: language is persisted immediately when user changes the dropdown.
            };
        }

        public void RequestAutoFitHeight()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var prev = SizeToContent;
                SizeToContent = SizeToContent.WidthAndHeight;
                UpdateLayout();

                const double minW = 650;
                const double maxW = 980;
                const double minH = 420;
                const double maxH = 800;

                Width = Math.Max(minW, Math.Min(maxW, ActualWidth));
                Height = Math.Max(minH, Math.Min(maxH, ActualHeight));

                SizeToContent = prev;
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        public void SyncLanguageSelectionFromSetting()
        {
            if (!IsLoaded)
                return;

            _suppressLanguageSelectionChanged = true;
            try
            {
                var current = Translator.Setting?.UiLanguageFileName;
                WelcomeLanguageBox.SelectedIndex = LocalizationHelper.GetComboIndexFromFileName(current);
            }
            finally
            {
                _suppressLanguageSelectionChanged = false;
            }
        }

        private void PersistCurrentUiLanguage()
        {
            var fileName = LocalizationHelper.GetFileNameFromComboIndex(WelcomeLanguageBox.SelectedIndex);
            if (Translator.Setting is not null)
                Translator.Setting.UiLanguageFileName = fileName;
        }

        private void InitializeLanguageSelection()
        {
            _suppressLanguageSelectionChanged = true;
            try
            {
                var current = Translator.Setting?.UiLanguageFileName;
                if (string.IsNullOrWhiteSpace(current))
                    current = LocalizationHelper.GetActiveLocalizationFileName();

                LocalizationHelper.ActivateLocalizationDictionary(current);
                WelcomeLanguageBox.SelectedIndex = LocalizationHelper.GetComboIndexFromFileName(current);
            }
            finally
            {
                _suppressLanguageSelectionChanged = false;
            }
        }

        private void WelcomeLanguageBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_suppressLanguageSelectionChanged || !IsLoaded)
                return;

            string fileName = LocalizationHelper.GetFileNameFromComboIndex(WelcomeLanguageBox.SelectedIndex);

            LocalizationHelper.ActivateLocalizationDictionary(fileName);
            LocalizationHelper.ApplyFlowDirectionForLocalization(fileName, keepOverlayWindowLtr: false);

            if (Translator.Setting is not null)
                Translator.Setting.UiLanguageFileName = fileName;

            foreach (Window w in Application.Current.Windows)
            {
                if (w is not FrameworkElement root)
                    continue;

                var sp = LocalizationHelper.FindDescendant<SettingPage>(root);
                if (sp is null)
                    continue;

                sp.SyncUILanguageSelectionFromSetting();
                sp.RequestAutoFitWidth();
            }

            var content = Content;
            Content = null;
            Content = content;

            RequestAutoFitHeight();
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

