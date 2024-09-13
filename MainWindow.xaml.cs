using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LiveCaptionsTranslator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += (sender, args) =>
            {
                Wpf.Ui.Appearance.SystemThemeWatcher.Watch(
                    this,                                    // Window class
                    Wpf.Ui.Controls.WindowBackdropType.Mica, // Background type
                    true                                     // Whether to change accents automatically
                );
            };
        }

        void CloseButton_Click()
        {

        }
    }
}