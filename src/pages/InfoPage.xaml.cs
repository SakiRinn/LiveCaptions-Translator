// zh-CN fork: 本文件由 Sky-lll27 修改（2026-10），加入「本汉化版」署名区块并删除版本号显示，
// 相应调整窗口自适应高度。Based on SakiRinn/LiveCaptions-Translator (Apache-2.0). Modified per §4(b).

using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;
using Wpf.Ui.Appearance;

namespace LiveCaptionsTranslator
{
    public partial class InfoPage : Page
    {
        // 210 → 330：新增「本汉化版」署名区块（4 行）与「原项目」一行，并删除版本号一行。
        public const int MIN_HEIGHT = 330;

        public InfoPage()
        {
            InitializeComponent();
            ApplicationThemeManager.ApplySystemTheme();

            Loaded += (s, e) =>
            {
                (App.Current.MainWindow as MainWindow)?.AutoHeightAdjust(minHeight: MIN_HEIGHT, maxHeight: MIN_HEIGHT);
            };
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}