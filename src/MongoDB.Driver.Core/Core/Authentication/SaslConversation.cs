using System;
using System.Collections.Generic;
using MongoDB.Driver.Core.Connections;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Authentication
{
    /// <summary>
    /// Represents a SASL conversation.
    /// </summary>
    internal sealed class SaslConversation : IDisposable
    {
        // fields
        private readonly ConnectionId _connectionId;
        private readonly List<IDisposable> _itemsNeedingDisposal;
        private bool _isDisposed;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="SaslConversation"/> class.
        /// </summary>
        /// <param name="connectionId">The connection identifier.</param>
        public SaslConversation(ConnectionId connectionId)
        {
            _connectionId = connectionId;
            _itemsNeedingDisposal = new List<IDisposable>();
        }

        // properties
        /// <summary>
        /// Gets the connection identifier.
        /// </summary>
        /// <value>
        /// The connection identifier.
        /// </value>
        public ConnectionId ConnectionId
        {
            get { return _connectionId; }
        }

        /// <summary>
        /// Registers the item for disposal.
        /// </summary>
        /// <param name="item">The disposable item.</param>
        public void RegisterItemForDisposal(IDisposable item)
        {
            Ensure.IsNotNull(item, nameof(item));
            _itemsNeedingDisposal.Add(item);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                for (int i = _itemsNeedingDisposal.Count - 1; i >= 0; i--)
                {
                    _itemsNeedingDisposal[i].Dispose();
                }

                _itemsNeedingDisposal.Clear();
                _isDisposed = true;
            }
        }
    }
}
