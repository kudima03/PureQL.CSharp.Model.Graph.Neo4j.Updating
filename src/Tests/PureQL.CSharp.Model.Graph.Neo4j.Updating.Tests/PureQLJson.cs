using System.Text.Json;
using System.Text.Json.Serialization;
using PureQL.CSharp.Model.Serialization;

namespace PureQL.CSharp.Model.Graph.Neo4j.Updating.Tests;

/// <summary>
/// JSON through every PureQL converter, to compare models. The options are
/// reflection-based: a source-generated context over the model does not compile in
/// reasonable time.
/// </summary>
internal static class PureQLJson
{
    /// <summary>The deepest queries nest past the default limit of 64.</summary>
    private static JsonSerializerOptions Options { get; } = CreateOptions();

    public static string Serialize(PureQLQuery query)
    {
        return JsonSerializer.Serialize(query, Options);
    }

    private static JsonSerializerOptions CreateOptions()
    {
        JsonSerializerOptions options = new JsonSerializerOptions { MaxDepth = 256 };
        foreach (JsonConverter converter in new PureQLConverters())
        {
            options.Converters.Add(converter);
        }

        return options;
    }
}
