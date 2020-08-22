using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Kuuhaku.Commands.Internal
{
    /// <summary>
    /// Stolen from Discord.Net
    /// </summary>
    internal class AsyncEvent<T>
        where T : class
    {
        private readonly Object _subLock = new Object();
        internal ImmutableArray<T> _subscriptions;

        public IReadOnlyList<T> Subscriptions => this._subscriptions;

        public AsyncEvent()
        {
            this._subscriptions = ImmutableArray.Create<T>();
        }

        public void Add(T subscriber)
        {
            if (subscriber == null)
                throw new ArgumentNullException(nameof(subscriber));
            lock (this._subLock)
                this._subscriptions = this._subscriptions.Add(subscriber);
        }

        public void Remove(T subscriber)
        {
            if (subscriber == null)
                throw new ArgumentNullException(nameof(subscriber));
            lock (this._subLock)
                this._subscriptions = this._subscriptions.Remove(subscriber);
        }
    }
}
