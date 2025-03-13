using System.Runtime.CompilerServices;

namespace CustomTask;

internal readonly struct CustomTaskAwaiter : INotifyCompletion
{
    private readonly CustomTask _task;

    internal CustomTaskAwaiter(CustomTask task) => _task = task;

    public bool IsCompleted => _task.IsCompleted;

    public void GetResult() => _task.Wait();

    public void OnCompleted(Action continuation) => _task.ContinueWith(continuation);
}
