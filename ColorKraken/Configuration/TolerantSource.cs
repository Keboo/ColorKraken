using System;
using System.Collections.Generic;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace ColorKraken.Configuration;

public class TolerantSource : IConfigurationSource
{
    public IConfigurationSource InternalSource { get; }
    public TolerantSource(IConfigurationSource internalSource)
    {
        InternalSource = internalSource;
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        return new TolerantProvider(InternalSource.Build(builder));
    }

    private class TolerantProvider : IConfigurationProvider
    {
        private IConfigurationProvider ConfigurationProvider { get; }

        public TolerantProvider(IConfigurationProvider internalProvider)
        {
            ConfigurationProvider = internalProvider;
        }

        public bool TryGet(string key, out string? value)
        {
            try
            {
                return ConfigurationProvider.TryGet(key, out value);
            }
            catch (Exception)
            {
                value = null;
                return false;
            }
        }

        public void Set(string key, string? value)
        {
            try
            {
                ConfigurationProvider.Set(key, value);
            }
            catch (Exception)
            { }
        }

        public IChangeToken GetReloadToken()
        {
            try
            {
                return ConfigurationProvider.GetReloadToken();
            }
            catch (Exception)
            {
                return EmptyChangeToken.Instance;
            }
        }

        public void Load()
        {
            try
            {
                ConfigurationProvider.Load();
            }
            catch (Exception)
            { }
        }

        public IEnumerable<string> GetChildKeys(IEnumerable<string> earlierKeys, string? parentPath)
        {
            try
            {
                return ConfigurationProvider.GetChildKeys(earlierKeys, parentPath);

            }
            catch (Exception)
            {
                return [];
            }
        }
    }

    private class EmptyChangeToken : IChangeToken
    {
        public static IChangeToken Instance { get; } = new EmptyChangeToken();

        public bool HasChanged => false;

        public bool ActiveChangeCallbacks => false;

        public IDisposable RegisterChangeCallback(Action<object?> callback, object? state)
        {
            return new EmptyDisposable();
        }
    }

    private sealed class EmptyDisposable : IDisposable
    {
        public void Dispose() { }
    }
}
