using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebIngest.Core.Data;
using WebIngest.Core.Scripting;

namespace WebIngest.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ScriptingController : ControllerBase
    {
        private ConfigurationContext _context;

        public ScriptingController(ConfigurationContext context)
        {
            _context = context;
        }

        [HttpPost]
        public IActionResult Post([FromBody] string scriptSourceCode)
        {
            try
            {
                var compiledScript = new ScriptCompiler(scriptSourceCode)
                    .GenerateSourceFromScript()
                    .Compile();
                
                return Ok();
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}