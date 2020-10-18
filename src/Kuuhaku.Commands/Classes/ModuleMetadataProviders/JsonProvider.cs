using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Kuuhaku.Commands.Interfaces;
using Kuuhaku.Commands.Models.Metadata;
using Kuuhaku.Infrastructure.Extensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Kuuhaku.Commands.Classes.ModuleMetadataProviders
{
    public class JsonProvider : IModuleMetadataProvider
    {
        private String _path;
        private readonly ILogger<JsonProvider> _logger;

        private Dictionary<String, ModuleMetadata> _metadata;
        private MetadataPath _metadataPath;

        public JsonProvider(String path, ILogger<JsonProvider> logger)
        {
            this._path = path;
            this._logger = logger;
            this._metadata = new Dictionary<String, ModuleMetadata>();
        }

        public async Task LoadAsync()
        {
            this._metadata.Clear();
            var directory = new DirectoryInfo(this._path);
            if (!directory.Exists)
                directory.Create();

            foreach (var file in directory.GetFiles("*.json"))
            {
                var relativePath = file.FullName.Replace(AppContext.BaseDirectory, "");
                this._logger.Trace("Reading metadata from {path}", relativePath);

                using var fs = new StreamReader(file.OpenRead());
                var json = await fs.ReadToEndAsync();
                var contents = JsonConvert.DeserializeObject<Dictionary<String, ModuleMetadata>>(json);

                foreach (var (module, metadata) in contents)
                {
                    this._metadata[module] = metadata;
                }
            }

            foreach (var (_, moduleMetadata) in this._metadata)
            {
                foreach (var (_, commandMetadata) in moduleMetadata.Commands)
                {
                    foreach (var argumentMetadata in commandMetadata.Arguments)
                    {
                        argumentMetadata.Parent = commandMetadata;
                    }
                    commandMetadata.Parent = moduleMetadata;
                }
            }

            var moduleCount = this._metadata.Count;
            var commandCount = 0;
            if (moduleCount > 0)
                commandCount = this._metadata.Sum(m => m.Value?.Commands?.Count ?? 0);

            this._logger.Trace("Loaded metadata for {moduleCount} modules and {commandCount} commands",
                moduleCount, commandCount);
        }

        public void SetCurrentPath(MetadataPath path)
        {
            this._metadataPath = path;
        }

        public TValue GetModuleValue<TValue>(Expression<Func<ModuleMetadata, TValue>> selector)
        {
            if (!this.KeyExists(true, false, false))
                return default;

            var expr = selector.Compile();
            return expr(this._metadata[this._metadataPath.CurrentModule]);
        }

        public TValue GetCommandValue<TValue>(Expression<Func<CommandMetadata, TValue>> selector)
        {
            if (!this.KeyExists(true, true, false))
                return default;
            var expr = selector.Compile();
            return expr(this._metadata[this._metadataPath.CurrentModule].Commands[this._metadataPath.CurrentCommand]);
        }

        public TValue GetArgumentValue<TValue>(Expression<Func<ArgumentMetadata, TValue>> selector)
        {
            if (!this.KeyExists(true, true, true))
                return default;
            var expr = selector.Compile();
            return expr(this._metadata[this._metadataPath.CurrentModule].Commands[this._metadataPath.CurrentCommand].Arguments[this._metadataPath.CurrentArgument.Value]);
        }

        private Boolean KeyExists(Boolean module, Boolean command, Boolean argument)
        {
            if (this._metadata == null)
                return false;
            if (module && !this._metadata.ContainsKey(this._metadataPath.CurrentModule))
                return false;
            if (command && !this._metadata[this._metadataPath.CurrentModule].Commands
                .ContainsKey(this._metadataPath.CurrentCommand))
                return false;
            if (argument && (!this._metadataPath.CurrentArgument.HasValue || this._metadataPath.CurrentArgument.Value >=
                this._metadata[this._metadataPath.CurrentModule].Commands[this._metadataPath.CurrentCommand].Arguments.Count))
                return false;
            return true;
        }
    }
}
