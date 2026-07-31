using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ATT.Monitor.Api.Infrastructure.Swagger;

/// <summary>
/// Marca en OpenAPI los endpoints que requieren Bearer JWT.
/// </summary>
public sealed class AuthorizeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var descriptor = context.ApiDescription.ActionDescriptor as ControllerActionDescriptor;
        if (descriptor is null)
            return;

        var ctrl = descriptor.ControllerTypeInfo?.AsType();
        var allowAnonymous = descriptor.MethodInfo.GetCustomAttributes(true).OfType<AllowAnonymousAttribute>().Any()
            || ctrl?.GetCustomAttributes(typeof(AllowAnonymousAttribute), true).OfType<AllowAnonymousAttribute>().Any() == true;
        if (allowAnonymous)
            return;

        var authorize = descriptor.MethodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any()
            || ctrl?.GetCustomAttributes(typeof(AuthorizeAttribute), true).OfType<AuthorizeAttribute>().Any() == true;
        if (!authorize)
            return;

        operation.Responses.TryAdd("401", new OpenApiResponse { Description = "No autorizado" });
        operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Prohibido" });

        operation.Security =
        [
            new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            }
        ];
    }
}
