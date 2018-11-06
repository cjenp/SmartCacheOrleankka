using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CacheGrainInter;
using Microsoft.AspNetCore.Mvc;

namespace WebApiO.Controllers
{
    [ApiController]
    public class ValuesController : ControllerBase
    {
        [Route("")]
        [HttpGet]
        public string Get()
        {
            return "WebApi is running. Use GET or POST.";
        }

        [Route("{id}")]
        [HttpGet]
        public async Task<string> Get(string id)
        {
            if (id.IndexOf('@') == -1)
                return "Inncorrect email!";

            string domain = id.Substring(id.IndexOf('@'));
            var grain = Startup.orleansClient.GetGrain<IDomain>(domain);
            var response = await grain.Exists(id);
            if (response)
                return "OK";
            else
                return "Not found";
        }

        [Route("{id}")]
        [HttpPost]
        public async Task<string> Post(string id)
        {
            if (id.IndexOf('@') == -1)
                return "Incorrect email!";

            string domain = id.Substring(id.IndexOf('@'));
            var grain = Startup.orleansClient.GetGrain<IDomain>(domain);
            bool isOK = await grain.AddEmail(id);

            if (isOK)
                return "Created";
            else
                return "Conflict";
        }
    }
}
