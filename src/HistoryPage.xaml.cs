using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using LiveCaptionsTranslator.controllers;
using LiveCaptionsTranslator.models;
using Wpf.Ui.Appearance;

namespace LiveCaptionsTranslator
{
    public partial class HistoryPage : Page
    {
        private List<TranslationHistoryEntry> allHistory;
        private string currentSearchText = "";
        private int currentPage = 1;
        private int totalPages = 1;
        private bool isLoading = false;

        public HistoryPage()
        {
            InitializeComponent();
            ApplicationThemeManager.ApplySystemTheme();
            DataContext = App.Settings;
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            await LoadHistoryAsync();
            TranslationController.TranslationLogged += async () => await LoadHistoryAsync();
        }

        private async Task LoadHistoryAsync()
        {
            if (isLoading) return;
            isLoading = true;
            try
            {
                totalPages = await SQLiteHistoryLogger.GetTotalPagesAsync();
                allHistory = await SQLiteHistoryLogger.LoadHistoryAsync(currentPage);
                
                FilterAndDisplayHistory();
                UpdatePaginationStatus();
            }
            finally
            {
                isLoading = false;
            }
        }

        private void FilterAndDisplayHistory()
        {
            var filteredHistory = allHistory;
            if (!string.IsNullOrWhiteSpace(currentSearchText))
            {
                filteredHistory = allHistory.Where(h =>
                    h.SourceText.Contains(currentSearchText, StringComparison.OrdinalIgnoreCase) ||
                    h.TranslatedText.Contains(currentSearchText, StringComparison.OrdinalIgnoreCase)
                ).ToList();
            }

            Dispatcher.Invoke(() =>
            {
                HistoryDataGrid.ItemsSource = filteredHistory;
            });
        }

        private async void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "确定要清除所有历史记录吗？此操作不可撤销。",
                "确认清除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result == MessageBoxResult.Yes)
            {
                await SQLiteHistoryLogger.ClearHistoryAsync();
                await LoadHistoryAsync();
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                currentSearchText = textBox.Text;
                FilterAndDisplayHistory();
            }
        }

        private void UpdatePaginationStatus()
        {
            Dispatcher.Invoke(() =>
            {
                PageInfo.Text = $"第 {currentPage} 页，共 {totalPages} 页";
                PrevButton.IsEnabled = currentPage > 1;
                NextButton.IsEnabled = currentPage < totalPages;
            });
        }

        private async void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage > 1)
            {
                currentPage--;
                await LoadHistoryAsync();
            }
        }

        private async void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (currentPage < totalPages)
            {
                currentPage++;
                await LoadHistoryAsync();
            }
        }
    }
}