namespace Fig.Agent.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A cancellation token source which triggers when the <c>Ctrl+C</c> keys are pressed.
    /// </summary>
    sealed class CancelSource : IDisposable
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        public CancelSource()
        {
            Console.CancelKeyPress += Console_CancelKeyPress;
        }

        public CancellationToken Token => this.cancellationTokenSource.Token;

        public void Dispose()
        {
            Console.CancelKeyPress -= Console_CancelKeyPress;
            this.cancellationTokenSource.Dispose();
        }

        private void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            this.cancellationTokenSource.Cancel();
        }
    }
}
