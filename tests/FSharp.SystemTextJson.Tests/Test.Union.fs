module Tests.Union

open Xunit
open System.Text.Json.Serialization
open System.Text.Json

module NonStruct =

    [<JsonFSharpConverter>]
    type A =
        | Aa
        | Ab of int
        | Ac of string * bool

    [<Fact>]
    let ``deserialize via explicit converter`` () =
        Assert.Equal(Aa, JsonSerializer.Deserialize """{"Case":"Aa"}""")
        Assert.Equal(Ab 32, JsonSerializer.Deserialize """{"Case":"Ab","Fields":[32]}""")
        Assert.Equal(Ac("test", true), JsonSerializer.Deserialize """{"Case":"Ac","Fields":["test",true]}""")

    [<Fact>]
    let ``serialize via explicit converter`` () =
        Assert.Equal("""{"Case":"Aa"}""", JsonSerializer.Serialize Aa)
        Assert.Equal("""{"Case":"Ab","Fields":[32]}""", JsonSerializer.Serialize(Ab 32))
        Assert.Equal("""{"Case":"Ac","Fields":["test",true]}""", JsonSerializer.Serialize(Ac("test", true)))

    type B =
        | Ba
        | Bb of int
        | Bc of string * bool

    let options = JsonSerializerOptions()
    options.Converters.Add(JsonFSharpConverter())

    [<Fact>]
    let ``deserialize via options`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"Case":"Ba"}""", options))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"Case":"Bb","Fields":[32]}""", options))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""{"Case":"Bc","Fields":["test",true]}""", options))

    [<Fact>]
    let ``serialize via options`` () =
        Assert.Equal("""{"Case":"Ba"}""", JsonSerializer.Serialize(Ba, options))
        Assert.Equal("""{"Case":"Bb","Fields":[32]}""", JsonSerializer.Serialize(Bb 32, options))
        Assert.Equal("""{"Case":"Bc","Fields":["test",true]}""", JsonSerializer.Serialize(Bc("test", true), options))

    [<CompilationRepresentation(CompilationRepresentationFlags.UseNullAsTrueValue)>]
    type C =
        | Ca
        | Cb of int

    [<Fact>]
    let ``deserialize UseNull`` () =
        Assert.Equal(Ca, JsonSerializer.Deserialize("""null""", options))
        Assert.Equal(Cb 32, JsonSerializer.Deserialize("""{"Case":"Cb","Fields":[32]}""", options))

    [<Fact>]
    let ``serialize UseNull`` () =
        Assert.Equal("""null""", JsonSerializer.Serialize(Ca, options))
        Assert.Equal("""{"Case":"Cb","Fields":[32]}""", JsonSerializer.Serialize(Cb 32, options))

module Struct =

    [<Struct; JsonFSharpConverter>]
    type A =
        | Aa
        | Ab of int
        | Ac of string * bool

    [<Fact>]
    let ``deserialize via explicit converter`` () =
        Assert.Equal(Aa, JsonSerializer.Deserialize """{"Case":"Aa"}""")
        Assert.Equal(Ab 32, JsonSerializer.Deserialize """{"Case":"Ab","Fields":[32]}""")
        Assert.Equal(Ac("test", true), JsonSerializer.Deserialize """{"Case":"Ac","Fields":["test",true]}""")

    [<Fact>]
    let ``serialize via explicit converter`` () =
        Assert.Equal("""{"Case":"Aa"}""", JsonSerializer.Serialize Aa)
        Assert.Equal("""{"Case":"Ab","Fields":[32]}""", JsonSerializer.Serialize(Ab 32))
        Assert.Equal("""{"Case":"Ac","Fields":["test",true]}""", JsonSerializer.Serialize(Ac("test", true)))

    [<Struct>]
    type B =
        | Ba
        | Bb of int
        | Bc of string * bool

    let options = JsonSerializerOptions()
    options.Converters.Add(JsonFSharpConverter())

    [<Fact>]
    let ``deserialize via options`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"Case":"Ba"}""", options))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"Case":"Bb","Fields":[32]}""", options))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""{"Case":"Bc","Fields":["test",true]}""", options))

    [<Fact>]
    let ``serialize via options`` () =
        Assert.Equal("""{"Case":"Ba"}""", JsonSerializer.Serialize(Ba, options))
        Assert.Equal("""{"Case":"Bb","Fields":[32]}""", JsonSerializer.Serialize(Bb 32, options))
        Assert.Equal("""{"Case":"Bc","Fields":["test",true]}""", JsonSerializer.Serialize(Bc("test", true), options))
