using System.Windows;
using System.Windows.Automation;

using LiveCaptionsTranslator.models;
using LiveCaptionsTranslator.utils;

namespace LiveCaptionsTranslator
{
    public partial class App : Application
    {
        private static AutomationElement? window = null;
        private static Caption? caption = null;
        private static Setting? setting = null;

        public static AutomationElement? Window
        {
            get => window;
            set => window = value;
        }
        public static Caption? Caption
        {
            get => caption;
        }
        public static Setting? Setting
        {
            get => setting;
        }

        App()
        {
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

            window = LiveCaptionsHandler.LaunchLiveCaptions();
            LiveCaptionsHandler.FixLiveCaptions(window);
            LiveCaptionsHandler.HideLiveCaptions(window);

            caption = Caption.GetInstance();
            setting = Setting.Load();

            Task.Run(() => App.Caption?.Sync());
            Task.Run(() => App.Caption?.Translate());
        }

        static void OnProcessExit(object sender, EventArgs e)
        {
            if (window != null)
            {
                LiveCaptionsHandler.RestoreLiveCaptions(window);
                LiveCaptionsHandler.KillLiveCaptions(window);
            }
        }
    }
}
