using Orleans;
using System.Threading.Tasks;

namespace CacheGrainInter
{
    public interface IDomain : IGrainWithStringKey
    {
        /// <summary>
        /// Check if email exists
        /// </summary>
        /// <param name="email">Email address</param>
        /// <returns>True if email is found, false otherwise</returns>
        Task<bool> Exists(string email);

        /// <summary>
        /// Adds email
        /// </summary>
        /// <param name="email">Email address</param>
        /// <returns>True if email was added, false if the email allready exists</returns>
        Task<bool> AddEmail(string email);
    }
}
