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
        | Bc of x: string * bool

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

    let untaggedOptions = JsonSerializerOptions()
    untaggedOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.Untagged))

    [<Fact>]
    let ``deserialize Untagged`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{}""", untaggedOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"Item":32}""", untaggedOptions))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""{"x":"test","Item2":true}""", untaggedOptions))

    [<Fact>]
    let ``serialize Untagged`` () =
        Assert.Equal("""{}""", JsonSerializer.Serialize(Ba, untaggedOptions))
        Assert.Equal("""{"Item":32}""", JsonSerializer.Serialize(Bb 32, untaggedOptions))
        Assert.Equal("""{"x":"test","Item2":true}""", JsonSerializer.Serialize(Bc("test", true), untaggedOptions))

    let adjacentTagNamedFieldsOptions = JsonSerializerOptions()
    adjacentTagNamedFieldsOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.AdjacentTag ||| JsonUnionEncoding.NamedFields))

    [<Fact>]
    let ``deserialize AdjacentTag NamedFields`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"Case":"Ba"}""", adjacentTagNamedFieldsOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"Case":"Bb","Fields":{"Item":32}}""", adjacentTagNamedFieldsOptions))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""{"Case":"Bc","Fields":{"x":"test","Item2":true}}""", adjacentTagNamedFieldsOptions))

    [<Fact>]
    let ``serialize AdjacentTag NamedFields`` () =
        Assert.Equal("""{"Case":"Ba"}""", JsonSerializer.Serialize(Ba, adjacentTagNamedFieldsOptions))
        Assert.Equal("""{"Case":"Bb","Fields":{"Item":32}}""", JsonSerializer.Serialize(Bb 32, adjacentTagNamedFieldsOptions))
        Assert.Equal("""{"Case":"Bc","Fields":{"x":"test","Item2":true}}""", JsonSerializer.Serialize(Bc("test", true), adjacentTagNamedFieldsOptions))

    let externalTagNamedFieldsOptions = JsonSerializerOptions()
    externalTagNamedFieldsOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.ExternalTag ||| JsonUnionEncoding.NamedFields))

    [<Fact>]
    let ``deserialize ExternalTag NamedFields`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"Ba":{}}""", externalTagNamedFieldsOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"Bb":{"Item":32}}""", externalTagNamedFieldsOptions))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""{"Bc":{"x":"test","Item2":true}}""", externalTagNamedFieldsOptions))

    [<Fact>]
    let ``serialize ExternalTag NamedFields`` () =
        Assert.Equal("""{"Ba":{}}""", JsonSerializer.Serialize(Ba, externalTagNamedFieldsOptions))
        Assert.Equal("""{"Bb":{"Item":32}}""", JsonSerializer.Serialize(Bb 32, externalTagNamedFieldsOptions))
        Assert.Equal("""{"Bc":{"x":"test","Item2":true}}""", JsonSerializer.Serialize(Bc("test", true), externalTagNamedFieldsOptions))

    let internalTagNamedFieldsOptions = JsonSerializerOptions()
    internalTagNamedFieldsOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.InternalTag ||| JsonUnionEncoding.NamedFields))

    [<Fact>]
    let ``deserialize InternalTag NamedFields`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"Case":"Ba"}""", internalTagNamedFieldsOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"Case":"Bb","Item":32}""", internalTagNamedFieldsOptions))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""{"Case":"Bc","x":"test","Item2":true}""", internalTagNamedFieldsOptions))

    [<Fact>]
    let ``serialize InternalTag NamedFields`` () =
        Assert.Equal("""{"Case":"Ba"}""", JsonSerializer.Serialize(Ba, internalTagNamedFieldsOptions))
        Assert.Equal("""{"Case":"Bb","Item":32}""", JsonSerializer.Serialize(Bb 32, internalTagNamedFieldsOptions))
        Assert.Equal("""{"Case":"Bc","x":"test","Item2":true}""", JsonSerializer.Serialize(Bc("test", true), internalTagNamedFieldsOptions))

    let internalTagNamedFieldsConfiguredTagOptions = JsonSerializerOptions()
    internalTagNamedFieldsConfiguredTagOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.InternalTag ||| JsonUnionEncoding.NamedFields, "type"))

    [<Fact>]
    let ``deserialize InternalTag NamedFields alternative Tag`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"type":"Ba"}""", internalTagNamedFieldsConfiguredTagOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"type":"Bb","Item":32}""", internalTagNamedFieldsConfiguredTagOptions))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""{"type":"Bc","x":"test","Item2":true}""", internalTagNamedFieldsConfiguredTagOptions))

    [<Fact>]
    let ``serialize InternalTag NamedFields alternative Tag`` () =
        Assert.Equal("""{"type":"Ba"}""", JsonSerializer.Serialize(Ba, internalTagNamedFieldsConfiguredTagOptions))
        Assert.Equal("""{"type":"Bb","Item":32}""", JsonSerializer.Serialize(Bb 32, internalTagNamedFieldsConfiguredTagOptions))
        Assert.Equal("""{"type":"Bc","x":"test","Item2":true}""", JsonSerializer.Serialize(Bc("test", true), internalTagNamedFieldsConfiguredTagOptions))

    let bareFieldlessTagsOptions = JsonSerializerOptions()
    bareFieldlessTagsOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.BareFieldlessTags))

    type S = { b: B }

    [<Fact>]
    let ``deserialize BareFieldlessTags`` () =
        Assert.Equal({b=Ba}, JsonSerializer.Deserialize("""{"b":"Ba"}""", bareFieldlessTagsOptions))
        Assert.Equal(Ab 32, JsonSerializer.Deserialize """{"Case":"Ab","Fields":[32]}""")
        Assert.Equal(Ac("test", true), JsonSerializer.Deserialize """{"Case":"Ac","Fields":["test",true]}""")

    [<Fact>]
    let ``serialize BareFieldlessTags`` () =
        Assert.Equal("""{"b":"Ba"}""", JsonSerializer.Serialize({b=Ba}, bareFieldlessTagsOptions))
        Assert.Equal("""{"Case":"Ab","Fields":[32]}""", JsonSerializer.Serialize(Ab 32))
        Assert.Equal("""{"Case":"Ac","Fields":["test",true]}""", JsonSerializer.Serialize(Ac("test", true)))

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
        | Bc of x: string * bool

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

    let untaggedOptions = JsonSerializerOptions()
    untaggedOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.Untagged))

    [<Fact>]
    let ``deserialize Untagged`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{}""", untaggedOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"Item":32}""", untaggedOptions))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""{"x":"test","Item2":true}""", untaggedOptions))

    [<Fact>]
    let ``serialize Untagged`` () =
        Assert.Equal("""{}""", JsonSerializer.Serialize(Ba, untaggedOptions))
        Assert.Equal("""{"Item":32}""", JsonSerializer.Serialize(Bb 32, untaggedOptions))
        Assert.Equal("""{"x":"test","Item2":true}""", JsonSerializer.Serialize(Bc("test", true), untaggedOptions))

    let adjacentTagNamedFieldsOptions = JsonSerializerOptions()
    adjacentTagNamedFieldsOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.AdjacentTag ||| JsonUnionEncoding.NamedFields))

    [<Fact>]
    let ``deserialize AdjacentTag NamedFields`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"Case":"Ba"}""", adjacentTagNamedFieldsOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"Case":"Bb","Fields":{"Item":32}}""", adjacentTagNamedFieldsOptions))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""{"Case":"Bc","Fields":{"x":"test","Item2":true}}""", adjacentTagNamedFieldsOptions))

    [<Fact>]
    let ``serialize AdjacentTag NamedFields`` () =
        Assert.Equal("""{"Case":"Ba"}""", JsonSerializer.Serialize(Ba, adjacentTagNamedFieldsOptions))
        Assert.Equal("""{"Case":"Bb","Fields":{"Item":32}}""", JsonSerializer.Serialize(Bb 32, adjacentTagNamedFieldsOptions))
        Assert.Equal("""{"Case":"Bc","Fields":{"x":"test","Item2":true}}""", JsonSerializer.Serialize(Bc("test", true), adjacentTagNamedFieldsOptions))

    let externalTagNamedFieldsOptions = JsonSerializerOptions()
    externalTagNamedFieldsOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.ExternalTag ||| JsonUnionEncoding.NamedFields))

    [<Fact>]
    let ``deserialize ExternalTag NamedFields`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"Ba":{}}""", externalTagNamedFieldsOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"Bb":{"Item":32}}""", externalTagNamedFieldsOptions))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""{"Bc":{"x":"test","Item2":true}}""", externalTagNamedFieldsOptions))

    [<Fact>]
    let ``serialize ExternalTag NamedFields`` () =
        Assert.Equal("""{"Ba":{}}""", JsonSerializer.Serialize(Ba, externalTagNamedFieldsOptions))
        Assert.Equal("""{"Bb":{"Item":32}}""", JsonSerializer.Serialize(Bb 32, externalTagNamedFieldsOptions))
        Assert.Equal("""{"Bc":{"x":"test","Item2":true}}""", JsonSerializer.Serialize(Bc("test", true), externalTagNamedFieldsOptions))

    let internalTagNamedFieldsOptions = JsonSerializerOptions()
    internalTagNamedFieldsOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.InternalTag ||| JsonUnionEncoding.NamedFields))

    [<Fact>]
    let ``deserialize InternalTag NamedFields`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"Case":"Ba"}""", internalTagNamedFieldsOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"Case":"Bb","Item":32}""", internalTagNamedFieldsOptions))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""{"Case":"Bc","x":"test","Item2":true}""", internalTagNamedFieldsOptions))

    [<Fact>]
    let ``serialize InternalTag NamedFields`` () =
        Assert.Equal("""{"Case":"Ba"}""", JsonSerializer.Serialize(Ba, internalTagNamedFieldsOptions))
        Assert.Equal("""{"Case":"Bb","Item":32}""", JsonSerializer.Serialize(Bb 32, internalTagNamedFieldsOptions))
        Assert.Equal("""{"Case":"Bc","x":"test","Item2":true}""", JsonSerializer.Serialize(Bc("test", true), internalTagNamedFieldsOptions))

    let internalTagNamedFieldsConfiguredTagOptions = JsonSerializerOptions()
    internalTagNamedFieldsConfiguredTagOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.InternalTag ||| JsonUnionEncoding.NamedFields, "type"))

    [<Fact>]
    let ``deserialize InternalTag NamedFields alternative Tag`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"type":"Ba"}""", internalTagNamedFieldsConfiguredTagOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"type":"Bb","Item":32}""", internalTagNamedFieldsConfiguredTagOptions))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""{"type":"Bc","x":"test","Item2":true}""", internalTagNamedFieldsConfiguredTagOptions))

    [<Fact>]
    let ``serialize InternalTag NamedFields alternative Tag`` () =
        Assert.Equal("""{"type":"Ba"}""", JsonSerializer.Serialize(Ba, internalTagNamedFieldsConfiguredTagOptions))
        Assert.Equal("""{"type":"Bb","Item":32}""", JsonSerializer.Serialize(Bb 32, internalTagNamedFieldsConfiguredTagOptions))
        Assert.Equal("""{"type":"Bc","x":"test","Item2":true}""", JsonSerializer.Serialize(Bc("test", true), internalTagNamedFieldsConfiguredTagOptions))

    let bareFieldlessTagsOptions = JsonSerializerOptions()
    bareFieldlessTagsOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.BareFieldlessTags))

    [<Struct>]
    type S = { b: B }

    [<Fact>]
    let ``deserialize BareFieldlessTags`` () =
        Assert.Equal({b=Ba}, JsonSerializer.Deserialize("""{"b":"Ba"}""", bareFieldlessTagsOptions))

    [<Fact>]
    let ``serialize BareFieldlessTags`` () =
        Assert.Equal("""{"b":"Ba"}""", JsonSerializer.Serialize({b=Ba}, bareFieldlessTagsOptions))
