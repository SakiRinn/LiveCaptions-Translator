using System.Diagnostics;
using System.IO;
using System.Text;

namespace LiveCaptionsTranslator.utils
{
    public static class SteamVROverlayService
    {
        private static readonly object _lock = new();
        private static Process? _process;
        private static readonly StringBuilder _output = new();

        public static event Action<bool, string?>? StateChanged;

        public static bool IsRunning
        {
            get
            {
                lock (_lock)
                {
                    return _process != null && !_process.HasExited;
                }
            }
        }

        public static bool TryStart(out string error)
        {
            if (IsRunning)
            {
                error = string.Empty;
                return true;
            }

            string overlayDir = Path.Combine(AppContext.BaseDirectory, "SteamVR");
            string exePath = Path.Combine(overlayDir, "SteamVR-TransView.exe");
            string scriptPath = Path.Combine(overlayDir, "steampy_overlay.py");

            string dbPath = Path.GetFullPath(Path.Combine(
                Directory.GetCurrentDirectory(), SQLiteHistoryLogger.DB_FILENAME));
            if (!File.Exists(dbPath))
            {
                error = $"未找到翻译数据库：{dbPath}";
                return false;
            }

            if (File.Exists(exePath))
                return TryStartProcess(exePath, string.Empty, dbPath, overlayDir, out error);

            if (!File.Exists(scriptPath))
            {
                error = $"未找到 SteamVR 面板：{exePath}";
                return false;
            }

            foreach (string launcher in new[] { "python", "py" })
            {
                if (TryStartProcess(launcher, scriptPath, dbPath, overlayDir, out error))
                    return true;
            }

            error = "无法启动 SteamVR 面板，也未找到可用的 Python。";
            return false;
        }

        public static void Stop()
        {
            Process? process;
            lock (_lock)
            {
                process = _process;
                _process = null;
            }

            if (process == null)
                return;

            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                    process.WaitForExit(2000);
                }
            }
            catch
            {
            }
            finally
            {
                process.Dispose();
            }
        }

        private static bool TryStartProcess(string launcher, string scriptPath, string dbPath,
            string workingDirectory, out string error)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = launcher,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                },
                EnableRaisingEvents = true
            };

            if (!string.IsNullOrEmpty(scriptPath))
                process.StartInfo.ArgumentList.Add(scriptPath);
            process.StartInfo.ArgumentList.Add("--db-path");
            process.StartInfo.ArgumentList.Add(dbPath);

            lock (_lock)
            {
                _output.Clear();
            }

            process.OutputDataReceived += (_, e) => AppendOutput(e.Data);
            process.ErrorDataReceived += (_, e) => AppendOutput(e.Data);
            process.Exited += (_, _) => OnProcessExited(process);

            try
            {
                if (!process.Start())
                {
                    error = $"{launcher} 启动失败。";
                    process.Dispose();
                    return false;
                }

                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                lock (_lock)
                {
                    _process = process;
                }
                StateChanged?.Invoke(true, null);

                error = string.Empty;
                return true;
            }
            catch (Exception ex)
            {
                error = $"{launcher}: {ex.Message}";
                process.Dispose();
                return false;
            }
        }

        private static void OnProcessExited(Process process)
        {
            lock (_lock)
            {
                if (!ReferenceEquals(_process, process))
                    return;
                _process = null;
            }

            string detail = GetOutput();
            StateChanged?.Invoke(false,
                string.IsNullOrWhiteSpace(detail) ? "SteamVR 面板已退出。" : detail);
        }

        private static void AppendOutput(string? line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return;

            lock (_lock)
            {
                if (_output.Length > 4000)
                    _output.Clear();
                _output.AppendLine(line);
            }
        }

        private static string GetOutput()
        {
            lock (_lock)
            {
                string output = _output.ToString().Trim();
                return output.Length <= 800 ? output : output[^800..];
            }
        }
    }
}
