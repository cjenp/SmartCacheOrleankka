using System.Threading.Tasks;
using CacheGrainInter;
using Microsoft.AspNetCore.Mvc;
using ServiceInterface;

namespace WebApiO.Controllers
{
    [ApiController]
    public class EmailController : ControllerBase
    {
        private IEmailCheck emailChechker;
        public EmailController(IEmailCheck _emailChechker)
        {
            emailChechker = _emailChechker;
        }

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
            var response=await emailChechker.EmailExists(id);
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
            var response = await emailChechker.AddEmail(id);
            if (response)
                return "Created";
            else
                return "Conflict";
        }
    }
}
