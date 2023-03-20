using System;
using System.Collections.Generic;
using System.Linq;
using Castle.Core.Internal;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebIngest.Common;
using WebIngest.Common.Extensions;
using WebIngest.Common.Models;
using WebIngest.Common.Models.OriginConfiguration;
using WebIngest.Core.Data;
using WebIngest.Core.Extraction;
using WebIngest.Core.Jobs;

namespace WebIngest.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class MappingController : ControllerBase
    {
        private ConfigurationContext _context;

        public MappingController(ConfigurationContext context)
        {
            _context = context;
        }

        // GET api/mapping
        [HttpGet]
        public ActionResult<IEnumerable<Mapping>> Get()
        {
            return _context
                .Mappings
                .Where(x => x.DataOrigin.Name != GlobalConstants.DefaultOriginName) // exclude default seed
                .OrderBy(x => x.Id)
                .ToList();
        }

        // GET api/mapping/5
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Mapping), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Get(int id)
        {
            var item = _context.Mappings.Find(id);
            if (item == null || item.DataOrigin.Name == GlobalConstants.DefaultOriginName)
                return NotFound();
            return Ok(item);
        }


        // POST api/mapping/{valued}
        [HttpPost]
        public IActionResult Post([FromBody] Mapping value)
        {
            if (TryValidateModel(value))
            {
                if (value.Id != 0)
                    return Put(value.Id, value);

                value.DataType = null;
                value.DataOrigin = null;

                _context.Mappings.Add(value);

                return _context.SaveChanges() > 0
                    ? Ok()
                    : StatusCode(400);
            }

            return BadRequest();
        }

        // POST api/dataorigin/test
        [HttpPost]
        [Route("test")]
        public IActionResult Test([FromBody] MappingTestInputModel input)
        {
            try
            {
                var mapping = input.Mapping;
                var sourceContent = input.ExampleSourceContent;
                if (TryValidateModel(mapping))
                {
                    if (sourceContent.IsNullOrEmpty())
                    {
                        return BadRequest("Automatic derivation of test content is not yet supported");
                    }

                    var dataOrigin = _context.DataOrigins.Find(mapping.DataOrigin.Id);
                    if (dataOrigin.ContentType == ContentType.HTML)
                    {
                        var res = HtmlExtractor.ValuesFromHtml(mapping,
                            dataOrigin.ContentTypeConfiguration,
                            dataOrigin.OriginTypeConfiguration,
                            sourceContent
                        );
                        
                        return Ok(res);
                    }
                    else
                    {
                        return BadRequest(
                            "Live-test not supported for this content type yet, please submit a request!");
                    }
                }
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }


            return BadRequest();
        }

        // PUT api/mapping/5
        [HttpPut("{id}")]
        public IActionResult Put(int id, [FromBody] Mapping value)
        {
            if (TryValidateModel(value))
            {
                value.DataType = null;
                value.DataOrigin = null;

                //try save
                _context.Mappings.Update(value);

                //return success / failure of save
                if (_context.SaveChanges() > 0) return Ok();
                return StatusCode(500);
            }

            return BadRequest();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            try
            {
                Mapping mapping = _context.Mappings.Find(id);

                // dont allow deletion of default mappings
                if (mapping.DataOriginName == GlobalConstants.DefaultOriginName)
                    return BadRequest();

                _context.Mappings.Attach(mapping);
                _context.Mappings.Remove(mapping);

                if (_context.SaveChanges() > 0)
                {
                    return Ok();
                }

                return StatusCode(500);
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }
    }
}