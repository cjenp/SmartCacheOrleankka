using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using CacheGrainInter;
using Orleans;
using Orleans.Providers;

namespace CacheGrainImpl
{
    [StorageProvider(ProviderName = "blobStore")]
    public class Domain : Grain<List<string>>, IDomain
    {
        bool stateChanged = false;

        public override Task OnActivateAsync()
        {
            RegisterTimer(WriteState, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
            return base.OnActivateAsync();
        }

        private async Task WriteState(object _)
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

        public Task<bool> AddEmail(string email)
        {
            if (State == null)
                State = new List<string>();

            if (State.Contains(email))
                return Task.FromResult(false);

            State.Add(email);
            stateChanged = true;
            return Task.FromResult(true);
        }

        public Task<bool> Exists(string email)
        {
            return Task.FromResult(State.Contains(email));
        }
    }
}
