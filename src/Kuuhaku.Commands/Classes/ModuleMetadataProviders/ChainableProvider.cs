using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Kuuhaku.Commands.Interfaces;
using Kuuhaku.Commands.Models.Metadata;
using Kuuhaku.Infrastructure.Extensions;
using Microsoft.Extensions.Logging;

namespace Kuuhaku.Commands.Classes.ModuleMetadataProviders
{
    public class ChainableProvider : IModuleMetadataProvider, IDisposable
    {
        private readonly ILogger<ChainableProvider> _logger;
        private List<IModuleMetadataProvider> _providers;

        public ChainableProvider(ILogger<ChainableProvider> logger)
        {
            this._logger = logger;
            this._providers = new List<IModuleMetadataProvider>();
        }

        public ChainableProvider AddProvider(IModuleMetadataProvider provider)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            this._providers.Add(provider);
            return this;
        }

        public async Task LoadAsync()
        {
            for (var i = 0; i < this._providers.Count; i++)
            {
                var provider = this._providers[i];
                this._logger.Trace("Chain Loading {idx}/{total}, {provider}", i+1, this._providers.Count, provider.GetType());
                await provider.LoadAsync();
            }
        }

        public void SetCurrentPath(MetadataPath path)
        {
            foreach (var provider in this._providers)
            {
                provider.SetCurrentPath(path);
            }
        }

        public TValue GetModuleValue<TValue>(Expression<Func<ModuleMetadata, TValue>> selector)
        {
            foreach (var provider in this._providers)
            {
                var value = provider.GetModuleValue(selector);
                if (Equals(value, default(TValue)))
                    continue;
                return value;
            }

            return default;
        }

        public TValue GetCommandValue<TValue>(Expression<Func<CommandMetadata, TValue>> selector)
        {
            foreach (var provider in this._providers)
            {
                var value = provider.GetCommandValue(selector);
                if (Equals(value, default(TValue)))
                    continue;
                return value;
            }

            return default;
        }

        public TValue GetArgumentValue<TValue>(Expression<Func<ArgumentMetadata, TValue>> selector)
        {
            foreach (var provider in this._providers)
            {
                var value = provider.GetArgumentValue(selector);
                if (Equals(value, default(TValue)))
                    continue;
                return value;
            }

            return default;
        }

        public void Dispose()
        {
            foreach (var provider in this._providers)
            {
                if (provider is IDisposable disposable)
                    disposable.Dispose();
            }
        }
    }
}
