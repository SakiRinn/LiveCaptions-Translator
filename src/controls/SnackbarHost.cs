using Wpf.Ui.Controls;
using System;

namespace LiveCaptionsTranslator
{
    class SnackbarHost
    {
        public static Snackbar? mainSnackbar;
        public static MainWindow? mainWindow = (MainWindow)App.Current.MainWindow;

        public static void Show(
            string title = "",
            string message = "",
            SnackbarType type = SnackbarType.Info,
            int width = 500,
            int timeout = 1,
            bool closeButton = false)
        {
            ControlAppearance appearance;
            SymbolIcon icon;

            switch (type)
            {
                case SnackbarType.Warning:
                    appearance = ControlAppearance.Caution;
                    icon = new SymbolIcon(SymbolRegular.Alert24);
                    break;
                case SnackbarType.Error:
                    appearance = ControlAppearance.Danger;
                    icon = new SymbolIcon(SymbolRegular.DismissCircle24);
                    break;
                case SnackbarType.Success:
                    appearance = ControlAppearance.Success;
                    icon = new SymbolIcon(SymbolRegular.CheckmarkCircle24);
                    break;
                default:
                    appearance = ControlAppearance.Secondary;
                    icon = new SymbolIcon(SymbolRegular.Info24);
                    break;
            }

            mainSnackbar ??= new Snackbar(mainWindow?.snackbarHost);
            var snackbar = mainSnackbar;

            snackbar.SetCurrentValue(Snackbar.TitleProperty, title);
            snackbar.SetCurrentValue(System.Windows.Controls.ContentControl.ContentProperty, message);
            snackbar.SetCurrentValue(Snackbar.AppearanceProperty, appearance);
            snackbar.SetCurrentValue(Snackbar.IconProperty, icon);
            snackbar.SetCurrentValue(Snackbar.TimeoutProperty, TimeSpan.FromSeconds(timeout));

            snackbar.MinWidth = width;
            snackbar.IsCloseButtonEnabled = closeButton;

            snackbar.Show(true);
        }

        public static void Show(
            string title,
            string message,
            string type,
            int timeout = 1,
            int width = 500,
            bool closeButton = false)
        {
            Show(title, message, ParseType(type), width, timeout, closeButton);
        }

        private static SnackbarType ParseType(string? type)
        {
            return type?.Trim().ToLowerInvariant() switch
            {
                "warning" => SnackbarType.Warning,
                "error" => SnackbarType.Error,
                "danger" => SnackbarType.Error,
                "success" => SnackbarType.Success,
                "info" => SnackbarType.Info,
                _ => SnackbarType.Info
            };
        }
    }

    public enum SnackbarType
    {
        Warning,
        Error,
        Success,
        Info
    }
}
