using System.Runtime.ExceptionServices;

namespace CustomTask
{
    public class CustomTask
    {
        private readonly Lock _lock = new();

        private bool _isCompleted;
        private Exception? _exception;
        private Action? _action;
        private ExecutionContext? _context;

        public bool IsCompleted
        {
            get
            {
                lock (_lock)
                {
                    return _isCompleted;
                }
            }
        }

        public static CustomTask Delay(TimeSpan delay)
        {
            CustomTask task = new();

            new Timer(_ => task.SetResult()).Change(delay, Timeout.InfiniteTimeSpan);

            return task;
        }

        public static CustomTask Run(Action action)
        {
            CustomTask task = new();

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    action();
                    task.SetResult();
                }
                catch (Exception ex)
                {
                    task.SetException(ex);
                }
            });

            return task;
        }

        public void Wait()
        {
            ManualResetEventSlim? resetEventSlim = null;

            lock (_lock)
            {
                if (!_isCompleted)
                {
                    resetEventSlim = new ManualResetEventSlim();
                    ContinueWith(() => resetEventSlim.Set());
                }
            }

            resetEventSlim?.Wait();

            if (_exception is not null)
            {
                ExceptionDispatchInfo.Throw(_exception);
            }
        }

        public CustomTask ContinueWith(Action action)
        {
            CustomTask task = new();

            lock (_lock)
            {
                if (_isCompleted)
                {
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        try
                        {
                            action();
                            task.SetResult();
                        }
                        catch (Exception ex)
                        {
                            task.SetException(ex);
                        }
                    });
                }
                else
                {
                    _action = action;
                    _context = ExecutionContext.Capture();
                }
            }

            return task;
        }

        public void SetResult() => CompleteTask(null);

        public void SetException(Exception exception) => CompleteTask(exception);

        private void CompleteTask(Exception? exception)
        {
            lock (_lock)
            {
                if (_isCompleted)
                {
                    throw new InvalidOperationException("CustomTask is already completed." +
                        "Cannot set result of a completed CustomTask.");
                }

                _isCompleted = true;
                _exception = exception;

                if (_action is not null)
                {
                    if (_context is null)
                    {
                        _action.Invoke();
                    }
                    else
                    {
                        ExecutionContext.Run(_context, state => ((Action?)state)?.Invoke(), _action);
                    }
                }
            }
        }
    }
}
