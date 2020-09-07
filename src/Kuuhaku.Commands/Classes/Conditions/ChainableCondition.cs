using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord.Commands;
using Kuuhaku.Commands.Interfaces;

namespace Kuuhaku.Commands.Classes.Conditions
{
    public class ChainableCondition<T> : ICondition<T>
    {
        private List<ICondition<T>> _conditions;

        public ChainableCondition()
        {
            this._conditions = new List<ICondition<T>>();
        }

        public ChainableCondition(IEnumerable<ICondition<T>> conditions)
        {
            this._conditions = conditions.ToList();
        }

        public ICondition<T> AddCondition(ICondition<T> condition)
        {
            this._conditions.Add(condition);
            return this;
        }

        public async Task<Boolean> ValidateAsync(ICommandContext context, T param)
        {
            foreach (var condition in this._conditions)
            {
                var res = await condition.ValidateAsync(context, param);
                if (!res)
                    return false;
            }

            return true;
        }
    }
}
