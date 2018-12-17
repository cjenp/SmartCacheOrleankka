using ServiceInterface;
using System;
using System.Threading.Tasks;
using CacheGrainInter;
using System.Net.Mail;
using Orleankka.Client;
using Orleankka;
using CacheGrainImpl;
using System.Collections.Generic;

namespace ServiceCode
{
    public class EmailCheck : IEmailCheck
    {
        private IClientActorSystem client;

        public EmailCheck(IClientActorSystem c)
        {
            client = c;
        }

        public async Task<bool> AddEmail(string email)
        {
            try
            {
                MailAddress emailAddress = new MailAddress(email);
                var domain = client.ActorOf<IDomain>(emailAddress.Host);
                await domain.Tell(new AddEmail(email));
            }
            catch (EmailConflictException)
            {
                return false;
            }
            catch (FormatException)
            {
                throw new FormatException(String.Format("Invalid email format: '{0}'.",email));
            }
            return true;
        }

        public async Task<bool> EmailExists(string email)
        {
            try
            {
                MailAddress emailAddress = new MailAddress(email);
                var domain = client.ActorOf<IDomainReader>(emailAddress.Host);
                return await domain.Ask<bool>(new CheckEmail(email));
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

        public async Task<Dictionary<string, int>> GetDomainsInfo()
        {
            var domain = client.ActorOf<IDomainsInfoReader>("#");
            return await domain.Ask<Dictionary<String, int>>(new GetDomainsInfo());
        }
    }
}
