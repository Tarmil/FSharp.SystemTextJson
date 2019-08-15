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

    let externalTagOptions = JsonSerializerOptions()
    externalTagOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.ExternalTag))

    [<Fact>]
    let ``deserialize ExternalTag`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"Ba":[]}""", externalTagOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"Bb":[32]}""", externalTagOptions))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""{"Bc":["test",true]}""", externalTagOptions))

    [<Fact>]
    let ``serialize ExternalTag`` () =
        Assert.Equal("""{"Ba":[]}""", JsonSerializer.Serialize(Ba, externalTagOptions))
        Assert.Equal("""{"Bb":[32]}""", JsonSerializer.Serialize(Bb 32, externalTagOptions))
        Assert.Equal("""{"Bc":["test",true]}""", JsonSerializer.Serialize(Bc("test", true), externalTagOptions))

    let internalTagOptions = JsonSerializerOptions()
    internalTagOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.InternalTag))

    [<Fact>]
    let ``deserialize InternalTag`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""["Ba"]""", internalTagOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""["Bb",32]""", internalTagOptions))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""["Bc","test",true]""", internalTagOptions))

    [<Fact>]
    let ``serialize InternalTag`` () =
        Assert.Equal("""["Ba"]""", JsonSerializer.Serialize(Ba, internalTagOptions))
        Assert.Equal("""["Bb",32]""", JsonSerializer.Serialize(Bb 32, internalTagOptions))
        Assert.Equal("""["Bc","test",true]""", JsonSerializer.Serialize(Bc("test", true), internalTagOptions))

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

    let externalTagOptions = JsonSerializerOptions()
    externalTagOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.ExternalTag))

    [<Fact>]
    let ``deserialize ExternalTag`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"Ba":[]}""", externalTagOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"Bb":[32]}""", externalTagOptions))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""{"Bc":["test",true]}""", externalTagOptions))

    [<Fact>]
    let ``serialize ExternalTag`` () =
        Assert.Equal("""{"Ba":[]}""", JsonSerializer.Serialize(Ba, externalTagOptions))
        Assert.Equal("""{"Bb":[32]}""", JsonSerializer.Serialize(Bb 32, externalTagOptions))
        Assert.Equal("""{"Bc":["test",true]}""", JsonSerializer.Serialize(Bc("test", true), externalTagOptions))

    let internalTagOptions = JsonSerializerOptions()
    internalTagOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.InternalTag))

    [<Fact>]
    let ``deserialize InternalTag`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""["Ba"]""", internalTagOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""["Bb",32]""", internalTagOptions))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""["Bc","test",true]""", internalTagOptions))

    [<Fact>]
    let ``serialize InternalTag`` () =
        Assert.Equal("""["Ba"]""", JsonSerializer.Serialize(Ba, internalTagOptions))
        Assert.Equal("""["Bb",32]""", JsonSerializer.Serialize(Bb 32, internalTagOptions))
        Assert.Equal("""["Bc","test",true]""", JsonSerializer.Serialize(Bc("test", true), internalTagOptions))
