open System.Text.Json
open System.Text.Json.Serialization
open Xunit

[<Fact>]
let ``Regression #77`` () =
    let options = JsonSerializerOptions()
    options.Converters.Add(JsonFSharpConverter())
    Assert.Equal<int list>([1; 2; 3], JsonSerializer.Deserialize("[1,2,3]", options))
    Assert.Equal("""[1,2,3]""", JsonSerializer.Serialize([1;2;3], options))

type Create =
    {
        rooms: Set<int> Skippable
    }

[<Fact>]
let other () =
    let options = JsonSerializerOptions()
    options.Converters.Add(
        JsonFSharpConverter(
            JsonUnionEncoding.Default |||
            JsonUnionEncoding.InternalTag |||
            JsonUnionEncoding.NamedFields |||
            JsonUnionEncoding.UnwrapFieldlessTags,
            allowOverride = true,
            unionTagCaseInsensitive = true,
            unionTagNamingPolicy = JsonNamingPolicy.CamelCase)
        )
    Assert.Equal({ rooms = Skip }, JsonSerializer.Deserialize<Create>("{}", options))

[<EntryPoint>]
let main args = Xunit.ConsoleClient.Program.Main args