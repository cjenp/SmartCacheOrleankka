using System;
using System.Net;
using System.Threading.Tasks;
using CacheGrainInter;
using Microsoft.AspNetCore.Http;
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

        //GET: www.example.com/{email}
        [Route("{email}")]
        [HttpGet]
        public async Task<IActionResult> ExistsEmail(string email)
        {
            try
            {
                var response = await emailChechker.EmailExists(email);
                if (response)
                    return Ok();
                else
                    return NotFound();
            }
            catch(FormatException e)
            {
                return BadRequest(e.Message);
            }
        }

        //POST: www.example.com/{email}
        [Route("{email}")]
        [HttpPost]
        public async Task<IActionResult> Post(string email)
        {
            try
            {
                var response = await emailChechker.AddEmail(email);
                if (response)
                    return Created("", email);
                else
                    return Conflict();
            }
            catch (FormatException e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
