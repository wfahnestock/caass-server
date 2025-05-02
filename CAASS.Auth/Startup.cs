using System.Text;
using AutoMapper;
using CAASS.Auth.Messaging;
using CAASS.Auth.Models.Context;
using CAASS.Auth.Models.Entities;
using CAASS.Auth.Models.Entities.Mappings;
using CAASS.Auth.Services;
using CAASS.Auth.Services.Implementations;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace CAASS.Auth;

public class Startup
{
    public IConfiguration Configuration { get; }
    
    public Startup(IWebHostEnvironment env)
    {
        IConfigurationBuilder builder = new ConfigurationBuilder()
            .SetBasePath(env.ContentRootPath)
            .AddJsonFile("appsettings.json", true, true)
            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", true)
            .AddEnvironmentVariables();
        
        Configuration = builder.Build();
    }

    public void ConfigureServices(IServiceCollection services)
    {
        byte[] key = Encoding.ASCII.GetBytes(Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not found in configuration"));
        
        services.Configure<RabbitMqSettings>(Configuration.GetSection("RabbitMQ"));

        services.AddScoped<IAuthRequestService, AuthRequestService>();
        services.AddScoped(typeof(IRabbitMqPublisher<>), typeof(RabbitMqPublisher<>));
        
        services.AddSingleton<IPasswordHasher<Tenant>, PasswordHasher<Tenant>>();

        // Add AutoMapper
        services.AddAutoMapper(typeof(TenantContactMappingProfile));
        
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                
                
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    
                    ValidIssuer = Configuration["Jwt:Issuer"],
                    ValidAudience = Configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };
                
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        Console.WriteLine("OnAuthenticationFailed: " + context.Exception.Message);
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        Console.WriteLine("OnTokenValidated: " + context.SecurityToken);
                        return Task.CompletedTask;
                    }
                };
            });
        
        services.AddAuthorizationBuilder()
                    .AddPolicy("Member", policy => policy.RequireClaim("MembershipId"));


        services.AddCors(options =>
        {
            options.AddPolicy("CorsPolicy", builder =>
            {
                builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
            });

        });
        
        services.AddMemoryCache();

        services.AddDbContext<TenantContext>(options =>
        {
            options.UseNpgsql(Configuration.GetConnectionString("Postgres"));
        });

        // Add MassTransit / RabbitMQ 
        // services.AddMassTransit(options =>
        // {
        //     options.UsingRabbitMq((ctx, cfg) =>
        //     {
        //         cfg.Host(Configuration["RabbitMQ:Host"], "/", h =>
        //         {
        //             h.Username(Configuration["RabbitMQ:Username"] ?? "guest");
        //             h.Password(Configuration["RabbitMQ:Password"] ?? "guest");
        //         });
        //     });
        // });

        services.AddHealthChecks();
        
        services.AddControllers();
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        // Initialize the database
        app.InitializeDatabase();
        
        // Add static files to the request pipeline
        app.UseStaticFiles();
        
        // Add routing
        app.UseRouting();
        
        // Add CORS
        app.UseCors("CorsPolicy");
        
        app.UseDeveloperExceptionPage();
        
        // Add authentication and authorization to the request pipeline
        app.UseAuthentication();
        app.UseAuthorization();
        
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapDefaultControllerRoute();
            
            endpoints.MapHealthChecks("/health");
        });
    }
}