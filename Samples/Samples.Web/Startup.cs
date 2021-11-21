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
using Samples.Web.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using RCommon;
using RCommon.DependencyInjection.Microsoft;
using RCommon.Configuration;
using RCommon.ExceptionHandling.EnterpriseLibraryCore;
using Samples.Application;
using Samples.Domain;
using AutoMapper;
using Samples.ObjectAccess.EFCore;
using RCommon.DataServices.Transactions;
using RCommon.Persistence.EFCore;
using RCommon.ApplicationServices;
using RCommon.DataServices;

namespace Samples.Web
{
    public class Startup
    {
        public Startup(IWebHostEnvironment env)
        {
            // In ASP.NET Core 3.0 `env` will be an IWebHostEnvironment, not IHostingEnvironment.
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            this.Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; private set; }


        public Startup(IConfigurationRoot configuration)
        {
            Configuration = configuration;
        }

        // ConfigureServices is where you register dependencies. This gets
        // called by the runtime before the ConfigureContainer method, below.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add services to the collection. Don't build or return
            // any IServiceProvider or the ConfigureContainer method
            // won't get called.


            // Configure RCommon
            ConfigureRCommon.Using(new DotNetCoreContainerAdapter(services)) // Allows us to use generic Dependency Injection. We could easily swap out for Autofac with a few lines of code
                .WithStateStorage<DefaultStateStorageConfiguration>() // Basic state management. This layer mostly encapsulates the web runtime. Microsoft has a bad habit of revising what an HttpContext is/means so we limit that impact.
                .WithCrudHelpers()
                .And<DataServicesConfiguration>(x=>
                    x.WithUnitOfWork<DefaultUnitOfWorkConfiguration>()) // Everything releated to transaction management. Powerful stuff happens here.
                .And<EFCoreConfiguration>(x => // Repository/ORM configuration. We could easily swap out to NHibernate without impact to domain service up through the stack
                {
                    // Add all the DbContexts here
                    x.UsingDbContext<SamplesContext>();
                })
                .And<EhabExceptionHandlingConfiguration>(x => // I prefer using Enterprise Library for this. It is one of the only fully though through libraries for exception handling.
                    x.UsingDefaultExceptionPolicies())
                .And<DomainLayerConfiguration>()
                .And<ApplicationLayerConfiguration>();
                

            // AutoMapper Mapping Profiles
            services.AddAutoMapper(x => // Where all of our DTO mapping occurs
            {
                x.AddProfile<ApplicationLayerMappingProfile>();
            });



            services.AddOptions();
            ConfigureCookieSettings(services);

            /*services.AddIdentity<ApplicationUser, IdentityRole>()
                       .AddDefaultUI()
                       .AddEntityFrameworkStores<ApplicationDbContext>()
                                       .AddDefaultTokenProviders()
                                       .AddRoleManager<RoleManager<IdentityRole>>();*/

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("Samples")));
            services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<ApplicationDbContext>();
            services.AddControllersWithViews();
            services.AddRazorPages();



            services.AddMvc(options =>
            {
                // If we wanted caching we could use this
                /*options.CacheProfiles.Add("Default0", new CacheProfile()
                {
                    NoStore = true,
                    Location = ResponseCacheLocation.None,
                    Duration = 0
                });*/

            })
            .AddSessionStateTempDataProvider();

            services.AddSession();
            services.AddRazorPages(options =>
            {

                options.Conventions.AuthorizeFolder("/admin");

            }).AddRazorRuntimeCompilation();

            services.AddControllersWithViews();

            services.AddHttpContextAccessor(); // Allows us to encapsulate the HttpContext in RCommon

        }

        // Not required for this sample, just good practice nowadays
        private static void ConfigureCookieSettings(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = Microsoft.AspNetCore.Http.SameSiteMode.None;
            });
            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.HttpOnly = true;
                options.ExpireTimeSpan = TimeSpan.FromHours(1);
                options.LoginPath = $"/Identity/Account/Login";
                options.LogoutPath = $"/Identity/Account/Logout";
                options.Cookie = new CookieBuilder
                {
                    IsEssential = true // required for auth to work without explicit user consent; adjust to suit your privacy policy

                };
            });
        }


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IServiceProvider serviceProvider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();

            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }



            app.UseStaticFiles(new StaticFileOptions // for wwwroot files
            {
                OnPrepareResponse = (context) =>
                {
                    var headers = context.Context.Response.GetTypedHeaders();
                    if (env.IsProduction())
                    {
                        headers.CacheControl = new CacheControlHeaderValue
                        {
                            Public = true,
                            MaxAge = TimeSpan.FromDays(30)

                        };
                    }


                }
            });



            app.UseRouting();
            app.UseSession();
            app.UseCookiePolicy();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
               
                endpoints.MapControllerRoute(
                        name: "default",
                        pattern: "{controller=Home}/{action=Index}/{id?}");

                endpoints.MapControllerRoute(
                    name: "divelocations",
                    pattern: "{controller=DiveLocations}/{action=Index}/{id?}");

                endpoints.MapRazorPages();
            });
        }
    }
}
