using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceInterface
{
    public interface IEmailCheck
    {
        /// <summary>
        /// Check if email exists
        /// </summary>
        /// <param name="email">Email address</param>
        /// <returns>True if email is found, false otherwise</returns>
        Task<bool> EmailExists(string email);

        /// <summary>
        /// Adds email
        /// </summary>
        /// <param name="email">Email address</param>
        /// <returns>True if email was added, false if the email allready exists</returns>
        Task<bool> AddEmail(string email);

        /// <summary>
        /// Adds email
        /// </summary>
        /// <param name="email">Email address</param>
        /// <returns>True if email was added, false if the email allready exists</returns>
        Task<Dictionary<string,int>> GetDomainsInfo();
    }
}
