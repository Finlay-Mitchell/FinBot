using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.Commands;

namespace FinBot.Interactivity
{
    public class Criteria<T> : ICriterion<T>
    {
        private List<ICriterion<T>> _criteria = new List<ICriterion<T>>();

        public Criteria<T> AddCriterion(ICriterion<T> criterion)
        {
            _criteria.Add(criterion);
            return this;
        }

        public async Task<bool> JudgeAsync(ShardedCommandContext sourceContext, T parameter)
        {
            bool result;

            foreach (ICriterion<T> criterion in _criteria)
            {
                result = await criterion.JudgeAsync(sourceContext, parameter).ConfigureAwait(false);

                if (!result)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
