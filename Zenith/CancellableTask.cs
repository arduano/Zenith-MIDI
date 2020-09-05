using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace Zenith
{
    public class CancellableTask
    {
        public static CancellableTask Run(Action<CancellationToken> start)
        {
            var source = new CancellationTokenSource();
            return new CancellableTask(Task.Run(() => start(source.Token)), source);
        }

        private CancellableTask(Task task, CancellationTokenSource token)
        {
            Task = task;
            TokenSource = token;
        }

        public Task Task { get; }
        public CancellationTokenSource TokenSource { get; }
        public CancellationToken Token => TokenSource.Token;
        public bool IsCancelling => Token.IsCancellationRequested;

        public void Cancel()
        {
            if (IsCancelling) return;
            TokenSource.Cancel();
        }

        public void Wait()
        {
            Task.Wait();
        }
    }
}
