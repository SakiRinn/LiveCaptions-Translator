using Wpf.Ui.Controls;

namespace LiveCaptionsTranslator.utils
{
    public static class SnackBarHost
    {
        public static Snackbar? snackbar;
        public static void Show(string title, string message, string type = "info")
        {
            ControlAppearance appearance;
            SymbolIcon icon;

            if (type == "warning")
            {
                appearance = ControlAppearance.Caution;
                icon = new SymbolIcon(SymbolRegular.Alert24);
            }
            else if (type == "success")
            {
                appearance = ControlAppearance.Success;
                icon = new SymbolIcon(SymbolRegular.CheckmarkCircle24);
            }
            else if (type == "error")
            {
                appearance = ControlAppearance.Danger;
                icon = new SymbolIcon(SymbolRegular.DismissCircle24);
            }
            else
            {
                appearance = ControlAppearance.Secondary;
                icon = new SymbolIcon(SymbolRegular.Info24);
            }

            snackbar ??= new Snackbar((App.Current.MainWindow as MainWindow)?.SnackbarHost);

            snackbar.SetCurrentValue(Snackbar.TitleProperty, title);
            snackbar.SetCurrentValue(System.Windows.Controls.ContentControl.ContentProperty, message);
            snackbar.SetCurrentValue(Snackbar.AppearanceProperty, appearance);
            snackbar.SetCurrentValue(Snackbar.IconProperty, icon);
            snackbar.SetCurrentValue(Snackbar.TimeoutProperty, TimeSpan.FromSeconds(3));

            snackbar.Show(true);
        }
    }
}
