using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

using LiveCaptionsTranslator.apis;
using LiveCaptionsTranslator.models;
using LiveCaptionsTranslator.utils;

namespace LiveCaptionsTranslator
{
    /// <summary>
    /// 「文本」页：把手输/粘贴的文本交给「设置」里当前选中的翻译引擎。
    /// 与字幕链路完全隔离：
    ///   · 走 120 秒超时的独立管道（TranslateAPI.TranslatePatiently），字幕仍然是原来的 8 秒；
    ///   · 超长文本按句切分，逐段翻译、逐段显示，最后合并成**一条**写入历史记录；
    ///   · 不写入字幕的上下文队列（Caption.Contexts），不影响字幕的上下文感知。
    /// </summary>
    public partial class TextPage : Page
    {
        /// <summary>本页显示的窗口高度（字幕页仍然是原来的 170，靠 CaptionPage.AutoHeight 恢复）。</summary>
        private const int TEXT_PAGE_WINDOW_HEIGHT = 520;

        /// <summary>单个片段的 UTF-8 字节上限。控制在 360 字节（约 120 个汉字）以内，
        /// 这样一段译文基本不会撞上云端 LLM 的 max_tokens 上限。</summary>
        private const int MAX_SEGMENT_BYTES = 360;

        /// <summary>超过这个时间还没出第一段译文，就给出「为什么在等」的解释。</summary>
        private const double STAGE2_DELAY_SECONDS = 1.2;

        /// <summary>MainWindow 的自绘标题栏高度。</summary>
        private const double TITLE_BAR_HEIGHT = 27;

        private static readonly SolidColorBrush ErrorBrush = CreateFrozen(0xFF, 0xB4, 0xAB);
        private static readonly HttpClient probeClient = new() { Timeout = TimeSpan.FromSeconds(1.5) };

        private readonly List<string> finishedSegments = new();
        private readonly DispatcherTimer stageTimer = new();

        private CancellationTokenSource? cts;
        private Task<Stage2Notice>? coldProbe;
        private bool translating;

        /// <summary>输入框内部那个"只滚不画条"的滚动器，以及我们自己的滚动条。</summary>
        private ScrollViewer? inputScroller;
        private Window? hostWindow;
        private bool inputBarSyncing;

        public TextPage()
        {
            InitializeComponent();

            stageTimer.Interval = TimeSpan.FromSeconds(STAGE2_DELAY_SECONDS);
            stageTimer.Tick += StageTimer_Tick;

            Loaded += TextPage_Loaded;
            Unloaded += TextPage_Unloaded;

            UpdateInputHint();
            UpdateEngineLabel();
        }

        // ───────────────────────── 页面生命周期 ─────────────────────────

        private void TextPage_Loaded(object sender, RoutedEventArgs e)
        {
            // 文本页需要比字幕页高得多；用户自己拉得更大就不动它。
            if (App.Current.MainWindow is MainWindow mainWindow)
            {
                if (mainWindow.Height < TEXT_PAGE_WINDOW_HEIGHT)
                    mainWindow.Height = TEXT_PAGE_WINDOW_HEIGHT;
            }

            UpdateEngineLabel();
            HookInputScrollBar();
            HookWindowWheel();
        }

        private void TextPage_Unloaded(object sender, RoutedEventArgs e)
        {
            UnhookWindowWheel();

            // 摘掉输入框滚动条的监听，避免页面被缓存后重复挂
            if (inputScroller != null)
            {
                inputScroller.ScrollChanged -= InputScroller_ScrollChanged;
                inputScroller = null;
            }

            // 离开本页时把窗户还给字幕页（它自己知道该多高：看字幕卡片开关）。
            try
            {
                if (App.Current.MainWindow is MainWindow mainWindow)
                {
                    mainWindow.IsAutoHeight = true;
                    CaptionPage.Instance?.AutoHeight();
                }
            }
            catch
            {
                // 关窗退出时可能拿不到实例，忽略即可
            }
        }

        /// <summary>
        /// 译文区自己滚：鼠标停在译文框上就直接滚它，不用先点一下。
        /// （不接管的话，鼠标停在文字之间的空隙上时，滚轮事件根本送不到这里。）
        /// 没得滚时放行，让外层页面去滚。
        /// </summary>
        private void OutputScroll_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (OutputScroll.ScrollableHeight <= 0.5)
                return;

            OutputScroll.ScrollToVerticalOffset(OutputScroll.VerticalOffset - e.Delta);
            e.Handled = true;
        }

        // ───────── 译文区滚轮：在窗口层接管（隧道事件从这里开始，下层截不走） ─────────

        private void HookWindowWheel()
        {
            if (hostWindow != null)
                return;

            hostWindow = Window.GetWindow(this);
            if (hostWindow == null)
                return;

            hostWindow.PreviewMouseWheel += OnHostWindowPreviewMouseWheel;
            hostWindow.SizeChanged += OnHostWindowSizeChanged;
            UpdateBoundedHeight();
        }

        private void UnhookWindowWheel()
        {
            if (hostWindow == null)
                return;

            hostWindow.PreviewMouseWheel -= OnHostWindowPreviewMouseWheel;
            hostWindow.SizeChanged -= OnHostWindowSizeChanged;
            hostWindow = null;
        }

        /// <summary>
        /// 鼠标停在译文区就滚译文区。挂在窗口上是因为：PreviewMouseWheel 从窗口开始往下传，
        /// 挂在这里任何下层控件都截不走，不用去猜是谁吃了事件。
        /// 鼠标不在译文区就一概不管，输入框等控件照常。
        /// </summary>
        private void OnHostWindowSizeChanged(object sender, SizeChangedEventArgs e) => UpdateBoundedHeight();

        /// <summary>
        /// 关键修复：NavigationView 不会限制页面高度，布局里那个 `*` 行会随译文长度无限长高，
        /// 于是译文区永远「内容 = 视口」，滚轮怎么推都没用。
        /// 这里按窗口高度给页面和输入框算出上限，让译文区真正拿到一块有限的高度。
        /// </summary>
        private void UpdateBoundedHeight()
        {
            if (hostWindow == null)
                return;

            double available = hostWindow.ActualHeight - TITLE_BAR_HEIGHT - 18;
            if (available < 220)
                available = 220;

            RootGrid.MaxHeight = available;

            // 除输入框外的行(两个标签行 + 按钮行)大约占 91px，译文区至少保留 110px
            double inputMax = available - 211;
            if (inputMax < 106) inputMax = 106;
            if (inputMax > 240) inputMax = 240;
            InputBox.MaxHeight = inputMax;
        }

        private void OnHostWindowPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!OutputCard.IsMouseOver)
                return;

            double scrollable = OutputScroll.ScrollableHeight;
            if (scrollable <= 0.5)
                return;

            OutputScroll.ScrollToVerticalOffset(OutputScroll.VerticalOffset - e.Delta);
            e.Handled = true;
        }

        // ───────── 输入框自带滚动条看不见 → 自己贴一个跟它同步 ─────────

        /// <summary>
        /// ui:TextBox 内部的滚动器是 PassiveScrollViewer —— 它只负责滚动、故意不画滚动条，
        /// 所以 VerticalScrollBarVisibility 设成什么都看不见（Auto / Visible 都实测过）。
        /// 做法：找到那个内部滚动器，在旁边贴一个我们自己的滚动条跟它双向同步。
        /// 输入框自己照旧管滚动和光标，原有行为一点不变。
        /// </summary>
        private void HookInputScrollBar()
        {
            if (inputScroller != null)
                return;

            inputScroller = FindDescendant<ScrollViewer>(InputBox);
            if (inputScroller == null)
                return;

            inputScroller.ScrollChanged += InputScroller_ScrollChanged;
            SyncInputScrollBar();
        }

        private void InputScroller_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            SyncInputScrollBar();
        }

        private void SyncInputScrollBar()
        {
            if (inputScroller == null)
                return;

            double scrollable = inputScroller.ScrollableHeight;
            if (scrollable <= 0.5)
            {
                InputScrollBar.Visibility = Visibility.Collapsed;
                return;
            }

            InputScrollBar.Visibility = Visibility.Visible;
            InputScrollBar.Minimum = 0;
            InputScrollBar.Maximum = scrollable;
            InputScrollBar.ViewportSize = inputScroller.ViewportHeight;
            InputScrollBar.LargeChange = Math.Max(1, inputScroller.ViewportHeight);
            InputScrollBar.SmallChange = 16;

            inputBarSyncing = true;
            InputScrollBar.Value = inputScroller.VerticalOffset;
            inputBarSyncing = false;
        }

        private void InputScrollBar_Scroll(object sender, ScrollEventArgs e)
        {
            if (inputBarSyncing || inputScroller == null)
                return;

            inputScroller.ScrollToVerticalOffset(InputScrollBar.Value);
        }

        private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T hit)
                    return hit;

                var deeper = FindDescendant<T>(child);
                if (deeper != null)
                    return deeper;
            }

            return null;
        }

        // ───────────────────────── 输入区 ─────────────────────────

        private void InputBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateInputHint();
            // 高度要等布局跑完才准，下一帧再算一次上限提示
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(UpdateInputHint));
        }

        private void UpdateInputHint()
        {
            string text = InputBox.Text ?? string.Empty;
            InputPlaceholder.Visibility = text.Length == 0 ? Visibility.Visible : Visibility.Collapsed;

            bool atCap = InputBox.ActualHeight >= InputBox.MaxHeight - 1;
            CounterLabel.Text = atCap
                ? $"{text.Length} 字 · 输入框已到上限，框内滚动"
                : $"{text.Length} 字";

            HookInputScrollBar();
        }

        private void InputBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                e.Handled = true;
                _ = RunTranslationAsync();
            }
        }

        private void UpdateEngineLabel()
        {
            var setting = Translator.Setting;
            if (setting == null)
            {
                EngineLabel.Text = string.Empty;
                return;
            }

            string apiName = setting.ApiName ?? string.Empty;
            string modelName = (setting[apiName] as BaseLLMConfig)?.ModelName ?? string.Empty;

            EngineLabel.Text = modelName.Length > 0
                ? $"目标语言：{setting.TargetLanguage} · {apiName} · {modelName}"
                : $"目标语言：{setting.TargetLanguage} · {apiName}";
        }

        // ───────────────────────── 按钮 ─────────────────────────

        private async void TranslateButton_Click(object sender, RoutedEventArgs e)
        {
            await RunTranslationAsync();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            cts?.Cancel();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            if (translating)
                return;

            InputBox.Text = string.Empty;
            finishedSegments.Clear();
            OutputText.Text = string.Empty;
            OutputText.Visibility = Visibility.Collapsed;
            LatencyLabel.Text = string.Empty;
            SetStatus("译文会显示在这里…", StatusKind.Dim);
            UpdateInputHint();
            InputBox.Focus();
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            string text = OutputText.Text ?? string.Empty;
            if (text.Length == 0)
            {
                SnackbarHost.Show("还没有可复制的译文", string.Empty, SnackbarType.Warning, 100);
                return;
            }

            try
            {
                Clipboard.SetText(text);
                SnackbarHost.Show("已复制译文", string.Empty, SnackbarType.Success, 100);
            }
            catch
            {
                SnackbarHost.Show("复制失败", "请手动选中译文后再复制。", SnackbarType.Error, 100);
            }
        }

        // ───────────────────────── 主流程 ─────────────────────────

        private async Task RunTranslationAsync()
        {
            if (translating)
                return;

            string source = (InputBox.Text ?? string.Empty).Trim();
            if (source.Length == 0)
            {
                SetStatus("请先输入要翻译的文本。", StatusKind.Error);
                InputBox.Focus();
                return;
            }

            var segments = BuildSegments(source, MAX_SEGMENT_BYTES);
            if (segments.Count == 0)
            {
                SetStatus("没能从这段文字里找到可翻译的内容。", StatusKind.Error);
                return;
            }

            translating = true;
            finishedSegments.Clear();
            cts?.Dispose();
            cts = new CancellationTokenSource();
            var token = cts.Token;

            OutputText.Text = string.Empty;
            OutputText.Visibility = Visibility.Collapsed;
            LatencyLabel.Text = string.Empty;
            SetStatus("……翻译中……", StatusKind.Dim);
            SetBusy(true);

            // 冷启动探测和「阶段二提示」并行跑，不阻塞第一段译文
            coldProbe = ProbeStage2NoticeAsync(token);
            stageTimer.Start();

            var totalWatch = Stopwatch.StartNew();
            Stopwatch? generateWatch = null;
            int done = 0;

            try
            {
                while (done < segments.Count)
                {
                    token.ThrowIfCancellationRequested();
                    generateWatch ??= Stopwatch.StartNew();

                    string result;
                    try
                    {
                        result = await TranslateAPI.TranslatePatiently(segments[done], token);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        result = $"[ERROR] {ex.Message}";
                    }

                    result = NormalizeResult(result);
                    if (IsFailure(result))
                    {
                        // 引擎报错：保留已经翻好的部分，红字提示，这次不写历史
                        stageTimer.Stop();
                        RenderFinished();
                        SetStatus(result, StatusKind.Error);
                        LatencyLabel.Text = $"已翻好 {done}/{segments.Count} 段，第 {done + 1} 段出错";
                        return;
                    }

                    finishedSegments.Add(result);
                    done++;
                    stageTimer.Stop();
                    RenderFinished();

                    if (done < segments.Count)
                    {
                        // 冷启动那段时间不计入平均，否则预估会离谱
                        double average = generateWatch.Elapsed.TotalSeconds / done;
                        int remain = Math.Max(0, (int)Math.Round(average * (segments.Count - done)));
                        SetStatus($"……翻译中…… 第 {done}/{segments.Count} 段 · 预计还需约 {remain} 秒", StatusKind.Dim);
                    }
                }

                SetStatus(string.Empty, StatusKind.Dim);
                LatencyLabel.Text = $"完成 · {segments.Count} 段 · 用时 {totalWatch.Elapsed.TotalSeconds:F1} 秒";

                // 整篇合并成一条写进历史（分段不散落成几十条）
                await Translator.Log(source, string.Concat(finishedSegments), false, token);
            }
            catch (OperationCanceledException)
            {
                RenderFinished();
                SetStatus($"已取消 · 已完成 {finishedSegments.Count}/{segments.Count} 段",
                    StatusKind.Warn, "已翻好的部分保留在上面；这次不会写进历史记录。");
                LatencyLabel.Text = "已取消";
            }
            finally
            {
                stageTimer.Stop();
                translating = false;
                SetBusy(false);
            }
        }

        private void SetBusy(bool busy)
        {
            TranslateButton.Visibility = busy ? Visibility.Collapsed : Visibility.Visible;
            CancelButton.Visibility = busy ? Visibility.Visible : Visibility.Collapsed;
            ClearButton.IsEnabled = !busy;
            InputBox.IsReadOnly = busy;
        }

        private void RenderFinished()
        {
            if (finishedSegments.Count == 0)
            {
                OutputText.Text = string.Empty;
                OutputText.Visibility = Visibility.Collapsed;
                return;
            }

            OutputText.Text = string.Concat(finishedSegments);
            OutputText.Visibility = Visibility.Visible;
        }

        private enum StatusKind
        {
            Dim,
            Warn,
            Error
        }

        private void SetStatus(string main, StatusKind kind = StatusKind.Dim, string? sub = null)
        {
            StatusLine.Text = main ?? string.Empty;
            StatusSub.Text = sub ?? string.Empty;
            StatusSub.Visibility = string.IsNullOrEmpty(sub) ? Visibility.Collapsed : Visibility.Visible;

            if (kind == StatusKind.Error)
            {
                // 失败必须是实色红字，能一眼看见（引擎的错误是当正常文字返回的，不是弹窗）
                StatusLine.Foreground = ErrorBrush;
                StatusLine.Opacity = 1.0;
                StatusSub.Opacity = 0.9;
            }
            else
            {
                // XAML 里没设 Foreground，ClearValue 会退回主题默认色，浅色/深色都跟着走
                StatusLine.ClearValue(TextBlock.ForegroundProperty);
                StatusLine.Opacity = kind == StatusKind.Warn ? 0.85 : 0.55;
                StatusSub.Opacity = 0.6;
            }
        }

        // ───────────────────────── 阶段二提示（为什么在等） ─────────────────────────

        private readonly record struct Stage2Notice(string Main, string? Sub);

        private static readonly Stage2Notice GenericNotice =
            new("正在等待翻译引擎响应……", null);

        private async void StageTimer_Tick(object? sender, EventArgs e)
        {
            stageTimer.Stop();
            if (!translating || finishedSegments.Count > 0)
                return;

            Stage2Notice notice = GenericNotice;
            if (coldProbe != null)
            {
                try
                {
                    notice = await coldProbe;
                }
                catch
                {
                    notice = GenericNotice;
                }
            }

            // await 期间可能已经出译文了，再确认一次别把真实进度覆盖掉
            if (!translating || finishedSegments.Count > 0)
                return;

            SetStatus(notice.Main, StatusKind.Dim, notice.Sub);
        }

        /// <summary>
        /// 决定「等这么久到底是在等什么」。原则：**只说能证实的话**。
        /// 只有确认模型不在显存里时才说「正在加载本地模型」，否则一律用通用文案。
        /// </summary>
        private async Task<Stage2Notice> ProbeStage2NoticeAsync(CancellationToken token)
        {
            string apiName = Translator.Setting?.ApiName ?? string.Empty;

            if (!IsLocalEngine(apiName))
            {
                return new Stage2Notice("正在等待翻译引擎响应……",
                    "云端模型翻译长文通常需要几秒到几十秒，请稍候。");
            }

            if (apiName == "Ollama")
            {
                try
                {
                    string modelName = (Translator.Setting?["Ollama"] as OllamaConfig)?.ModelName ?? string.Empty;
                    if (modelName.Length > 0 && !await IsOllamaModelLoadedAsync(modelName, token))
                    {
                        return new Stage2Notice("正在加载本地模型……",
                            "模型当前不在显存里，需要先读进去，一般 5～10 秒。"
                            + "读进去之后，10 分钟内再翻译都是秒回。");
                    }
                }
                catch
                {
                    // 探测失败就退回通用文案，绝不瞎说
                }
            }

            return new Stage2Notice("正在等待翻译引擎响应……", "本地模型正在处理这段文字，请稍候。");
        }

        private static bool IsLocalEngine(string apiName) =>
            apiName is "Ollama" or "LMStudio" or "MTranServer" or "LibreTranslate";

        /// <summary>问 Ollama：这个模型现在在不在显存里？查不到就返回 true（当作"已加载"，不乱报需要加载）。</summary>
        private static async Task<bool> IsOllamaModelLoadedAsync(string modelName, CancellationToken token)
        {
            var config = Translator.Setting?["Ollama"] as OllamaConfig;
            string baseUrl = config?.ApiUrl ?? "http://localhost:11434";
            string url = TextUtil.NormalizeUrl(baseUrl) + "/api/ps";

            using var response = await probeClient.GetAsync(url, token);
            if (!response.IsSuccessStatusCode)
                return true;

            string body = await response.Content.ReadAsStringAsync(token);
            using var document = JsonDocument.Parse(body);

            if (!document.RootElement.TryGetProperty("models", out var models) ||
                models.ValueKind != JsonValueKind.Array)
                return true;

            string wantedBase = modelName.Split(':')[0];
            foreach (var item in models.EnumerateArray())
            {
                if (!item.TryGetProperty("name", out var nameProperty))
                    continue;

                string loaded = nameProperty.GetString() ?? string.Empty;
                if (loaded.Length == 0)
                    continue;

                if (string.Equals(loaded, modelName, StringComparison.OrdinalIgnoreCase))
                    return true;
                if (string.Equals(loaded.Split(':')[0], wantedBase, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        // ───────────────────────── 文本切分 ─────────────────────────

        /// <summary>先按句末标点切句，再把相邻短句贪心合并到字节预算内；单句超预算的按逗号/空格再切。</summary>
        internal static List<string> BuildSegments(string text, int maxBytes)
        {
            var segments = new List<string>();
            var current = new StringBuilder();

            foreach (string sentence in SplitBySentence(text))
            {
                foreach (string piece in SplitByBudget(sentence, maxBytes))
                {
                    if (current.Length > 0 &&
                        Encoding.UTF8.GetByteCount(current.ToString()) + Encoding.UTF8.GetByteCount(piece) > maxBytes)
                    {
                        segments.Add(current.ToString());
                        current.Clear();
                    }

                    // 切句时把英文句子之间的空格丢掉了，这里补回来（中文不加空格）
                    if (current.Length > 0 &&
                        !char.IsWhiteSpace(current[current.Length - 1]) &&
                        !TextUtil.isCJChar(current[current.Length - 1]))
                    {
                        current.Append(' ');
                    }

                    current.Append(piece);
                }
            }

            if (current.Length > 0)
                segments.Add(current.ToString());

            return segments;
        }

        private static IEnumerable<string> SplitBySentence(string text)
        {
            var buffer = new StringBuilder();
            foreach (char c in text)
            {
                buffer.Append(c);
                if (Array.IndexOf(TextUtil.PUNC_EOS, c) < 0)
                    continue;

                string sentence = buffer.ToString().Trim();
                if (sentence.Length > 0)
                    yield return sentence;
                buffer.Clear();
            }

            string tail = buffer.ToString().Trim();
            if (tail.Length > 0)
                yield return tail;
        }

        private static IEnumerable<string> SplitByBudget(string text, int maxBytes)
        {
            int start = 0;
            while (start < text.Length)
            {
                int length = 1;
                while (start + length <= text.Length &&
                       Encoding.UTF8.GetByteCount(text.AsSpan(start, length)) <= maxBytes)
                {
                    length++;
                }

                length--;
                if (length < 1)
                    length = 1;

                if (start + length >= text.Length)
                {
                    string rest = text.Substring(start).Trim();
                    if (rest.Length > 0)
                        yield return rest;
                    yield break;
                }

                // 优先在逗号、顿号、空格处断开，读起来自然些
                int cut = -1;
                for (int i = start + length - 1; i > start; i--)
                {
                    if (Array.IndexOf(TextUtil.PUNC_COMMA, text[i]) >= 0 || text[i] == ' ')
                    {
                        cut = i;
                        break;
                    }
                }

                int end = cut > start ? cut + 1 : start + length;
                string piece = text.Substring(start, end - start).Trim();
                if (piece.Length > 0)
                    yield return piece;
                start = end;
            }
        }

        // ───────────────────────── 小工具 ─────────────────────────

        private static string NormalizeResult(string text) =>
            (text ?? string.Empty).Replace("🔤", string.Empty).Trim();

        private static bool IsFailure(string text) =>
            text.Contains("[ERROR]") || text.Contains("[WARNING]");

        private static SolidColorBrush CreateFrozen(byte r, byte g, byte b)
        {
            var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
            brush.Freeze();
            return brush;
        }
    }
}
