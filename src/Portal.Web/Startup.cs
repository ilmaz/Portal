using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Portal.Persistance;
using Portal.Domain;
using Portal.Web.Common;
using Polly;
using Portal.Web.Workers;
using Portal.Web.Hubs;

namespace Portal.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                 options.UseSqlServer(
                     Configuration.GetConnectionString("DefaultConnection")));

            services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<ApplicationDbContext>();
            services.AddControllers();
            services.AddRazorPages()
                .AddRazorRuntimeCompilation();

            services.AddHttpClient<PostClient>()
                .AddTransientHttpErrorPolicy(p =>
                p.CircuitBreakerAsync(2, TimeSpan.FromSeconds(10)));

            services.AddSignalR();

            services.AddHostedService<NotificationWorker>();
            services.AddHostedService<HealthMonitoringWorker>();

            services.AddScoped<INotificationScopedService, NotificationScopedService>();
            services.AddScoped<IHealthMonitoringService, HealthMonitoringService>();
        }

        
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapRazorPages();
                endpoints.MapHub<NotificationHub>("/notifyhub");
                endpoints.MapHub<MonitoringHub>("/monitorhub");
            });
        }
    }
}
