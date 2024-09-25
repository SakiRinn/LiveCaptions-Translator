using System.Windows.Controls;

namespace LiveCaptionsTranslator
{
    public partial class CaptionPage : Page
    {
        public CaptionPage()
        {
            InitializeComponent();

            var caption = new Caption();
            this.DataContext = caption;
            Task.Run(() => caption.Translate(App.Window));
        }
    }
}
