using ServiceInterface;
using System;
using System.Threading.Tasks;
using Orleans;
using CacheGrainInter;
using System.Net.Mail;

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
            try
            {
                MailAddress emailAddress = new MailAddress(email);
                var grain = client.GetGrain<IDomain>(emailAddress.Host);
                return await grain.AddEmail(email);
            }
            catch (FormatException)
            {
                throw new FormatException(String.Format("Invalid email format: '{0}'.",email));
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        public async Task<bool> EmailExists(string email)
        {
            try
            {
                MailAddress emailAddress = new MailAddress(email);
                var grain = client.GetGrain<IDomain>(emailAddress.Host);
                return await grain.Exists(email);
            }
            catch (FormatException)
            {
                throw new FormatException(String.Format("Invalid email format: '{0}'.", email));
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }
    }
}
