namespace Bankai.MLApi.Infrastructure.Extensions;

public static class TransformerExtenstion
{
    public static byte[] ToByteArray(this ITransformer transformer, MLContext mlContext, DataViewSchema schema)
    {
        using var stream = new MemoryStream();
        mlContext.Model.Save(transformer, schema, stream);

        return stream.ToArray();
    }
}