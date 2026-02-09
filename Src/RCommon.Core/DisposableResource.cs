using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RCommon
{
    /// <summary>
    /// Abstract base class that implements the standard Dispose pattern for both synchronous
    /// (<see cref="IDisposable"/>) and asynchronous (<see cref="IAsyncDisposable"/>) disposal.
    /// </summary>
    /// <remarks>
    /// Derived classes should override <see cref="Dispose(bool)"/> and/or <see cref="DisposeAsync(bool)"/>
    /// to release managed and unmanaged resources. The finalizer calls <see cref="Dispose(bool)"/>
    /// with <c>false</c> to release unmanaged resources only.
    /// </remarks>
    public abstract class DisposableResource : IDisposable, IAsyncDisposable
    {
        /// <summary>
        /// Finalizer that invokes <see cref="Dispose(bool)"/> with <c>false</c> to release unmanaged resources.
        /// </summary>
        ~DisposableResource()
        {
            Dispose(false);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting resources.
        /// Suppresses finalization after disposal.
        /// </summary>
        [DebuggerStepThrough]
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Asynchronously performs application-defined tasks associated with freeing, releasing, or resetting resources.
        /// Suppresses finalization after disposal.
        /// </summary>
        /// <returns>A <see cref="ValueTask"/> representing the asynchronous dispose operation.</returns>
        public async ValueTask DisposeAsync()
        {
            await this.DisposeAsync(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases resources. Override in derived classes to release managed resources when <paramref name="disposing"/> is <c>true</c>.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
        }

        /// <summary>
        /// Asynchronously releases resources. Override in derived classes to release managed resources when <paramref name="disposing"/> is <c>true</c>.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous dispose operation.</returns>
        protected async virtual Task DisposeAsync(bool disposing)
        {

            await Task.Yield();
        }
    }
}
