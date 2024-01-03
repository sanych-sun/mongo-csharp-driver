using System.Threading;
using System.Threading.Tasks;

namespace MongoDB.Driver.Core.Authentication
{
    /// <summary>
    /// Represents a SASL step.
    /// </summary>
    internal interface ISaslStep
    {
        // properties
        /// <summary>
        /// Gets the bytes to send to server.
        /// </summary>
        /// <value>
        /// The bytes to send to server.
        /// </value>
        byte[] BytesToSendToServer { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is complete.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is complete; otherwise, <c>false</c>.
        /// </value>
        bool IsComplete { get; }

        // methods
        /// <summary>
        /// Transitions the SASL conversation to the next step.
        /// </summary>
        /// <param name="conversation">The SASL conversation.</param>
        /// <param name="bytesReceivedFromServer">The bytes received from server.</param>
        /// <returns>The next SASL step.</returns>
        ISaslStep Transition(SaslConversation conversation, byte[] bytesReceivedFromServer);

        /// <summary>
        /// Transitions the SASL conversation to the next step.
        /// </summary>
        /// <param name="conversation">The SASL conversation.</param>
        /// <param name="bytesReceivedFromServer">The bytes received from server.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The next SASL step.</returns>
        Task<ISaslStep> TransitionAsync(SaslConversation conversation, byte[] bytesReceivedFromServer, CancellationToken cancellationToken = default);
    }
}

