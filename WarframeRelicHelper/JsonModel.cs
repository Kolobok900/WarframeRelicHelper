using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace JsonModel
{
    public record V2ItemsResponse([property: JsonPropertyName("data")] List<V2Item> Data);
    public record V2Item([property: JsonPropertyName("slug")] string Slug, [property: JsonPropertyName("i18n")] V2I18n I18n);
    public record V2I18n([property: JsonPropertyName("en")] V2Locale En);
    public record V2Locale([property: JsonPropertyName("name")] string Name);

    public record V2OrdersResponse([property: JsonPropertyName("data")] List<V2Order> Data);
    public record V2Order([property: JsonPropertyName("platinum")] int Platinum, [property: JsonPropertyName("type")] string Type, [property: JsonPropertyName("user")] V2User User);
    public record V2User([property: JsonPropertyName("status")] string Status);

}
