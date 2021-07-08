using Denbot.API.Models;
using Denbot.API.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;

namespace Denbot.API {
    public class Startup {
        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            services.Configure<MongoDbSettings>(Configuration.GetSection("MongoDB"));

            services.AddSingleton<IMongoClient, MongoClient>(provider => {
                var settings = provider.GetRequiredService<IOptions<MongoDbSettings>>();
                var client = new MongoClient(settings.Value.ConnectionString);
                return client;
            });
            services.AddSingleton(provider => {
                var settings = provider.GetRequiredService<IOptions<MongoDbSettings>>();
                var db = provider.GetRequiredService<IMongoClient>().GetDatabase(settings.Value.DatabaseName);
                return db;
            });
            services.AddSingleton<IRemoveRoleVoteService, RemoveRoleVoteMongoService>();
            services.AddSingleton<IGuildsService, GuildsMongoService>();

            services.AddControllers();
            services.AddSwaggerGen(c => {
                c.SwaggerDoc("v1", new OpenApiInfo {Title = "Denbot.API", Version = "v1"});
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Denbot.API v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}