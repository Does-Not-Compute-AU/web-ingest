using System;
using System.Linq;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using WebIngest.Common;
using WebIngest.Common.Extensions;
using WebIngest.Common.Models;
using WebIngest.Common.Models.OriginConfiguration;

namespace WebIngest.Core.Data
{
    public static class SeedData
    {
        public static void Seed(ConfigurationContext ctx, IConfiguration configuration)
        {
            SeedDefaults(ctx);
            SeedSuperAdmin(ctx, configuration);
        }

        private static void SeedDefaults(ConfigurationContext ctx)
        {
            if (!ctx.DataOrigins.Any())
                ctx.DataOrigins.Add(new DataOrigin()
                {
                    Id = 1,
                    Name = GlobalConstants.DefaultOriginName,
                    OriginType = OriginType.HTTP,
                    ContentType = ContentType.CSV
                });

            ctx.SaveChanges();
        }

        private static void SeedSuperAdmin(ConfigurationContext ctx, IConfiguration configuration)
        {
            var suEmail = configuration.GetSuperAdminUsername();
            var suPass = configuration.GetSuperAdminPassword();
            var debugUser = new IdentityUser()
            {
                Email = suEmail,
                UserName = suEmail,
                NormalizedEmail = suEmail.ToUpper(),
                NormalizedUserName = suEmail.ToUpper(),
                EmailConfirmed = true,
                PhoneNumberConfirmed = true,
                SecurityStamp = Guid.NewGuid().ToString("D")
            };

            var userStore = new UserStore<IdentityUser>(ctx);

            if (!userStore.Users.Any(u => u.Email == debugUser.Email))
            {
                var password = new PasswordHasher<IdentityUser>();
                var hashed = password.HashPassword(debugUser, suPass);
                debugUser.PasswordHash = hashed;

                var result = userStore.CreateAsync(debugUser).Result;

                if (!result.Succeeded)
                {
                    var errorStack = result.Errors.Select(x => x.Description).StringJoin(Environment.NewLine);
                    throw new Exception("Errors in Default User Creation: " + errorStack);
                }

                ctx.SaveChanges();
            }
        }
    }
}