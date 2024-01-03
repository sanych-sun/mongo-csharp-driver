using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Connections;

namespace MongoDB.Driver.Core.Authentication
{
    /// <summary>
    /// Represents a SASL mechanism.
    /// </summary>
    internal interface ISaslMechanism
    {
        // properties
        /// <summary>
        /// Gets the name of the mechanism.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        string Name { get; }

        // methods
        /// <summary>
        /// Initializes the mechanism.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="conversation">The SASL conversation.</param>
        /// <param name="description">The connection description.</param>
        /// <returns>The initial SASL step.</returns>
        ISaslStep Initialize(IConnection connection, SaslConversation conversation, ConnectionDescription description);

        /// <summary>
        /// Initializes the mechanism.
        /// </summary>
        /// <param name="connection">The connection.</param>
        /// <param name="conversation">The SASL conversation.</param>
        /// <param name="description">The connection description.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The initial SASL step.</returns>
        Task<ISaslStep> InitializeAsync(IConnection connection, SaslConversation conversation, ConnectionDescription description, CancellationToken cancellationToken);
    }
}
