using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CacheGrainInter;
using Orleankka.Meta;

namespace CacheGrainImpl
{
    public class Domain : EventSourcedActor, IDomain
    {
        bool stateChanged = false;
        HashSet<String> emails=new HashSet<String>();

        /// <summary>
        /// Executes on grain activation and starts a timer for saving to storage
        /// </summary>
        /// <returns></returns>
        public override Task OnActivateAsync()
        {
            //RegisterTimer(WriteState, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
            return base.OnActivateAsync();
        }

        /// <summary>
        /// Saves grain state before grain deactivates
        /// </summary>
        /// <returns></returns>
        public override Task OnDeactivateAsync()
        {
            //WriteState(null).GetAwaiter().GetResult();
            return base.OnDeactivateAsync();
        }

        /// <summary>
        /// Saves grain state to blob storage
        /// </summary>
        /// <param name="_"></param>
        /// <returns></returns>
        /*private async Task WriteState(object _)
        {
            if (!stateChanged)
                return;
            stateChanged = false;

            try
            {
                await WriteStateAsync();
            }
            catch
            {
                stateChanged = true;
            }
        }

        /// <summary>
        /// Adds email
        /// </summary>
        /// <param name="email">Email address</param>
        /// <returns>True if email was added, false if the email allready exists</returns>
        public Task<bool> AddEmail(string email)
        {
            if (State == null)
                State = new HashSet<string>();

            stateChanged = State.Add(email);
            return Task.FromResult(stateChanged);
        }

        /// <summary>
        /// Check if email exists
        /// </summary>
        /// <param name="email">Email address</param>
        /// <returns>True if email is found, false otherwise</returns>
        public Task<bool> Exists(string email)
        {
            if (State == null)
                return Task.FromResult(false);
            else
                return Task.FromResult(State.Contains(email));
        }*/


        void On(DomainAddedEmail e)
        {
            emails.Add(e.Name);
        }

        IEnumerable<Event> Handle(AddEmail cmd)
        {
            if (string.IsNullOrEmpty(cmd.Name))
                throw new ArgumentException("Inventory item name cannot be null or empty");

            if (emails.Contains(cmd.Name))
                throw new Exception("Email allready exists");

            yield return new DomainAddedEmail(cmd.Name);
        }

        bool Handle(GetDetails query)
        {
            return true;
        }

    }
}
