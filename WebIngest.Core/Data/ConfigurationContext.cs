using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.Extensions.Configuration;
using StackExchange.Redis;
using WebIngest.Common;
using WebIngest.Common.Extensions;
using WebIngest.Common.Helpers;
using WebIngest.Common.Interfaces;
using WebIngest.Common.Models;
using WebIngest.Common.Models.OriginConfiguration;
using DataType = WebIngest.Common.Models.DataType;

namespace WebIngest.Core.Data
{
    public partial class ConfigurationContext
    {
        public DbSet<DataOrigin> DataOrigins { get; set; }
        public DbSet<DataType> DataTypes { get; set; }
        public DbSet<Mapping> Mappings { get; set; }
    }

    public partial class ConfigurationContext : IdentityDbContext
    {
        private readonly IConfiguration _configuration;
        private readonly IConnectionMultiplexer _connectionMultiplexer;

        public ConfigurationContext(
            IConnectionMultiplexer connectionMultiplexer,
            DbContextOptions<ConfigurationContext> options,
            IConfiguration configuration
        ) : base(options)
        {
            _connectionMultiplexer = connectionMultiplexer;
            _configuration = configuration;
        }

        public void Initialise()
        {
            Database.Migrate();
            SaveChanges();
            SeedData.Seed(this, _configuration);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //fluentAPI custom configuration
            modelBuilder.HasDefaultSchema(ConfigurationHelpers.ConfigSchemaName);

            modelBuilder.Entity<DataOrigin>()
                .HasIndex(x => x.Name)
                .IsUnique();

            modelBuilder.Entity<DataOrigin>()
                .Property(x => x.OriginType)
                .HasConversion(
                    x => x.ToJson(false),
                    x => x.FromJson<OriginType>(false));

            modelBuilder.Entity<DataOrigin>()
                .Property(x => x.ContentType)
                .HasConversion(
                    x => x.ToJson(false),
                    x => x.FromJson<ContentType>(false));

            modelBuilder.Entity<DataOrigin>()
                .Property(x => x.OriginTypeConfiguration)
                .HasConversion(
                    x => x.ToJson(false),
                    x => x.FromJson<OriginTypeConfiguration>(false));

            modelBuilder.Entity<DataOrigin>()
                .Property(x => x.ContentTypeConfiguration)
                .HasConversion(
                    x => x.ToJson(false),
                    x => x.FromJson<ContentTypeConfiguration>(false));

            modelBuilder.Entity<DataType>()
                .Property(x => x.Properties)
                .HasConversion(
                    x => x.ToJson(false),
                    x => x.FromJson<List<DataTypeProperty>>(false),
                    DefaultListComparer<DataTypeProperty>()
                );

            modelBuilder.Entity<Mapping>()
                .Property(x => x.PropertyMappings)
                .HasConversion(
                    x => x.ToJson(false),
                    x => x.FromJson<List<PropertyMapping>>(false),
                    DefaultListComparer<PropertyMapping>()
                );

            modelBuilder.Entity<PropertyMapping>()
                .HasNoKey()
                .Property(x => x.RegexTransform)
                .HasConversion(
                    x => x.ToJson(false),
                    x => x.FromJson<RegexTransform>(false));

            base.OnModelCreating(modelBuilder);
        }

        private ValueComparer DefaultListComparer<T>() where T : class
        {
            return new ValueComparer<List<T>>(
                (v1, v2) =>
                    v1.SequenceEqual(v2),
                c =>
                    c.Aggregate(0, (a, v) =>
                        HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()
            );
        }

        public override int SaveChanges()
        {
            //add timestamp to dated entities
            var now = DateTime.UtcNow;
            foreach (var entry in ChangeTracker.Entries<DatedEntity>())
            {
                var entity = entry.Entity;
                switch (entry.State)
                {
                    case EntityState.Added:
                        entity.Created = now;
                        entity.Updated = now;
                        break;
                    case EntityState.Modified:
                        entity.Updated = now;
                        break;
                }
            }

            ChangeTracker.DetectChanges();

            //validate entities
            var entities = from e in ChangeTracker.Entries()
                where e.State == EntityState.Added
                      || e.State == EntityState.Modified
                select e.Entity;
            foreach (var entity in entities)
            {
                var validationContext = new ValidationContext(entity);
                Validator.ValidateObject(entity, validationContext);
            }

            var changes = base.SaveChanges();

            // if changes took place, invoke the callbacks to notify them
            if (changes > 0)
                Task.Run(PublishChangesSaved);

            return changes;
        }

        private async Task PublishChangesSaved()
        {
            var subscriber = _connectionMultiplexer.GetSubscriber();
            await subscriber.PublishAsync(EventHelper.DbSaveChangesEvent, "");
        }
    }
}