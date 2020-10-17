using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.Commands.Builders;
using Kuuhaku.Commands.Attributes;
using Kuuhaku.Commands.Interfaces;
using Kuuhaku.Commands.Models;
using Kuuhaku.Commands.Models.Metadata;
using Kuuhaku.Infrastructure.Extensions;
using Kuuhaku.Infrastructure.Interfaces;
using Kuuhaku.Infrastructure.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ParameterInfo = System.Reflection.ParameterInfo;

namespace Kuuhaku.Commands.Classes
{
    public class CustomModuleBuilder : IHostedService, IModuleBuilder, IDisposable
    {
        private readonly IModuleMetadataProvider _metadataProvider;
        private readonly CommandService _commandService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<CustomModuleBuilder> _logger;

        public ImmutableList<ModuleInfo> Modules { get; private set; }

        public CustomModuleBuilder(IModuleMetadataProvider metadataProvider, CommandService commandService, IServiceProvider serviceProvider, ILogger<CustomModuleBuilder> logger)
        {
            this._metadataProvider = metadataProvider;
            this._commandService = commandService;
            this._serviceProvider = serviceProvider;
            this._logger = logger;
            this.Modules = ImmutableList<ModuleInfo>.Empty;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // TODO: Watch for changes to any .json files and issue a reload of the metadata.
            this._logger.Debug("CustomModuleBuilder starting.");
            await this._metadataProvider.LoadAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            // ??
            return Task.CompletedTask;
        }

        public async Task BuildAsync(params Assembly[] assemblies)
        {
            var createdModules = new List<ModuleInfo>();
            var modules = assemblies.SelectMany(t => t.GetTypes())
                .Where(IsValidModuleDefinition)
                .ToImmutableArray();

            foreach (var module in modules)
            {
                var currentPath = new MetadataPath(module.FullName);
                this._metadataProvider.SetCurrentPath(currentPath);

                var group = this._metadataProvider.GetModuleValue(m => m.Group);

                if (group.IsEmpty())
                    group = "";

                var moduleInfo = await this._commandService.CreateModuleAsync(group,
                        b => this.BuildModule(b, module.GetTypeInfo(), currentPath))
                    .ConfigureAwait(false);

                createdModules.Add(moduleInfo);
            }

            if (createdModules.Count > 0)
                this.Modules = this.Modules.Union(createdModules).ToImmutableList();
        }

        public static Boolean IsValidModuleDefinition(Type type)
            => typeof(KuuhakuModule).IsAssignableFrom(type) && !type.IsAbstract;

        public static Boolean IsValidCommandDefinition(MethodInfo method)
            => !method.IsDefined(typeof(NotACommandAttribute)) &&
               (method.ReturnType == typeof(Task) || method.ReturnType == typeof(Task<RuntimeResult>)) &&
               !method.IsStatic &&
               !method.IsGenericMethod;

        public void Dispose()
        {
            if (this._metadataProvider is IDisposable disposable)
                disposable.Dispose();
        }

        private void BuildModule(ModuleBuilder builder, TypeInfo moduleType, MetadataPath currentPath)
        {
            var attributes = moduleType.GetCustomAttributes()
                .ToImmutableArray();

            var preconditions = attributes.Where(a => a is PreconditionAttribute);
            foreach (var precondition in preconditions)
                builder.AddPrecondition(precondition as PreconditionAttribute);

            builder.AddAttributes(attributes.Where(a => !(a is PreconditionAttribute)).ToArray());

            builder.WithName(this._metadataProvider.GetModuleValue(m => m.Name))
                .WithSummary(this._metadataProvider.GetModuleValue(m => m.Summary))
                .WithRemarks(this._metadataProvider.GetModuleValue(m => m.Remarks))
                .AddAliases(this._metadataProvider.GetModuleValue(m => m.Aliases) ?? new String[0]);

            if (builder.Name.IsEmpty())
                builder.Name = moduleType.Name;

            var commands = moduleType.DeclaredMethods.Where(IsValidCommandDefinition);
            foreach (var command in commands)
            {
                currentPath.CurrentCommand = command.Name;
                currentPath.CurrentArgument = null;
                this._metadataProvider.SetCurrentPath(currentPath);

                this.BuildCommand(builder, moduleType, command, currentPath);
            }
        }

        private void BuildCommand(ModuleBuilder builder, TypeInfo moduleType, MethodInfo method,
            MetadataPath currentPath)
        {
            var name = this._metadataProvider.GetCommandValue(c => c.Name);
            var command = this._metadataProvider.GetCommandValue(c => c.Command);

            var isDefault = command == null && !builder.Aliases[0].IsEmpty();

            // This command is not configured properly
            if (!isDefault && command.IsEmpty())
                return;

            async Task<IResult> ExecuteCommand(ICommandContext context, Object[] args, IServiceProvider services,
                CommandInfo cmd)
            {
                var instance = (IModule) services.GetRequiredService(moduleType);
                instance.SetContext(context as KuuhakuCommandContext);

                try
                {
                    instance.BeforeExecute(cmd);
                    var task = method.Invoke(instance, args) as Task ?? Task.CompletedTask;
                    if (task is Task<RuntimeResult> resultTask)
                        return await resultTask;
                    await task;
                    return ExecuteResult.FromSuccess();
                }
                finally
                {
                    instance.AfterExecute(cmd);
                    (instance as IDisposable)?.Dispose();
                    if (instance is IAsyncDisposable disposable)
                        await disposable.DisposeAsync();
                }
            }

            void CreateCommand(CommandBuilder builder)
            {
                var attributes = method.GetCustomAttributes()
                    .ToImmutableArray();

                var preconditions = attributes.Where(a => a is PreconditionAttribute);
                foreach (var precondition in preconditions)
                    builder.AddPrecondition(precondition as PreconditionAttribute);

                builder.AddAttributes(attributes.Where(a => !(a is PreconditionAttribute)).ToArray());

                // TODO: Permissions
                // TODO: Ratelimiting
                // TODO: Generic Precondition Values?

                builder.WithPriority(this._metadataProvider.GetCommandValue(c => c.Priority))
                    .WithSummary(this._metadataProvider.GetCommandValue(c => c.Summary))
                    .WithRemarks(this._metadataProvider.GetCommandValue(c => c.Remarks))
                    .AddAliases(this._metadataProvider.GetCommandValue(c => c.Aliases) ?? new String[0]);

                if (builder.Name.IsEmpty())
                    builder.Name = method.Name.Replace("Async", "");

                var parameters = method.GetParameters();
                for (var i = 0; i < parameters.Length; i++)
                {
                    currentPath.CurrentArgument = i;
                    this._metadataProvider.SetCurrentPath(currentPath);
                    this.BuildArgument(builder, parameters[i], (current: i, total: parameters.Length));
                }
            }

            var primaryAlias = isDefault
                ? ""
                : command.IsEmpty()
                    ? name
                    : command;

            builder.AddCommand(primaryAlias, ExecuteCommand, CreateCommand);
        }

        private void BuildArgument(CommandBuilder builder, ParameterInfo parameter, (Int32 current, Int32 total) position)
        {
            var attributes = parameter.GetCustomAttributes()
                .ToImmutableArray();

            void CreateArgument(ParameterBuilder builder)
            {
                var preconditions = attributes.Where(a => a is ParameterPreconditionAttribute);
                foreach (var precondition in preconditions)
                    builder.AddPrecondition(precondition as ParameterPreconditionAttribute);
                builder.AddAttributes(attributes.Where(a => !(a is ParameterPreconditionAttribute)).ToArray());

                builder.IsOptional = parameter.IsOptional;
                builder.DefaultValue = parameter.HasDefaultValue ? parameter.DefaultValue : null;

                builder.WithSummary(this._metadataProvider.GetArgumentValue(a => a.Summary));

                var overrideTypeReader = this._metadataProvider.GetArgumentValue(a => a.TypeReader);
                if (overrideTypeReader != null)
                    builder.TypeReader = this.GetTypeReader(builder.ParameterType, overrideTypeReader);

                var remainder = this._metadataProvider.GetArgumentValue(a => a.Remainder);
                if (remainder || position.current == position.total - 1)
                {
                    if (position.current != position.total - 1)
                        throw new InvalidOperationException(
                            $"Remainder parameters must be the last parameter in a command. Paramerter: {parameter.Name} in {parameter.Member?.DeclaringType?.FullName ?? "Unknown"}.{parameter.Member.Name}");
                    builder.IsRemainder = true;
                }

                if (builder.TypeReader == null)
                {
                    var readers = this._commandService.TypeReaders.FirstOrDefault(t => t.Key == builder.ParameterType);
                    TypeReader reader;

                    if (readers != null)
                    {
                        reader = readers.FirstOrDefault();
                    }
                    else
                    {
                        reader = (TypeReader) typeof(CommandService).GetMethod("GetDefaultTypeReader")?.Invoke(this._commandService, new Object[] { builder.ParameterType }) ?? null;
                    }

                    builder.TypeReader = reader;
                }
            }

            var isParams = attributes.Any(a => a is ParamArrayAttribute);
            builder.AddParameter(parameter.Name,
                isParams ? parameter.ParameterType.GetElementType() : parameter.ParameterType, CreateArgument);
        }

        private TypeReader GetTypeReader(Type paramType, Type overrideTypeReader)
        {
            var readers = this._commandService.TypeReaders.FirstOrDefault(t => t.Key == paramType);
            var overrideReader = readers?.FirstOrDefault(t => t.GetType() == overrideTypeReader);
            if (readers != null && overrideReader != null)
                return overrideReader;

            var reader = (TypeReader) this._serviceProvider.GetService(overrideTypeReader);
            this._commandService.AddTypeReader(paramType, reader);
            return reader;
        }
    }
}
