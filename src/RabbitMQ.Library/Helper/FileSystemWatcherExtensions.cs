using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RabbitMQ.Library.Helper;

public static class FileSystemWatcherExtensions
{
    public static Task<FileSystemEventArgs> WaitForChangedAsync(this FileSystemWatcher watcher, CancellationToken token = default)
    {
        var promise = new TaskCompletionSource<FileSystemEventArgs>();
            
        // Using local function, since it have to know itself in order to
        // remove the event-handler once the promise is completed.
        void Handler(object sender, FileSystemEventArgs args)
        {
            if (token.IsCancellationRequested)
            {
                return;
            }
                
            watcher.Changed -= Handler;
            promise.TrySetResult(args);
        }

        watcher.EnableRaisingEvents = true;
        watcher.Changed += Handler;
        // When the token gets cancelled, remove the handler and set a result.
        token.Register(() =>
        {
            watcher.Changed -= Handler;
            promise.TrySetResult(null);
        });

        return promise.Task;
    }

    public static Task DisposeAsync(this FileSystemWatcher watcher, CancellationToken token = default)
    {
        var promise = new TaskCompletionSource();

        void Handler(object sender, EventArgs args)
        {
            promise.TrySetResult();
        }

        watcher.EnableRaisingEvents = true;
        watcher.Disposed += Handler;

        watcher.Dispose();

        token.Register(() =>
        {
            promise.TrySetResult();
        });

        return promise.Task;
    }
}