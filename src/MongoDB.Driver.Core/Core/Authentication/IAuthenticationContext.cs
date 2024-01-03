using System.Net;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Authentication
{
    /// <summary>
    /// Represent authentication state.
    /// </summary>
    public interface IAuthenticationContext
    {
        /// <summary>
        /// The current endpoint.
        /// </summary>
        EndPoint CurrentEndPoint { get; }
    }

    internal class AuthenticationContext : IAuthenticationContext
    {
        public AuthenticationContext(EndPoint endPoint)
        {
            CurrentEndPoint = Ensure.IsNotNull(endPoint, nameof(endPoint));
        }

        public EndPoint CurrentEndPoint { get; }
    }
}
