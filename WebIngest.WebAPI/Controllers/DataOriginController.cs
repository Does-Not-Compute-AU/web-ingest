using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebIngest.Common;
using WebIngest.Common.Models;
using WebIngest.Core.Data;
using WebIngest.Core.Jobs;
using WebIngest.WebAPI.BackgroundServices;

namespace WebIngest.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DataOriginController : ControllerBase
    {
        private ConfigurationContext _context;

        public DataOriginController(ConfigurationContext context)
        {
            _context = context;
        }

        [HttpGet]
        public ActionResult<IEnumerable<DataOrigin>> Get()
        {
            return _context
                .DataOrigins
                .Where(x => x.Name != GlobalConstants.DefaultOriginName)
                .OrderBy(x => x.Id)
                .ToList();
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(DataOrigin), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Get(int id)
        {
            if (id == 1)
                return BadRequest("Cannot Modify Default Origin");

            var item = _context.DataOrigins.Find(id);
            if (item == null)
                return NotFound();

            return Ok(item);
        }

        // POST api/dataorigin/
        [HttpPost]
        public IActionResult Post([FromBody] DataOrigin value)
        {
            if (TryValidateModel(value))
            {
                if (value.Id != 0)
                    return Put(value.Id, value);

                _context.DataOrigins.Add(value);

                //return success / failure of save
                if (_context.SaveChanges() > 0)
                {
                    HangfireTasks.EnqueueBackgroundJobServerRefresh();
                    return Ok();
                }

                return StatusCode(500);
            }

            return BadRequest();
        }

        // POST api/dataorigin/test
        [HttpPost]
        [Route("test")]
        public IActionResult Test([FromBody] DataOrigin value)
        {
            if (TryValidateModel(value))
            {
                try
                {
                    var jobs = DataOriginJobs.GetJobsForOrigin(value);
                    return Ok($"Configuration yielded {jobs.Count()} background jobs for processing");
                }
                catch (Exception e)
                {
                    return BadRequest($"Job enumeration threw error: {e.Message}");
                }
            }
            return BadRequest(ModelState);
        }

        // PUT api/dataorigin/5
        [HttpPut("{id}")]
        public IActionResult Put(int id, [FromBody] DataOrigin value)
        {
            if (id == 1)
                return BadRequest("Cannot Modify Default Origin");

            if (TryValidateModel(value))
            {
                //try save
                _context.DataOrigins.Update(value);

                //return success / failure of save
                if (_context.SaveChanges() > 0)
                {
                    HangfireTasks.EnqueueBackgroundJobServerRefresh();
                    return Ok();
                }

                return StatusCode(500);
            }

            return BadRequest();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            if (id == 1)
                return BadRequest("Cannot Modify Default Origin");

            try
            {
                DataOrigin dataOrigin = _context.DataOrigins.Find(id);
                _context.DataOrigins.Attach(dataOrigin);
                _context.DataOrigins.Remove(dataOrigin);

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