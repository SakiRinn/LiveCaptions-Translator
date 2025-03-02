using System.Text.RegularExpressions;
using System.Windows;

using LiveCaptionsTranslator.models;

namespace LiveCaptionsTranslator.utils
{
    public static class WindowHandler
    {
        public static Rect SaveState(Window? window, Setting? setting)
        {
            if (window == null || setting == null)
                return Rect.Empty;
            string windowName = window.GetType().Name;
            setting.WindowBounds[windowName] = Regex.Replace(
                window.RestoreBounds.ToString(), @"(\d+\.\d{1})\d+", "$1");
            setting.Save();
            return window.RestoreBounds;
        }

        public static Rect LoadState(Window? window, Setting? setting)
        {
            if (window == null || setting == null)
                return Rect.Empty;
            string windowName = window.GetType().Name;
            Rect bound = Rect.Parse(setting.WindowBounds[windowName]);
            return bound;
        }

        public static void RestoreState(Window? window, Rect bound)
        {
            if (window == null || bound.IsEmpty)
                return;
            window.Top = bound.Top;
            window.Left = bound.Left;

            // Restore the size only for a manually sized
            if (window.SizeToContent == SizeToContent.Manual)
            {
                window.Width = bound.Width;
                window.Height = bound.Height;
            }
        }
    }
}
