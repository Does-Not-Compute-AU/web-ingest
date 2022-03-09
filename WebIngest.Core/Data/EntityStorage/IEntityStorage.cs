using System.Collections.Generic;
using WebIngest.Common.Models;

namespace WebIngest.Core.Data.EntityStorage
{
    public interface IEntityStorage
    {
        public long CountStorageEntries(DataType dataType, DataOrigin dataOrigin = null);
        public void CreateStorageLocation(DataType dataType);
        public void DeleteStorageLocation(DataType dataType);
        public void BulkInsertEntities(
            DataOrigin dataOrigin,
            Mapping mapping,
            IEnumerable<IDictionary<string, object>> entities
        );
    }
}