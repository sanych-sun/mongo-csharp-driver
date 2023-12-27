using System;

namespace MongoDB.Driver.Core.Authentication
{
    /// <summary>
    /// Represents a completed SASL step.
    /// </summary>
    internal sealed class CompletedStep : ISaslStep
    {
        // fields
        private readonly byte[] _bytesToSendToServer;

        // constructors
        /// <summary>
        /// Initializes a new instance of the <see cref="CompletedStep"/> class.
        /// </summary>
        public CompletedStep()
            : this(Array.Empty<byte>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CompletedStep"/> class.
        /// </summary>
        /// <param name="bytesToSendToServer">The bytes to send to server.</param>
        public CompletedStep(byte[] bytesToSendToServer)
        {
            _bytesToSendToServer = bytesToSendToServer;
        }

        // properties
        /// <inheritdoc/>
        public byte[] BytesToSendToServer
        {
            get { return _bytesToSendToServer; }
        }

        /// <inheritdoc/>
        public bool IsComplete
        {
            get { return true; }
        }

        /// <inheritdoc/>
        public ISaslStep Transition(SaslConversation conversation, byte[] bytesReceivedFromServer)
        {
            throw new InvalidOperationException("Sasl conversation has completed.");
        }
    }
}
