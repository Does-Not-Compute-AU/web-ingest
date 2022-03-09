using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using WebIngest.Common.Interfaces;
using WebIngest.Common.Models;

namespace WebIngest.WebUI.Services
{
    public class EntityService
    {
        private readonly HttpClient _http;

        public EntityService(HttpClient http)
        {
            _http = http;
        }

        public async Task<INamedEntity[]> GetEntities(Type entityType)
        {
            if (entityType == typeof(DataType))
                return await _http.GetFromJsonAsync<DataType[]>("api/datatype");
            
            if (entityType == typeof(DataOrigin))
                return await _http.GetFromJsonAsync<DataOrigin[]>("api/dataorigin");
            
            if (entityType == typeof(Mapping))
                return await _http.GetFromJsonAsync<Mapping[]>("api/mapping");

            throw new NotImplementedException($"GET not implemented for type {entityType.Name}");
        }

        public async Task<HttpResponseMessage> SaveEntity(INamedEntity entity)
        {
            var entityType = entity.GetType();
            
            if (entityType == typeof(DataType))
                return await _http.PostAsJsonAsync("api/datatype", (DataType)entity);
            
            if (entityType == typeof(DataOrigin))
                return await _http.PostAsJsonAsync("api/dataorigin", (DataOrigin)entity);
            
            if (entityType == typeof(Mapping))
                return await _http.PostAsJsonAsync("api/mapping", (Mapping)entity);

            throw new NotImplementedException($"POST not implemented for type {entityType.Name}");
        }
        
        
        public async Task<HttpResponseMessage> DeleteEntity(INamedEntity entity)
        {
            var entityType = entity.GetType();
            
            if (entityType == typeof(DataType))
                return await _http.DeleteAsync($"api/datatype/{entity.Id}");
            
            if (entityType == typeof(DataOrigin))
                return await _http.DeleteAsync($"api/dataorigin/{entity.Id}");
            
            if (entityType == typeof(Mapping))
                return await _http.DeleteAsync($"api/mapping/{entity.Id}");

            throw new NotImplementedException($"DELETE not implemented for type {entityType.Name}");
        }
    }
}