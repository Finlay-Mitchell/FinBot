using Discord.Commands;
using System.Threading.Tasks;

namespace FinBot.Interactivity
{
    public interface ICriterion<in T>
    {
        Task<bool> JudgeAsync(ShardedCommandContext sourceContext, T parameter);
    }
}