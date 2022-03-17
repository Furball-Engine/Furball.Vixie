using System;
using System.Collections.Concurrent;

namespace Furball.Vixie {
    public static class DisposeQueue {
        private static ConcurrentQueue<IDisposable> _disposeQueue = new();

        public static void Enqueue(IDisposable disposable) => _disposeQueue.Enqueue(disposable);

        internal static void DoDispose() {
            if (_disposeQueue.TryDequeue(out IDisposable disposable))
                disposable.Dispose();
        }

        internal static void DoDispose(int elements) {
            for (int i = 0; i != elements; i++) {
                if (_disposeQueue.TryDequeue(out IDisposable disposable))
                    disposable.Dispose();
                else
                    return;
            }
        }
    }
}
