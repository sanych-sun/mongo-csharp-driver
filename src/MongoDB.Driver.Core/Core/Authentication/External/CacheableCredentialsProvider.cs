﻿/* Copyright 2010-present MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver.Core.Misc;

namespace MongoDB.Driver.Core.Authentication.External
{
    internal interface ICredentialsCache<TCredentials> where TCredentials : IExternalCredentials
    {
        TCredentials CachedCredentials { get; }
        void Clear();
    }

    internal sealed class CacheableCredentialsProvider<TCredentials> : IExternalAuthenticationCredentialsProvider<TCredentials>, ICredentialsCache<TCredentials>
        where TCredentials : IExternalCredentials
    {
        private TCredentials _cachedCredentials;
        private readonly IExternalAuthenticationCredentialsProvider<TCredentials> _provider;

        public CacheableCredentialsProvider(IExternalAuthenticationCredentialsProvider<TCredentials> provider)
        {
            _provider = Ensure.IsNotNull(provider, nameof(provider));
        }

        public TCredentials CachedCredentials => _cachedCredentials;

        public void Clear() => _cachedCredentials = default;

        public TCredentials CreateCredentialsFromExternalSource(CancellationToken cancellationToken = default)
        {
            if (!TryGetCachedCredentials(out var credentials))
            {
                Clear();
                credentials = _provider.CreateCredentialsFromExternalSource(cancellationToken);
                if (credentials.Expiration.HasValue) // allows caching
                {
                    _cachedCredentials = credentials;
                }
            }
            return credentials;
        }

        public async Task<TCredentials> CreateCredentialsFromExternalSourceAsync(CancellationToken cancellationToken = default)
        {
            if (!TryGetCachedCredentials(out var credentials))
            {
                Clear();
                credentials = await _provider.CreateCredentialsFromExternalSourceAsync(cancellationToken).ConfigureAwait(false);
                if (credentials.Expiration.HasValue) // allows caching
                {
                    _cachedCredentials = credentials;
                }
            }
            return credentials;
        }

        // private method
        private bool TryGetCachedCredentials(out TCredentials credentials)
        {
            var cachedCredentials = _cachedCredentials;
            var result = cachedCredentials != null && !cachedCredentials.ShouldBeRefreshed;
            credentials = result ? cachedCredentials : default;

            return result;
        }
    }
}
