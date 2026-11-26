using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace EquipmentBorrowing.Api;

/// <summary>Aligns the generated OpenAPI description with the published contract.</summary>
public sealed class ContractDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument document, DocumentFilterContext context)
    {
        // Requests
        Shape(document, "StudentRequest", "studentNumber,fullName", "contact");
        Shape(document, "EquipmentRequest", "equipmentCode,name", "description");
        Shape(document, "BorrowRequest", "studentId,equipmentId,expectedReturnDate", "");
        Shape(document, "CheckoutRequest", "studentId,equipmentIds,expectedReturnDate", "");
        Shape(document, "EligibilityRequest", "isEligible", "");

        // Responses
        Shape(document, "StudentDto", "id,studentNumber,fullName,contact,isEligible", "contact");
        Shape(document, "EquipmentDto", "id,equipmentCode,name,description,isAvailable", "description");
        Shape(document, "BorrowingDto",
            "id,studentId,equipmentId,borrowedDate,expectedReturnDate,returnedDate,status", "returnedDate");
        Shape(document, "CheckoutResponse", "borrowings", "");
        Shape(document, "ErrorBody", "code,message", "");

        var schemas = document.Components.Schemas;
        var ids = schemas["CheckoutRequest"].Properties["equipmentIds"];
        ids.MinItems = 2;
        ids.UniqueItems = true;
        schemas["BorrowingDto"].Properties["status"].Enum = Values("Active", "Returned");

        foreach (var path in document.Paths)
        {
            foreach (var operation in path.Value.Operations.Values)
            {
                foreach (var parameter in operation.Parameters)
                {
                    switch (parameter.Name)
                    {
                        case "available":
                            parameter.Schema = new OpenApiSchema { Type = "boolean" };
                            break;
                        case "status":
                            parameter.Schema.Enum = Values("Active", "Returned", "All");
                            parameter.Schema.Default = new OpenApiString("All");
                            break;
                        case "sort":
                            parameter.Schema.Enum = Values("borrowedDate", "expectedReturnDate");
                            parameter.Schema.Default = new OpenApiString("borrowedDate");
                            break;
                        case "direction":
                            parameter.Schema.Enum = Values("asc", "desc");
                            parameter.Schema.Default = new OpenApiString("asc");
                            break;
                    }
                }

                if (operation.OperationId is "CreateStudent" or "CreateEquipment" or "CreateBorrowing")
                {
                    operation.Responses["201"].Headers["Location"] = new OpenApiHeader
                    {
                        Description = "URL of the created resource.",
                        Schema = new OpenApiSchema { Type = "string" }
                    };
                }
            }
        }
    }

    private static void Shape(OpenApiDocument document, string name, string required, string nullable)
    {
        var schema = document.Components.Schemas[name];
        var nullableNames = nullable.Split(',');
        schema.Required = new HashSet<string>(required.Split(','));

        foreach (var property in schema.Properties)
            property.Value.Nullable = nullableNames.Contains(property.Key);
    }

    private static List<IOpenApiAny> Values(params string[] values) =>
        values.Select(v => (IOpenApiAny)new OpenApiString(v)).ToList();
}
