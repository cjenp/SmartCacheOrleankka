using ServiceInterface;
using System;
using System.Threading.Tasks;
using Orleans;
using CacheGrainInter;

namespace ServiceCode
{
    public class EmailCheck : IEmailCheck
    {
        private IClusterClient client;

        public EmailCheck(IClusterClient c)
        {
            client = c;
        }

        public async Task<bool> AddEmail(string email)
        {
            if (email.IndexOf('@') == -1)
                throw new Exception("Invalid email");

            string domain = email.Substring(email.IndexOf('@'));
            var grain = client.GetGrain<IDomain>(domain);
            return await grain.AddEmail(email);
        }

        public async Task<bool> EmailExists(string email)
        {
            if (email.IndexOf('@') == -1)
                throw new Exception("Invalid email");

            string domain = email.Substring(email.IndexOf('@'));
            var grain = client.GetGrain<IDomain>(domain);
            return await grain.Exists(email);
        }
    }
}
