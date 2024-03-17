using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Bankai.MLApi.Utils;

public class EnumDictionaryToStringDictionarySchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        // Only for fields that are Dictionary<Enum, TValue>
        if (!context.Type.IsGenericType)
            return;

        if (!context.Type.GetGenericTypeDefinition().IsAssignableFrom(typeof(Dictionary<,>))
            && !context.Type.GetGenericTypeDefinition().IsAssignableFrom(typeof(IDictionary<,>)))
            return;

        var keyType = context.Type.GetGenericArguments()[0];

        if (!keyType.IsEnum)
            return;

        var valueType = context.Type.GetGenericArguments()[1];
        var valueTypeSchema = context.SchemaGenerator.GenerateSchema(valueType, context.SchemaRepository);

        schema.Type = "object";
        schema.Properties.Clear();
        schema.AdditionalPropertiesAllowed = true;
        schema.AdditionalProperties = valueTypeSchema;
    }
}