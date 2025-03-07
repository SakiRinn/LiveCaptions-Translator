using System.Windows.Controls;

namespace LiveCaptionsTranslator
{
    public partial class PromptSetting : Page
    {
        public PromptSetting()
        {
            InitializeComponent();
            DataContext = App.Setting;
        }

        private void PromptTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            App.Setting?.Save();
        }
    }
}