using System;
using System.Collections.Concurrent;

namespace Furball.Vixie.Helpers; 

public static class DisposeQueue {
    private static ConcurrentQueue<IDisposable> _disposeQueue = new();

    public static void Enqueue(IDisposable disposable) => _disposeQueue.Enqueue(disposable);

    public static void DoDispose() {
        if (_disposeQueue.TryDequeue(out IDisposable disposable))
            disposable.Dispose();
    }

    public static void DoDispose(int elements) {
        for (int i = 0; i != elements; i++) {
            if (_disposeQueue.TryDequeue(out IDisposable disposable))
                disposable.Dispose();
            else
                return;
        }
    }

    public static void DisposeAll() {
        while(_disposeQueue.TryDequeue(out IDisposable disposable))
            disposable.Dispose();
    }
}