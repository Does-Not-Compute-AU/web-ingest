using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebIngest.Common;
using WebIngest.Common.Extensions;
using WebIngest.Core.Data;
using WebIngest.Core.Jobs;
using WebIngest.WebAPI.CustomAttributes;
using WebIngest.WebAPI.Extensions;

namespace WebIngest.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DataController : ControllerBase
    {
        private readonly ConfigurationContext _context;
        private const Int64 MaxFileSize = 10L * 1024L * 1024L * 1024L; // 10GB

        public DataController(ConfigurationContext context)
        {
            _context = context;
        }

        // gets all data for datatype
        [HttpGet("{dataTypeId}")]
        public ActionResult<IEnumerable<object>> Get(int dataTypeId)
        {
            var dataType = _context.DataTypes.Find(dataTypeId);
            throw new NotImplementedException();
        }

        [HttpPost("{dataTypeId}")]
        [DisableFormValueModelBinding]
        [RequestSizeLimit(MaxFileSize)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxFileSize)]
        public async Task<IActionResult> Post(int dataTypeId)
        {
            try
            {
                foreach (var file in Request.Form.Files)
                {
                    await using var fileStream = await file.OpenCompressedFileStream();
                    var fileData = fileStream.ReadAllText();
                    await fileStream.DisposeAsync();
                    DataOriginJobs.SaveResult(GlobalConstants.DefaultOriginName, fileData, null, dataTypeId);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex}");
            }
        }
    }
}