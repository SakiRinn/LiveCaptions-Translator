using System.Windows.Controls;
using LiveCaptionsTranslator.controllers;
using LiveCaptionsTranslator.models;
using System.Windows;

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
            MaxRowBox.SelectedIndex = App.Settings.HistoryMaxRow;

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
            {
                page--;
                LoadHistory();

            }
        }
        void PageUp(object sender, RoutedEventArgs e)
        {
            if (page < maxPage)
            {
                page++;
                LoadHistory();

            }
        }

        void RemoveLogs(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Do you want to clear translation storage history?",
                    "Clear history",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                SQLiteHistoryLogger.ClaerHistory();
                LoadHistory();
            }
        }

        private void maxRow_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string text = (e.AddedItems[0] as ComboBoxItem).Tag as string;
            maxRow = Convert.ToInt32(text);
            App.Settings.HistoryMaxRow = MaxRowBox.SelectedIndex;

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
    }
}