using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Controls;
using LiveCaptionsTranslator.controllers;
using LiveCaptionsTranslator.models;
using Wpf.Ui.Appearance;

namespace LiveCaptionsTranslator
{
    public partial class HistoryPage : Page
    {
        public HistoryPage()
        {
            InitializeComponent();
            ApplicationThemeManager.ApplySystemTheme();
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await LoadHistoryAsync();
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