using System.Text;
using Microsoft.AspNetCore.Authentication;
using ATT.Monitor.Api.Abstractions;
using ATT.Monitor.Api.Configuration;
using ATT.Monitor.Api.Infrastructure.Auth;
using ATT.Monitor.Api.Infrastructure.Persistence;
using ATT.Monitor.Api.Infrastructure.Swagger;
using ATT.Monitor.Api.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Threading.RateLimiting;

namespace ATT.Monitor.Api.Infrastructure.Composition;

public static class SecurityServiceExtensions
{
    /// <summary>
    /// JWT Bearer + validación en BD, o bypass solo Development, más rate limiting (login).
    /// </summary>
    public static WebApplicationBuilder AddAttMonitoringSecurity(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<MonitorAuthenticationOptions>(
            builder.Configuration.GetSection(MonitorAuthenticationOptions.SectionName));

        var auth = builder.Configuration.GetSection(MonitorAuthenticationOptions.SectionName)
            .Get<MonitorAuthenticationOptions>() ?? new MonitorAuthenticationOptions();
        var jwt = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>() ?? new JwtSettings();
        JwtSettings.ApplyEnvironmentDefaults(jwt);

        builder.Services.AddMemoryCache();

        builder.Services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("login", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 40,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));
        });

        if (!auth.EnableJwtBearer)
        {
            if (!builder.Environment.IsDevelopment())
            {
                throw new InvalidOperationException(
                    "Authentication:EnableJwtBearer = false solo está permitido en el entorno Development.");
            }

            Log.Warning(
                "JWT deshabilitado: esquema de autenticación '{Scheme}' (solo desarrollo). No usar en producción.",
                DevelopmentAuth.SchemeName);

            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = DevelopmentAuth.SchemeName;
                    options.DefaultChallengeScheme = DevelopmentAuth.SchemeName;
                })
                .AddScheme<AuthenticationSchemeOptions, DevelopmentBypassAuthenticationHandler>(
                    DevelopmentAuth.SchemeName,
                    _ => { });

            return builder;
        }

        if (string.IsNullOrWhiteSpace(jwt.Secret) ||
            string.IsNullOrWhiteSpace(jwt.Issuer) ||
            string.IsNullOrWhiteSpace(jwt.Audience))
        {
            throw new InvalidOperationException(
                "JWT habilitado: configure Jwt:Secret, Jwt:Issuer, Jwt:Audience (o variables JWT_SECRET, JWT_ISSUER, JWT_AUDIENCE).");
        }

        builder.Services.AddScoped<ITokenDbValidator, TokenDbValidator>();

        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwt.Issuer,
                    ValidAudience = jwt.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Secret))
                };
                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        try
                        {
                            if (!JwtBearerTokenAccessor.TryGetRawJwt(context.Request, out var rawJwt) ||
                                string.IsNullOrWhiteSpace(rawJwt))
                            {
                                context.Fail("Token Bearer ausente o inválido para validación en base de datos.");
                                return;
                            }

                            var validator = context.HttpContext.RequestServices.GetRequiredService<ITokenDbValidator>();
                            var result = await validator
                                .ValidateAsync(rawJwt, context.HttpContext.RequestAborted)
                                .ConfigureAwait(false);

                            if (result.Id == -1)
                                context.Fail("Token bloqueado en base de datos");
                        }
                        catch (Exception ex)
                        {
                            context.Fail($"Error validando token en BD: {ex.Message}");
                        }
                    }
                };
            });

        return builder;
    }

    public static IServiceCollection AddAttSwaggerWithJwt(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "ATT Monitor API v2",
                Version = "v1",
                Description = "API de monitoreo ATT — JWT + health + observabilidad."
            });
            options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                Description = "JWT: Bearer {token}"
            });
            options.OperationFilter<AuthorizeOperationFilter>();
        });
        return services;
    }

}
