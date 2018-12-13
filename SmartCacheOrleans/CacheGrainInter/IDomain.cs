using Orleankka;
using Orleans;
using System.Threading.Tasks;

namespace CacheGrainInter
{
    public interface IDomain : IActorGrain
    { }

    public interface IBreachedDomains : IActorGrain, IGrainWithGuidCompoundKey
    { }

    public interface IDomainProjection : IActorGrain, IGrainWithGuidCompoundKey
    { }
}
