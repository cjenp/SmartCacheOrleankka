using Orleankka;
using Orleans;
using System.Threading.Tasks;

namespace CacheGrainInter
{
    public interface IDomain : IActorGrain
    { }

    public interface IDomainsInfoProjection : IActorGrain, IGrainWithGuidCompoundKey
    { }

    public interface IDomainsInfoReader : IActorGrain, IGrainWithGuidCompoundKey
    { }

    public interface IDomainProjection : IActorGrain, IGrainWithGuidCompoundKey
    { }

    public interface IDomainReader : IActorGrain
    {}
}
