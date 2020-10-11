using System;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Kuuhaku.Commands.Models.Metadata;

namespace Kuuhaku.Commands.Interfaces
{
    public interface IModuleMetadataProvider
    {
        Task LoadAsync();
        void SetCurrentPath(MetadataPath path);
        TValue GetModuleValue<TValue>(Expression<Func<ModuleMetadata, TValue>> selector);
        TValue GetCommandValue<TValue>(Expression<Func<CommandMetadata, TValue>> selector);
        TValue GetArgumentValue<TValue>(Expression<Func<ArgumentMetadata, TValue>> selector);
    }
}
