using System.Windows.Controls;
using LiveCaptionsTranslator.controllers;
using LiveCaptionsTranslator.models;
using System.Windows;
using Microsoft.Win32;
using Wpf.Ui.Controls;

namespace LiveCaptionsTranslator
{
    public partial class HistoryPage : Page
    {
        int page = 1;
        int maxPage = 1;
        int maxRow = 20;

        public HistoryPage()
        {
            InitializeComponent();
            LoadHistory();

            HistoryMaxRow.SelectedIndex = App.Settings.HistoryMaxRow;
            TranslationController.TranslationLogged += async () => await LoadHistory();
        }

        private async Task LoadHistory()
        {
            var data = await SQLiteHistoryLogger.LoadHistoryAsync(page, maxRow);
            List<TranslationHistoryEntry> history = data.Item1;

            maxPage = (data.Item2 > 0) ? data.Item2 : 1;

            await Dispatcher.InvokeAsync(() =>
            {
                HistoryDataGrid.ItemsSource = history;
                PageNamber.Text = page.ToString() + "/" + maxPage.ToString();
            });
        }

        void PageDown(object sender, RoutedEventArgs e)
        {
            if (page - 1 >= 1)
                page--;
                LoadHistory();

        }
        void PageUp(object sender, RoutedEventArgs e)
        {
            if (page < maxPage)
                page++;
                LoadHistory();
        }

        private async void DeleteHistory(object sender, RoutedEventArgs e)
        {
            var dialog = new ContentDialog
            {
                Title = "Do you want to delete all history?",
                PrimaryButtonText = "Yes",
                CloseButtonText = "No",
                DefaultButton = ContentDialogButton.Close,
                DialogHost = (Application.Current.MainWindow as MainWindow)?.DialogHostContainer,
                Padding = new Thickness(8, 4, 8, 8),
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                page = 1;
                SQLiteHistoryLogger.ClearHistory();
                await LoadHistory();
            }
        }

        private void maxRow_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string text = (e.AddedItems[0] as ComboBoxItem).Tag as string;
            maxRow = Convert.ToInt32(text);
            App.Settings.HistoryMaxRow = HistoryMaxRow.SelectedIndex;

            LoadHistory();

            if (page > maxPage)
            {
                page = maxPage;
                LoadHistory(); ;
            }
        }

        private void ReloadLogs(object sender, RoutedEventArgs e)
        {
            LoadHistory();
        }

        private async void ExportHistory(object sender, RoutedEventArgs e)
        {

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV (*.csv)|*.csv|All file (*.*)|*.*",
                DefaultExt = ".csv",
                FileName = "exported_data.csv",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            };
            
            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    await SQLiteHistoryLogger.ExportToCsv(saveFileDialog.FileName);
                    ShowSnackbar("Saved Success",$"File saved to: {saveFileDialog.FileName}"); 
                }
                catch (Exception ex)
                {
                    ShowSnackbar("Save Failed",$"File saved faild:{ex.Message}");
                }
            }
        }
        
        private void ShowSnackbar(string title,string message, bool isError = false)
        {

            var snackbar = new Snackbar(SnackbarHost)
            {
                Title = title,
                Content = message,
                Appearance = isError ? ControlAppearance.Danger : ControlAppearance.Light,
                Timeout = TimeSpan.FromSeconds(2)
            };

            snackbar.Show(); 
        }

    }
}