using System.Windows.Controls;

namespace LiveCaptionsTranslator
{
    public partial class CaptionPage : Page
    {
        public CaptionPage()
        {
            InitializeComponent();
            DataContext = App.Captions;
        }
    }
}
