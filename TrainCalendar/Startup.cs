using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TrainCalendar.Data;
using TrainCalendar.Services;
using TrainCalendar.Services.MailProcesses;
using TrainCalendar.Services.TicketParsers;

namespace TrainCalendar
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<AppSettings>(Configuration.GetSection("settings"));
            services.AddSingleton(provider => new ApplicationDbContext(Configuration.GetConnectionString("DefaultConnection")));
            services.AddSingleton<UserService>();
            services.AddHostedServiceEx<MailboxMonitoringService>();
            services.AddSingleton<ITicketParser, BookingTicketParser>();
            services.AddSingleton<RailsApiService>();
            services.AddHostedServiceEx<StationService>();
            services.AddHostedServiceEx<TrainService>();
            services.AddSingleton<IMailProcess, TicketProcess>();
            services.AddSingleton<IMailProcess, AutoReplyProcess>();
            services.AddSingleton<PushMailService>();
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
        }
    }
}