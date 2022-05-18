using Duende.Bff.Yarp;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;

namespace MyBffSample
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
            services.AddControllersWithViews();

            services.AddBff()
                .AddServerSideSessions()
                .AddRemoteApis();

            services
                .AddAuthentication(options =>
                {
                    options.DefaultScheme = "cookie";
                    options.DefaultChallengeScheme = "oidc";
                    options.DefaultSignOutScheme = "oidc";
                })
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.Authority = "https://localhost:5001";
                    options.TokenValidationParameters.ValidateAudience = false;

                    // it's recommended to check the type header to avoid "JWT confusion" attacks
                    options.TokenValidationParameters.ValidTypes = new[] { "at+jwt" };
                })
                .AddCookie("cookie", options =>
                {
                    // set session lifetime
                    options.ExpireTimeSpan = TimeSpan.FromHours(8);

                    // sliding or absolute
                    options.SlidingExpiration = false;

                    // host prefixed cookie name
                    options.Cookie.Name = "__Host-mybff";
                    options.Cookie.SameSite = SameSiteMode.Strict;
                })
                .AddOpenIdConnect("oidc", options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.Authority = "https://localhost:5001";
                    options.ClientId = "TechnicalClientId";
                    options.ClientSecret = "gambit123";
                    options.UsePkce = true;
                    options.ResponseType = "code";
                    options.ResponseMode = "query";
                    options.GetClaimsFromUserInfoEndpoint = true;
                    options.MapInboundClaims = false;
                    options.SaveTokens = true;

                    options.Scope.Clear();
                    options.Scope.Add("openid");
                    options.Scope.Add("profile");
                    options.Scope.Add("advisory_access");
                    options.Scope.Add("offline_access");

                    options.ClaimActions.MapUniqueJsonKey("scope", "scope");

                    options.TokenValidationParameters = new()
                    {
                        NameClaimType = "name",
                        RoleClaimType = "role"
                    };
                });

            services.AddAuthorization(options =>
            {
                AuthorizationPolicyBuilder authorizationPolicyBuilder =
                    new("oidc", JwtBearerDefaults.AuthenticationScheme);
                authorizationPolicyBuilder = authorizationPolicyBuilder.RequireAuthenticatedUser();
                options.DefaultPolicy = authorizationPolicyBuilder.Build();
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseBff();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapBffManagementEndpoints();
                endpoints.MapBffManagementUserEndpoint();
                endpoints.MapDefaultControllerRoute();
                endpoints.MapControllers()
                    .RequireAuthorization()
                    .AsBffApiEndpoint(requireAntiForgeryCheck: false);
            });
        }
    }
}
