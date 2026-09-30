using Microsoft.OpenApi.Models;
using MultiTenantInventory.Api.Middleware;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MultiTenantInventory.Api.Configuration;

// Adds an "X-Tenant-ID" input box in Swagger UI for every [RequireTenant] endpoint.
public class TenantHeaderOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var requiresTenant = context.ApiDescription.ActionDescriptor.EndpointMetadata
            .OfType<RequireTenantAttribute>()
            .Any();

        if (!requiresTenant)
        {
            return;
        }

        operation.Parameters ??= new List<OpenApiParameter>();
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = TenantMiddleware.TenantHeader,
            In = ParameterLocation.Header,
            Required = true,
            Description = "Organization (tenant) Id. The server checks you are a member.",
            Schema = new OpenApiSchema { Type = "integer" }
        });
    }
}
