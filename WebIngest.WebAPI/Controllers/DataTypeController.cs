using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebIngest.Common;
using WebIngest.Common.Models;
using WebIngest.Core.Data;
using WebIngest.Core.Data.EntityStorage;

namespace WebIngest.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DataTypeController : ControllerBase
    {
        private readonly ConfigurationContext _context;
        private readonly IEnumerable<IEntityStorage> _storages;

        public DataTypeController(ConfigurationContext context, IEnumerable<IEntityStorage> storages)
        {
            _context = context;
            _storages = storages;
        }

        [HttpGet]
        public ActionResult<IEnumerable<DataType>> Get()
        {
            return _context.DataTypes.OrderBy(x => x.Id).ToList();
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(DataOrigin), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public IActionResult Get(int id)
        {
            var item = _context.DataTypes.Find(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        // POST api/dataorigin/
        [HttpPost]
        public IActionResult Post([FromBody] DataType dataType)
        {
            if (TryValidateModel(dataType))
            {
                if (dataType.Id != 0)
                    return Put(dataType.Id, dataType);

                _context.DataTypes.Add(dataType);
                CreateDefaultOriginMapping(dataType);

                if (_context.SaveChanges() > 0)
                {
                    foreach (var storage in _storages)
                        storage.CreateStorageLocation(dataType);
                    
                    return Ok();
                }

                return StatusCode(400);
            }

            return BadRequest();
        }


        // PUT api/values/5
        [HttpPut("{id}")]
        public IActionResult Put(int id, [FromBody] DataType dataType)
        {
            if (TryValidateModel(dataType))
            {
                //try save
                var dbDataType = _context.DataTypes.AsNoTracking().FirstOrDefault(x => x.Id == id);
                if (dataType.Equals(dbDataType))
                    return Ok();

                _context.DataTypes.Update(dataType);
                CreateDefaultOriginMapping(dataType);

                //return success / failure of save
                if (_context.SaveChanges() > 0)
                {
                    foreach (var storage in _storages)
                        storage.CreateStorageLocation(dataType);
                    return Ok();
                }

                return StatusCode(500);
            }

            return BadRequest();
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            try
            {
                DataType dataType = _context.DataTypes.Find(id);
                foreach (var storage in _storages)
                    storage.DeleteStorageLocation(dataType);
                _context.DataTypes.Attach(dataType);
                _context.DataTypes.Remove(dataType);

                if (_context.SaveChanges() > 0)
                {
                    foreach (var storage in _storages)
                        storage.DeleteStorageLocation(dataType);
                    return Ok();
                }

                return StatusCode(500);
            }
            catch (Exception)
            {
                return BadRequest();
            }
        }

        private void CreateDefaultOriginMapping(DataType dataType)
        {
            var defaultOriginId = _context.DataOrigins.First(x => x.Name == GlobalConstants.DefaultOriginName).Id;
            var mapping = 
                _context
                    .Mappings
                    .FirstOrDefault(x => 
                        x.DataOriginId == defaultOriginId
                        && x.DataType == dataType
                    )
            ?? new Mapping()
            {
                DataOriginId = defaultOriginId,
                DataType = dataType
            };
            
            _context.Mappings.Attach(mapping);
            
            mapping.PropertyMappings = dataType
                .Properties
                .Select(x =>
                    new PropertyMapping()
                    {
                        DataTypeProperty = x.PropertyName,
                        Selector = x.PropertyName
                    })
                .ToList();
        }
    }
}