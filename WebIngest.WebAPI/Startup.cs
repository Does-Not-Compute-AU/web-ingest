using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Redis.StackExchange;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
//using NSwag;
using WebIngest.Common;
using WebIngest.Core.Data;
using WebIngest.WebAPI.Hubs;
using StackExchange.Redis;
using WebIngest.Common.Models.Messaging;
using WebIngest.Core.Data.EntityStorage;
using WebIngest.Core.Info;
using WebIngest.WebAPI.BackgroundServices;

namespace WebIngest.WebAPI
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            Configuration.SelfTest();
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // configure a redis connection multiplexer and add to services
            var redisMultiplexer = ConnectionMultiplexer.Connect(Configuration.GetRedisConnString());
            services.AddSingleton<IConnectionMultiplexer>(redisMultiplexer);

            // configure storage of web-ingest configuration classes
            services.AddDbContext<ConfigurationContext>(opt =>
            {
                opt.UseLazyLoadingProxies();
                opt.UseNpgsql(Configuration.GetPgDbConnString("EntityFramework"));
            });

            // configure storage of ingested data
            if (Configuration.GetElasticStoreData())
                services.AddSingleton<IEntityStorage>(new ElasticStorageService(Configuration));
            if (Configuration.GetPgStoreData())
                services.AddSingleton<IEntityStorage>(new NpgsqlStorageService(Configuration));

            if (services.All(x => x.ServiceType != typeof(IEntityStorage)))
                throw new ApplicationException("You must configure at least one place for data storage");

            // identity storage using entity framework context
            services
                .AddDefaultIdentity<IdentityUser>(opt =>
                {
                    opt.Password.RequiredLength = 2;
                    opt.Password.RequiredUniqueChars = 1;
                    opt.Password.RequireDigit = false;
                    opt.Password.RequireLowercase = false;
                    opt.Password.RequireUppercase = false;
                    opt.Password.RequireNonAlphanumeric = false;
                })
                .AddEntityFrameworkStores<ConfigurationContext>();


            // allow CORS so API can be called from apps hosted on domains other than self
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                    builder =>
                    {
                        builder
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowAnyOrigin();
                    });
            });

            // asp identity JWT authentication
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = Configuration.GetJwtIssuer(),
                        ValidAudience = Configuration.GetJwtAudience(),
                        IssuerSigningKey =
                            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration.GetJwtSecurityKey()))
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            // If the request is for our hubs or hangfire...
                            var path = context.HttpContext.Request.Path;
                            if (
                                path.StartsWithSegments("/hubs")
                                || path.StartsWithSegments("/jobs")
                            )
                            {
                                // if not already authorized, check for jwt in cookies
                                var authToken = context.Request.Cookies["authToken"];

                                // no token in cookies, no auth.
                                if (!string.IsNullOrEmpty(authToken))
                                    context.Token = authToken;
                            }

                            return Task.CompletedTask;
                        }
                    };
                });

            // run a global redis subscriber (message broker) as background service
            services.AddHostedService<RedisSubscriber>();

            // service provider for application stats
            services.AddSingleton<StatisticsService>();

            // hangfire for background task processing, using redis as backplane
            services.AddHangfire(opt =>
            {
                opt.UseRecommendedSerializerSettings();
                opt.UseRedisStorage(redisMultiplexer);
            });

            // add signalr realtime message protocol, using redis as backplane
            services.AddSignalR()
                .AddMessagePackProtocol()
                .AddStackExchangeRedis(opt =>
                {
                    opt.ConnectionFactory = async _ => redisMultiplexer;
                });

            services
                .AddControllers()
                .AddNewtonsoftJson(opt =>
                {
                    opt.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                });

            // automatic swagger documentation of controller endpoints
            // TODO re-add once dependency conflict is resolved
            // Identity.UI -> Microsoft.Extensions.FileProviders.Embedded >= 6.0.0
            // NSwag ->  Microsoft.Extensions.FileProviders.Embedded >=5 < 6
            /*services.AddSwaggerDocument(config =>
            {
                config.PostProcess = document =>
                {
                    document.Info.Title = "WebIngest API";
                    document.Info.Contact = new OpenApiContact()
                    {
                        Name = "Sean Missingham",
                        Email = "admin@webingest.com.au",
                        Url = "https://webingest.com.au"
                    };
                    document.Info.License = new OpenApiLicense
                    {
                        Name = "GNU Affero General Public License v3",
                        Url = "https://www.gnu.org/licenses/agpl-3.0.en.html"
                    };
                };
            });*/

            services.AddResponseCompression(opts =>
            {
                opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                    new[] {"application/octet-stream"});
            });

            services.AddResponseCaching();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            using (var serviceScope = app.ApplicationServices.CreateScope())
            {
                var dbContext = serviceScope.ServiceProvider.GetRequiredService<ConfigurationContext>();
                dbContext.Initialise();
            }

            app.UseResponseCompression();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see1 https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            // TODO re-enable swagger
            //app.UseOpenApi();
            //app.UseSwaggerUi3(config => { config.CustomStylesheetPath = "/css/swagger-dark.css"; });

            app.UseRouting();

            app.UseCors();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();

            app.ConfigureHangfireInstance("WebAPI");

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapHub<SignalRHub>(NotificationViewModel.HubUrl);
                endpoints.MapFallbackToFile("index.html");
            });
        }
    }
}