﻿using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using LiveCaptionsTranslator.models;

namespace LiveCaptionsTranslator
{
    public partial class App : Application, IDisposable
    {
        private static AutomationElement? window = null;
        private static Caption? captions = null;
        private static Setting? settings = null;
        private readonly CancellationTokenSource _appCts;
        private bool _disposed;

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
            _appCts = new CancellationTokenSource();
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;

            window = LiveCaptionsHandler.LaunchLiveCaptions();
            captions = Caption.GetInstance();
            settings = Setting.Load();

            // Initialize caption provider based on current API setting
            captions?.InitializeProvider(settings?.ApiName ?? "OpenAI");

            // Start caption sync and translation tasks
            Task.Run(async () => await RunCaptionSyncAsync(_appCts.Token));
            Task.Run(async () => await RunTranslationAsync(_appCts.Token));
        }

        private async Task RunCaptionSyncAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Captions != null)
                {
                    await Captions.SyncAsync();
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Normal shutdown, no action needed
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Caption sync error: {ex.Message}");
            }
        }

        private async Task RunTranslationAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (Captions != null)
                {
                    await Captions.TranslateAsync(cancellationToken);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // Normal shutdown, no action needed
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Translation error: {ex.Message}");
            }
        }

        private void OnProcessExit(object sender, EventArgs e)
        {
            Dispose();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                _appCts.Cancel();
                _appCts.Dispose();
                
                if (Captions != null)
                {
                    ((IDisposable)Captions).Dispose();
                }
                
                LiveCaptionsHandler.KillLiveCaptions();
            }
        }
    }
}
