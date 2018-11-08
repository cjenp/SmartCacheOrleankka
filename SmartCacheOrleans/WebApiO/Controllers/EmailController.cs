using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CacheGrainInter;
using Microsoft.AspNetCore.Mvc;

namespace WebApiO.Controllers
{
    [ApiController]
    public class EmailController : ControllerBase
    {
        //GET: www.example.com/
        [Route("")]
        [HttpGet]
        public string Info()
        {
            return "WebApi is running. Use GET or POST.";
        }

        //GET: www.example.com/{id}
        [Route("{id}")]
        [HttpGet]
        public async Task<string> ExistsEmail(string id)
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

        //POST: www.example.com/{id}
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
