using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Discord.Commands;
using Kuuhaku.Commands.Interfaces;
using Kuuhaku.Commands.Models.Metadata;
using Kuuhaku.Infrastructure.Interfaces;

namespace Kuuhaku.Commands.Classes.ModuleMetadataProviders
{
    public class AttributeProvider : IModuleMetadataProvider
    {
        internal enum AttributeLevel
        {
            Module,
            Command,
            Argument,
        }

        private Dictionary<AttributeLevel, Dictionary<String, ImmutableArray<Attribute>>> _attributeMap;

        private Dictionary<AttributeLevel, Dictionary<String, (Type type, Func<Attribute, Object> func)>> _map;
        private IEnumerable<IPluginFactory> _pluginFactories;
        private MetadataPath _path;

        public AttributeProvider(IEnumerable<IPluginFactory> pluginFactories)
        {
            this._map = new Dictionary<AttributeLevel, Dictionary<String, (Type, Func<Attribute, Object>)>>();

            // Facades for Module Attributes
            this._map.Add(AttributeLevel.Module, nameof(ModuleMetadata.Name), typeof(NameAttribute),
                a => ((NameAttribute) a).Text);
            this._map.Add(AttributeLevel.Module, nameof(ModuleMetadata.Group), typeof(GroupAttribute),
                a => ((GroupAttribute) a).Prefix);
            this._map.Add(AttributeLevel.Module, nameof(ModuleMetadata.Aliases), typeof(AliasAttribute),
                a => ((AliasAttribute) a).Aliases);
            this._map.Add(AttributeLevel.Module, nameof(ModuleMetadata.Summary), typeof(SummaryAttribute),
                a => ((SummaryAttribute) a).Text);
            this._map.Add(AttributeLevel.Module, nameof(ModuleMetadata.Remarks), typeof(RemarksAttribute),
                a => ((RemarksAttribute) a).Text);

            // Facade for Command Attributes
            this._map.Add(AttributeLevel.Command, nameof(CommandMetadata.Name), typeof(NameAttribute),
                a => ((NameAttribute) a).Text);
            this._map.Add(AttributeLevel.Command, nameof(CommandMetadata.Command), typeof(CommandAttribute),
                a => ((CommandAttribute) a).Text);
            this._map.Add(AttributeLevel.Command, nameof(CommandMetadata.Aliases), typeof(AliasAttribute),
                a => ((AliasAttribute) a).Aliases);
            this._map.Add(AttributeLevel.Command, nameof(CommandMetadata.Summary), typeof(SummaryAttribute),
                a => ((SummaryAttribute) a).Text);
            this._map.Add(AttributeLevel.Command, nameof(CommandMetadata.Remarks), typeof(RemarksAttribute),
                a => ((RemarksAttribute) a).Text);
            this._map.Add(AttributeLevel.Command, nameof(CommandMetadata.Priority), typeof(PriorityAttribute),
                a => ((PriorityAttribute) a).Priority);

            // Facade for Argument Attributes
            this._map.Add(AttributeLevel.Argument, nameof(ArgumentMetadata.Summary), typeof(SummaryAttribute),
                a => ((SummaryAttribute)a).Text);
            this._map.Add(AttributeLevel.Argument, nameof(ArgumentMetadata.TypeReader), typeof(OverrideTypeReaderAttribute),
                a => ((OverrideTypeReaderAttribute)a).TypeReader);
            this._map.Add(AttributeLevel.Argument, nameof(ArgumentMetadata.Remainder), typeof(RemainderAttribute),
                a => a is RemainderAttribute);

            this._attributeMap = new Dictionary<AttributeLevel, Dictionary<String, ImmutableArray<Attribute>>>();
            this._attributeMap[AttributeLevel.Module] = new Dictionary<String, ImmutableArray<Attribute>>();
            this._attributeMap[AttributeLevel.Command] = new Dictionary<String, ImmutableArray<Attribute>>();
            this._attributeMap[AttributeLevel.Argument] = new Dictionary<String, ImmutableArray<Attribute>>();

            this._pluginFactories = pluginFactories;
        }

        public Task LoadAsync()
        {
            this._attributeMap.Clear();
            this._attributeMap[AttributeLevel.Module] = new Dictionary<String, ImmutableArray<Attribute>>();
            this._attributeMap[AttributeLevel.Command] = new Dictionary<String, ImmutableArray<Attribute>>();
            this._attributeMap[AttributeLevel.Argument] = new Dictionary<String, ImmutableArray<Attribute>>();

            foreach (var factory in this._pluginFactories)
            {
                var assembly = factory.GetType().Assembly;
                var modules = assembly.GetTypes()
                    .Where(CustomModuleBuilder.IsValidModuleDefinition)
                    .Select(t => t.GetTypeInfo());

                foreach (var module in modules)
                {
                    this._attributeMap[AttributeLevel.Module][module.FullName] =
                        module.GetCustomAttributes<Attribute>().ToImmutableArray();

                    var commands = module.DeclaredMethods.Where(CustomModuleBuilder.IsValidCommandDefinition);

                    foreach (var command in commands)
                    {
                        this._attributeMap[AttributeLevel.Command][$"{module.FullName}.{command.Name}"] =
                            command.GetCustomAttributes().ToImmutableArray();

                        var arguments = command.GetParameters();
                        for (var i = 0; i < arguments.Length; i++)
                        {
                            this._attributeMap[AttributeLevel.Argument][$"{module.FullName}.{command.Name}[{i}]"] =
                                arguments[i].GetCustomAttributes().ToImmutableArray();
                        }
                    }
                }
            }

            return Task.CompletedTask;
        }

        public void SetCurrentPath(MetadataPath path)
        {
            this._path = path;
        }

        public TValue GetModuleValue<TValue>(Expression<Func<ModuleMetadata, TValue>> selector)
        {
            var memberName = selector.GetMemberName();
            var attributes = this._attributeMap[AttributeLevel.Module][this.GetCurrentPath(AttributeLevel.Module)];

            var (type, func) = this._map[AttributeLevel.Module][memberName];
            var attribute = attributes.FirstOrDefault(a => a.GetType() == type);

            if (attribute == null)
                return default;

            var value = (TValue) func(attribute);

            return value;
        }

        public TValue GetCommandValue<TValue>(Expression<Func<CommandMetadata, TValue>> selector)
        {
            var memberName = selector.GetMemberName();
            var attributes = this._attributeMap[AttributeLevel.Command][this.GetCurrentPath(AttributeLevel.Command)];

            var (type, func) = this._map[AttributeLevel.Command][memberName];
            var attribute = attributes.FirstOrDefault(a => a.GetType() == type);

            if (attribute == null)
                return default;

            var value = (TValue) func(attribute);

            return value;
        }

        public TValue GetArgumentValue<TValue>(Expression<Func<ArgumentMetadata, TValue>> selector)
        {
            var memberName = selector.GetMemberName();
            var attributes = this._attributeMap[AttributeLevel.Argument][this.GetCurrentPath(AttributeLevel.Argument)];

            var (type, func) = this._map[AttributeLevel.Argument][memberName];
            var attribute = attributes.FirstOrDefault(a => a.GetType() == type);

            if (attribute == null)
                return default;

            var value = (TValue) func(attribute);

            return value;
        }

        private String GetCurrentPath(AttributeLevel level)
        {
#pragma warning disable 8509
            return level switch
#pragma warning restore 8509
            {
                AttributeLevel.Module => this._path.CurrentModule,
                AttributeLevel.Command => $"{this._path.CurrentModule}.{this._path.CurrentCommand}",
                AttributeLevel.Argument =>
                    $"{this._path.CurrentModule}.{this._path.CurrentCommand}[{this._path.CurrentArgument.Value}]",
            };
        }
    }

    internal static class AttributeProviderHelper
    {
        public static void Add(
            this Dictionary<AttributeProvider.AttributeLevel, Dictionary<String, (Type, Func<Attribute, Object>)>> map,
            AttributeProvider.AttributeLevel level, String propertyName, Type attributeType,
            Func<Attribute, Object> func)
        {
            if (!map.ContainsKey(level))
                map[level] = new Dictionary<String, (Type, Func<Attribute, Object>)>();

            map[level][propertyName] = (attributeType, func);
        }

        public static String GetMemberName<TExpressionType>(this Expression<TExpressionType> expression)
        {
            var expr = expression.Body switch
            {
                MemberExpression memberExpression => memberExpression,
                UnaryExpression unaryExpression => (MemberExpression) unaryExpression.Operand,
                _ => throw new ArgumentException($@"Expression '{expression}' not supported.")
            };

            return expr.Member.Name;
        }
    }
}
