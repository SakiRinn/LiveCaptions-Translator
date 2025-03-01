using System.Windows;
using System.Windows.Automation;

using LiveCaptionsTranslator.models;
using LiveCaptionsTranslator.utils;

namespace LiveCaptionsTranslator
{
    public partial class App : Application
    {
        private static AutomationElement? window = null;
        private static Caption? captions = null;
        private static Setting? settings = null;

        public static AutomationElement? Window
        {
            get => window;
            set => window = value;
        }
        public static Caption? Captions
        {
            get => captions;
        }
        public static Setting? Settings
        {
            get => settings;
        }

        App()
        {
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

            window = LiveCaptionsHandler.LaunchLiveCaptions();
            LiveCaptionsHandler.FixLiveCaptions(window);
            LiveCaptionsHandler.HideLiveCaptions(window);

            captions = Caption.GetInstance();
            settings = Setting.Load();

            Task.Run(() => Captions?.Sync());
            Task.Run(() => Captions?.Translate());
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
