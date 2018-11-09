using System.Net;
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
        public async Task<IActionResult> ExistsEmail(string id)
        {
            var response=await emailChechker.EmailExists(id);
            if (response)
                return Ok();
            else
                return NotFound();
        }

        //POST: www.example.com/{id}
        [Route("{id}")]
        [HttpPost]
        public async Task<IActionResult> Post(string id)
        {
            var response = await emailChechker.AddEmail(id);
            if (response)
                return Created("", id);
            else
                return Conflict();
        }
    }
}
