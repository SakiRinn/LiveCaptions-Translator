using Wpf.Ui.Controls;

namespace LiveCaptionsTranslator.utils
{
    public static class SnackBarHost
    {
        public static void Show(string title, string message, string type = "info")
        {
            ControlAppearance appearance = ControlAppearance.Secondary;
            SymbolIcon icon = new SymbolIcon(SymbolRegular.Info24);

            if (type == "warning")
            {
                appearance = ControlAppearance.Caution;
                icon = new SymbolIcon(SymbolRegular.Alert24);
            }
            else if (type == "success")
            {
                appearance = ControlAppearance.Success;
                icon = new SymbolIcon(SymbolRegular.Checkmark24);
            }
            else if (type == "error")
            {
                appearance = ControlAppearance.Danger;
                icon = new SymbolIcon(SymbolRegular.Dismiss24);
            }

            var snackBarHost = (App.Current.MainWindow as MainWindow)?.SnackbarHost;
            var snackbar = new Snackbar(snackBarHost)
            {
                Title = title,
                Content = message,
                Appearance = appearance,
                Timeout = TimeSpan.FromSeconds(3),
                Icon = icon,
            };

            snackbar.Show();
        }
    }
}
