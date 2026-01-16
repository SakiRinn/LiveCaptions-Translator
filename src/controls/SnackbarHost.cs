using Wpf.Ui.Controls;

namespace LiveCaptionsTranslator
{
    class SnackbarHost
    {
        public static Snackbar? snackbarMain;
        public static Snackbar? snackbarCapture;
        public static MainWindow? mainWindow = (MainWindow)App.Current.MainWindow;

        public static void Show(string title = "", string message = "", string type = "info", 
                                int timeout = 5, int width = 500, bool closeButton = true)
        {
            ControlAppearance appearance;
            SymbolIcon icon;
            Snackbar? snackbar;

            switch (type)
            {
                case "warning":
                    appearance = ControlAppearance.Caution;
                    icon = new SymbolIcon(SymbolRegular.Alert24);
                    break;
                case "success":
                    appearance = ControlAppearance.Success;
                    icon = new SymbolIcon(SymbolRegular.CheckmarkCircle24);
                    break;
                case "error":
                    appearance = ControlAppearance.Danger;
                    icon = new SymbolIcon(SymbolRegular.DismissCircle24);
                    break;
                default:
                    appearance = ControlAppearance.Secondary;
                    icon = new SymbolIcon(SymbolRegular.Info24);
                    break;
            }

            snackbarMain ??= new Snackbar(mainWindow?.snackbarHost);
            snackbar = snackbarMain;

            snackbar.SetCurrentValue(Snackbar.TitleProperty, title);
            snackbar.SetCurrentValue(System.Windows.Controls.ContentControl.ContentProperty, message);
            snackbar.SetCurrentValue(Snackbar.AppearanceProperty, appearance);
            snackbar.SetCurrentValue(Snackbar.IconProperty, icon);
            snackbar.SetCurrentValue(Snackbar.TimeoutProperty, TimeSpan.FromSeconds(timeout));

            snackbar.MinWidth = width;
            snackbar.IsCloseButtonEnabled = closeButton;

            snackbar.Show(true);
        }
    }
}
