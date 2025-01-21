using System.Windows.Controls;
using LiveCaptionsTranslator.controllers;
using LiveCaptionsTranslator.models;

namespace LiveCaptionsTranslator
{
    public partial class HistoryPage : Page
    {
        public HistoryPage()
        {
            InitializeComponent();
            LoadHistory();

            TranslationController.TranslationLogged += async () => await LoadHistory();
        }

        private async Task LoadHistory()
        {
            List<TranslationHistoryEntry> history = await SQLiteHistoryLogger.LoadHistory();

            await Dispatcher.InvokeAsync(() =>
            {
                HistoryDataGrid.ItemsSource = history;
            });
        }
    }
}