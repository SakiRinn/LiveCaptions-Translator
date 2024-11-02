using System.Collections.Generic;
using System.Threading.Tasks;
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
            LoadHistoryAsync();
            
            TranslationController.TranslationLogged += async () => await LoadHistoryAsync();
        }

        private async Task LoadHistoryAsync()
        {
            List<TranslationHistoryEntry> history = await SQLiteHistoryLogger.LoadHistoryAsync();
            
            await Dispatcher.InvokeAsync(() =>
            {
                HistoryDataGrid.ItemsSource = history;
            });
        }
    }
}