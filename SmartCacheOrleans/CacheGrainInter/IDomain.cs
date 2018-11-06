using Orleans;
using System.Threading.Tasks;

namespace CacheGrainInter
{
    public interface IDomain : IGrainWithStringKey
    {
        Task<bool> Exists(string email);
        Task<bool> AddEmail(string email);
    }
}
