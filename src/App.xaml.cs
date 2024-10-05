using System.Windows;
using System.Windows.Automation;
using LiveCaptionsTranslator.models;

namespace LiveCaptionsTranslator
{
    public partial class App : Application
    {
        public static AutomationElement Window { get; } = LiveCaptionsHandler.LaunchLiveCaptions();

        private static Caption? captions = null;
        private static Setting? settings = null;

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

            captions = Caption.GetInstance();
            settings = Setting.Load();

            Task.Run(() => Captions?.Sync(Window));
            Task.Run(() => Captions?.Translate());
        }

        static void OnProcessExit(object sender, EventArgs e)
        {
            LiveCaptionsHandler.KillLiveCaptions();
            Settings?.Save();
        }
    }
}
