using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Text.Json;

namespace SmartTaskManagement.API.Extensions;

/// <summary>
/// Operation filter to add default values, descriptions, and deprecation info to Swagger.
/// </summary>
public class SwaggerDefaultValues : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var apiDescription = context.ApiDescription;

        operation.Deprecated |= apiDescription.IsDeprecated();

        // If the operation has no parameters, nothing more to do.
        if (operation.Parameters == null)
            return;

        foreach (var parameter in operation.Parameters)
        {
            var description = apiDescription.ParameterDescriptions
                .First(p => p.Name == parameter.Name);

            parameter.Description ??= description.ModelMetadata?.Description;

            // Set default value if available
            if (parameter.Schema.Default == null && description.DefaultValue != null)
            {
                var json = JsonSerializer.Serialize(
                    description.DefaultValue,
                    description.ModelMetadata!.ModelType);
                parameter.Schema.Default = OpenApiAnyFactory.CreateFromJson(json);
            }

            // Mark as required if it is required
            parameter.Required |= description.IsRequired;
        }

        // Add a 401 response to every secured operation
        if (operation.Security?.Any() == true)
        {
            operation.Responses.TryAdd("401", new OpenApiResponse { Description = "Unauthorized" });
            operation.Responses.TryAdd("403", new OpenApiResponse { Description = "Forbidden" });
        }
    }
}