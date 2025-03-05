namespace LiveCaptionsTranslator.models
{
    public class TranslationTaskQueue
    {
        private readonly object _lock = new object();

        private readonly List<TranslationTask> tasks;
        private string output;

        public string Output
        {
            get => output;
        }

        public TranslationTaskQueue()
        {
            tasks = new List<TranslationTask>();
            output = string.Empty;
        }

        public void Enqueue(Func<CancellationToken, Task<string>> worker)
        {
            var newTranslationTask = new TranslationTask(worker, new CancellationTokenSource());
            lock (_lock)
            {
                tasks.Add(newTranslationTask);
            }
            newTranslationTask.Task.ContinueWith(
                task => OnTaskCompleted(newTranslationTask),
                TaskContinuationOptions.OnlyOnRanToCompletion
            );
        }

        private void OnTaskCompleted(TranslationTask translationTask)
        {
            lock (_lock)
            {
                var index = tasks.IndexOf(translationTask);
                for (int i = 0; i < index; i++)
                    tasks[i].CTS.Cancel();
                for (int i = index; i >= 0; i--)
                    tasks.RemoveAt(i);
                output = translationTask.Task.Result;
            }
        }
    }

    public class TranslationTask
    {
        public Task<string> Task { get; }
        public CancellationTokenSource CTS { get; }

        public TranslationTask(Func<CancellationToken, Task<string>> worker, CancellationTokenSource cts)
        {
            Task = worker(cts.Token);
            CTS = cts;
        }
    }
}