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

    let tagPolicyOptions = JsonSerializerOptions()
    tagPolicyOptions.Converters.Add(JsonFSharpConverter(unionTagNamingPolicy = JsonNamingPolicy.CamelCase))

    [<Fact>]
    let ``deserialize AdjacentTag with tag policy`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"Case":"ba"}""", tagPolicyOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"Case":"bb","Fields":[32]}""", tagPolicyOptions))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""{"Case":"bc","Fields":["test",true]}""", tagPolicyOptions))

    [<Fact>]
    let ``serialize AdjacentTag with tag policy`` () =
        Assert.Equal("""{"Case":"ba"}""", JsonSerializer.Serialize(Ba, tagPolicyOptions))
        Assert.Equal("""{"Case":"bb","Fields":[32]}""", JsonSerializer.Serialize(Bb 32, tagPolicyOptions))
        Assert.Equal("""{"Case":"bc","Fields":["test",true]}""", JsonSerializer.Serialize(Bc("test", true), tagPolicyOptions))

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

    let externalTagPolicyOptions = JsonSerializerOptions()
    externalTagPolicyOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.ExternalTag, unionTagNamingPolicy = JsonNamingPolicy.CamelCase))

    [<Fact>]
    let ``deserialize ExternalTag with tag policy`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"ba":[]}""", externalTagPolicyOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"bb":[32]}""", externalTagPolicyOptions))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""{"bc":["test",true]}""", externalTagPolicyOptions))

    [<Fact>]
    let ``serialize ExternalTag with tag policy`` () =
        Assert.Equal("""{"ba":[]}""", JsonSerializer.Serialize(Ba, externalTagPolicyOptions))
        Assert.Equal("""{"bb":[32]}""", JsonSerializer.Serialize(Bb 32, externalTagPolicyOptions))
        Assert.Equal("""{"bc":["test",true]}""", JsonSerializer.Serialize(Bc("test", true), externalTagPolicyOptions))

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

    let internalTagPolicyOptions = JsonSerializerOptions()
    internalTagPolicyOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.InternalTag, unionTagNamingPolicy = JsonNamingPolicy.CamelCase))

    [<Fact>]
    let ``deserialize InternalTag with tag policy`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""["ba"]""", internalTagPolicyOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""["bb",32]""", internalTagPolicyOptions))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""["bc","test",true]""", internalTagPolicyOptions))

    [<Fact>]
    let ``serialize InternalTag with tag policy`` () =
        Assert.Equal("""["ba"]""", JsonSerializer.Serialize(Ba, internalTagPolicyOptions))
        Assert.Equal("""["bb",32]""", JsonSerializer.Serialize(Bb 32, internalTagPolicyOptions))
        Assert.Equal("""["bc","test",true]""", JsonSerializer.Serialize(Bc("test", true), internalTagPolicyOptions))

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

    let adjacentTagNamedFieldsTagPolicyOptions = JsonSerializerOptions()
    adjacentTagNamedFieldsTagPolicyOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.AdjacentTag ||| JsonUnionEncoding.NamedFields, unionTagNamingPolicy = JsonNamingPolicy.CamelCase))

    [<Fact>]
    let ``deserialize AdjacentTag NamedFields with tag policy`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"Case":"ba"}""", adjacentTagNamedFieldsTagPolicyOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"Case":"bb","Fields":{"Item":32}}""", adjacentTagNamedFieldsTagPolicyOptions))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""{"Case":"bc","Fields":{"x":"test","Item2":true}}""", adjacentTagNamedFieldsTagPolicyOptions))

    [<Fact>]
    let ``serialize AdjacentTag NamedFields with tag policy`` () =
        Assert.Equal("""{"Case":"ba"}""", JsonSerializer.Serialize(Ba, adjacentTagNamedFieldsTagPolicyOptions))
        Assert.Equal("""{"Case":"bb","Fields":{"Item":32}}""", JsonSerializer.Serialize(Bb 32, adjacentTagNamedFieldsTagPolicyOptions))
        Assert.Equal("""{"Case":"bc","Fields":{"x":"test","Item2":true}}""", JsonSerializer.Serialize(Bc("test", true), adjacentTagNamedFieldsTagPolicyOptions))

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

    let externalTagNamedFieldsTagPolicyOptions = JsonSerializerOptions()
    externalTagNamedFieldsTagPolicyOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.ExternalTag ||| JsonUnionEncoding.NamedFields, unionTagNamingPolicy = JsonNamingPolicy.CamelCase))

    [<Fact>]
    let ``deserialize ExternalTag NamedFields with tag policy`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"ba":{}}""", externalTagNamedFieldsTagPolicyOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"bb":{"Item":32}}""", externalTagNamedFieldsTagPolicyOptions))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""{"bc":{"x":"test","Item2":true}}""", externalTagNamedFieldsTagPolicyOptions))

    [<Fact>]
    let ``serialize ExternalTag NamedFields with tag policy`` () =
        Assert.Equal("""{"ba":{}}""", JsonSerializer.Serialize(Ba, externalTagNamedFieldsTagPolicyOptions))
        Assert.Equal("""{"bb":{"Item":32}}""", JsonSerializer.Serialize(Bb 32, externalTagNamedFieldsTagPolicyOptions))
        Assert.Equal("""{"bc":{"x":"test","Item2":true}}""", JsonSerializer.Serialize(Bc("test", true), externalTagNamedFieldsTagPolicyOptions))

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

    let internalTagNamedFieldsTagPolicyOptions = JsonSerializerOptions()
    internalTagNamedFieldsTagPolicyOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.InternalTag ||| JsonUnionEncoding.NamedFields, unionTagNamingPolicy = JsonNamingPolicy.CamelCase))

    [<Fact>]
    let ``deserialize InternalTag NamedFields with tag policy`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"Case":"ba"}""", internalTagNamedFieldsTagPolicyOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"Case":"bb","Item":32}""", internalTagNamedFieldsTagPolicyOptions))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""{"Case":"bc","x":"test","Item2":true}""", internalTagNamedFieldsTagPolicyOptions))

    [<Fact>]
    let ``serialize InternalTag NamedFields with tag policy`` () =
        Assert.Equal("""{"Case":"ba"}""", JsonSerializer.Serialize(Ba, internalTagNamedFieldsTagPolicyOptions))
        Assert.Equal("""{"Case":"bb","Item":32}""", JsonSerializer.Serialize(Bb 32, internalTagNamedFieldsTagPolicyOptions))
        Assert.Equal("""{"Case":"bc","x":"test","Item2":true}""", JsonSerializer.Serialize(Bc("test", true), internalTagNamedFieldsTagPolicyOptions))

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

    let adjacentTagNamedFieldsConfiguredFieldsOptions = JsonSerializerOptions()
    adjacentTagNamedFieldsConfiguredFieldsOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.AdjacentTag, "type", "args"))

    [<Fact>]
    let ``deserialize AdjacentTag NamedFields alternative Fields`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"type":"Ba"}""", adjacentTagNamedFieldsConfiguredFieldsOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"type":"Bb","args":[32]}""", adjacentTagNamedFieldsConfiguredFieldsOptions))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""{"type":"Bc","args":["test",true]}""", adjacentTagNamedFieldsConfiguredFieldsOptions))

    [<Fact>]
    let ``serialize AdjacentTag NamedFields alternative Fields`` () =
        Assert.Equal("""{"type":"Ba"}""", JsonSerializer.Serialize(Ba, adjacentTagNamedFieldsConfiguredFieldsOptions))
        Assert.Equal("""{"type":"Bb","args":[32]}""", JsonSerializer.Serialize(Bb 32, adjacentTagNamedFieldsConfiguredFieldsOptions))
        Assert.Equal("""{"type":"Bc","args":["test",true]}""", JsonSerializer.Serialize(Bc("test", true), adjacentTagNamedFieldsConfiguredFieldsOptions))

    type O = { o: option<int> }

    [<Fact>]
    let ``deserialize SuccintOption`` () =
        Assert.Equal("""{"o":123}""", JsonSerializer.Serialize({o=Some 123}, options))
        Assert.Equal("""{"o":null}""", JsonSerializer.Serialize({o=None}, options))

    [<Fact>]
    let ``serialize SuccintOption`` () =
        Assert.Equal({o=Some 123}, JsonSerializer.Deserialize("""{"o":123}""", options))
        Assert.Equal({o=None}, JsonSerializer.Deserialize("""{"o":null}""", options))

    let bareFieldlessTagsOptions = JsonSerializerOptions()
    bareFieldlessTagsOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.BareFieldlessTags))

    type S = { b: B }

    [<Fact>]
    let ``deserialize BareFieldlessTags`` () =
        Assert.Equal({b=Ba}, JsonSerializer.Deserialize("""{"b":"Ba"}""", bareFieldlessTagsOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"Case":"Bb","Fields":[32]}""", bareFieldlessTagsOptions))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""{"Case":"Bc","Fields":["test",true]}""", bareFieldlessTagsOptions))

    [<Fact>]
    let ``serialize BareFieldlessTags`` () =
        Assert.Equal("""{"b":"Ba"}""", JsonSerializer.Serialize({b=Ba}, bareFieldlessTagsOptions))
        Assert.Equal("""{"Case":"Bb","Fields":[32]}""", JsonSerializer.Serialize(Bb 32, bareFieldlessTagsOptions))
        Assert.Equal("""{"Case":"Bc","Fields":["test",true]}""", JsonSerializer.Serialize(Bc("test", true), bareFieldlessTagsOptions))

    let bareFieldlessTagsTagPolicyOptions = JsonSerializerOptions()
    bareFieldlessTagsTagPolicyOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.BareFieldlessTags, unionTagNamingPolicy = JsonNamingPolicy.CamelCase))

    [<Fact>]
    let ``deserialize BareFieldlessTags with tag policy`` () =
        Assert.Equal({b=Ba}, JsonSerializer.Deserialize("""{"b":"ba"}""", bareFieldlessTagsTagPolicyOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"Case":"bb","Fields":[32]}""", bareFieldlessTagsTagPolicyOptions))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""{"Case":"bc","Fields":["test",true]}""", bareFieldlessTagsTagPolicyOptions))

    [<Fact>]
    let ``serialize BareFieldlessTags with tag policy`` () =
        Assert.Equal("""{"b":"ba"}""", JsonSerializer.Serialize({b=Ba}, bareFieldlessTagsTagPolicyOptions))
        Assert.Equal("""{"Case":"bb","Fields":[32]}""", JsonSerializer.Serialize(Bb 32, bareFieldlessTagsTagPolicyOptions))
        Assert.Equal("""{"Case":"bc","Fields":["test",true]}""", JsonSerializer.Serialize(Bc("test", true), bareFieldlessTagsTagPolicyOptions))

    type UnionWithPropertyNames =
        | [<JsonPropertyName "nullary">] NamedNullary
        | [<JsonPropertyName "withArgs">] NamedWithArgs of int

    [<Fact>]
    let ``deserialize with JsonPropertyName on case`` () =
        Assert.Equal(NamedNullary, JsonSerializer.Deserialize("\"nullary\"", bareFieldlessTagsOptions))
        Assert.Equal(NamedWithArgs 42, JsonSerializer.Deserialize("""{"Case":"withArgs","Fields":[42]}""", bareFieldlessTagsOptions))

    [<Fact>]
    let ``serialize with JsonPropertyName on case`` () =
        Assert.Equal("\"nullary\"", JsonSerializer.Serialize(NamedNullary, bareFieldlessTagsOptions))
        Assert.Equal("""{"Case":"withArgs","Fields":[42]}""", JsonSerializer.Serialize(NamedWithArgs 42, bareFieldlessTagsOptions))

    let nonSuccintOptionOptions = JsonSerializerOptions()
    nonSuccintOptionOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.AdjacentTag))

    [<Fact>]
    let ``deserialize non-SuccintOption`` () =
        Assert.Equal("""{"o":{"Case":"Some","Fields":[123]}}""", JsonSerializer.Serialize({o=Some 123}, nonSuccintOptionOptions))
        Assert.Equal("""{"o":null}""", JsonSerializer.Serialize({o=None}, nonSuccintOptionOptions))

    [<Fact>]
    let ``serialize non-SuccintOption`` () =
        Assert.Equal({o=Some 123}, JsonSerializer.Deserialize("""{"o":{"Case":"Some","Fields":[123]}}""", nonSuccintOptionOptions))
        Assert.Equal({o=None}, JsonSerializer.Deserialize("""{"o":null}""", nonSuccintOptionOptions))

    let ignoreNullOptions = JsonSerializerOptions(IgnoreNullValues = true)
    ignoreNullOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.InternalTag ||| JsonUnionEncoding.NamedFields))

    [<AllowNullLiteral>]
    type Cls() = class end

    type UnionWithNullableArgument =
        | Foo of x: int * y: Cls

    [<Fact>]
    let ``deserialize with IgnoreNullValues`` () =
        let actual = JsonSerializer.Deserialize("""{"Case":"Foo","x":1}""", ignoreNullOptions)
        Assert.Equal(Foo(1, null), actual)

    [<Fact>]
    let ``serialize with IgnoreNullValues`` () =
        let actual = JsonSerializer.Serialize(Foo(1, null), ignoreNullOptions)
        Assert.Equal("""{"Case":"Foo","x":1}""", actual)

    let propertyNamingPolicyOptions = JsonSerializerOptions(PropertyNamingPolicy = JsonNamingPolicy.CamelCase)
    propertyNamingPolicyOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.Untagged))

    type CamelCase =
        | CCA of CcFirst: int * CcSecond: string

    [<Fact>]
    let ``deserialize with property naming policy`` () =
        let actual = JsonSerializer.Deserialize("""{"ccFirst":1,"ccSecond":"a"}""", propertyNamingPolicyOptions)
        Assert.Equal(CCA(1, "a"), actual)

    [<Fact>]
    let ``serialize with property naming policy`` () =
        let actual = JsonSerializer.Serialize(CCA(1, "a"), propertyNamingPolicyOptions)
        Assert.Equal("""{"ccFirst":1,"ccSecond":"a"}""", actual)

    type Erased = Erased of string

    [<Fact>]
    let ``deserialize erased single-case`` () =
        Assert.Equal(Erased "foo", JsonSerializer.Deserialize("\"foo\"", options))

    [<Fact>]
    let ``serialize erased single-case`` () =
        Assert.Equal("\"foo\"", JsonSerializer.Serialize(Erased "foo", options))

    let noNewtypeOptions = JsonSerializerOptions()
    noNewtypeOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.Default &&& ~~~JsonUnionEncoding.EraseSingleCaseUnions))

    [<Fact>]
    let ``deserialize non-erased single-case`` () =
        Assert.Equal(Erased "foo", JsonSerializer.Deserialize("""{"Case":"Erased","Fields":["foo"]}""", noNewtypeOptions))

    [<Fact>]
    let ``serialize non-erased single-case`` () =
        Assert.Equal("""{"Case":"Erased","Fields":["foo"]}""", JsonSerializer.Serialize(Erased "foo", noNewtypeOptions))

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

    let tagPolicyOptions = JsonSerializerOptions()
    tagPolicyOptions.Converters.Add(JsonFSharpConverter(unionTagNamingPolicy = JsonNamingPolicy.CamelCase))

    [<Fact>]
    let ``deserialize AdjacentTag with tag policy`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"Case":"ba"}""", tagPolicyOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"Case":"bb","Fields":[32]}""", tagPolicyOptions))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""{"Case":"bc","Fields":["test",true]}""", tagPolicyOptions))

    [<Fact>]
    let ``serialize AdjacentTag with tag policy`` () =
        Assert.Equal("""{"Case":"ba"}""", JsonSerializer.Serialize(Ba, tagPolicyOptions))
        Assert.Equal("""{"Case":"bb","Fields":[32]}""", JsonSerializer.Serialize(Bb 32, tagPolicyOptions))
        Assert.Equal("""{"Case":"bc","Fields":["test",true]}""", JsonSerializer.Serialize(Bc("test", true), tagPolicyOptions))

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

    let externalTagPolicyOptions = JsonSerializerOptions()
    externalTagPolicyOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.ExternalTag, unionTagNamingPolicy = JsonNamingPolicy.CamelCase))

    [<Fact>]
    let ``deserialize ExternalTag with tag policy`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"ba":[]}""", externalTagPolicyOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"bb":[32]}""", externalTagPolicyOptions))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""{"bc":["test",true]}""", externalTagPolicyOptions))

    [<Fact>]
    let ``serialize ExternalTag with tag policy`` () =
        Assert.Equal("""{"ba":[]}""", JsonSerializer.Serialize(Ba, externalTagPolicyOptions))
        Assert.Equal("""{"bb":[32]}""", JsonSerializer.Serialize(Bb 32, externalTagPolicyOptions))
        Assert.Equal("""{"bc":["test",true]}""", JsonSerializer.Serialize(Bc("test", true), externalTagPolicyOptions))

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

    let internalTagPolicyOptions = JsonSerializerOptions()
    internalTagPolicyOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.InternalTag, unionTagNamingPolicy = JsonNamingPolicy.CamelCase))

    [<Fact>]
    let ``deserialize InternalTag with tag policy`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""["ba"]""", internalTagPolicyOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""["bb",32]""", internalTagPolicyOptions))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""["bc","test",true]""", internalTagPolicyOptions))

    [<Fact>]
    let ``serialize InternalTag with tag policy`` () =
        Assert.Equal("""["ba"]""", JsonSerializer.Serialize(Ba, internalTagPolicyOptions))
        Assert.Equal("""["bb",32]""", JsonSerializer.Serialize(Bb 32, internalTagPolicyOptions))
        Assert.Equal("""["bc","test",true]""", JsonSerializer.Serialize(Bc("test", true), internalTagPolicyOptions))

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

    let adjacentTagNamedFieldsTagPolicyOptions = JsonSerializerOptions()
    adjacentTagNamedFieldsTagPolicyOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.AdjacentTag ||| JsonUnionEncoding.NamedFields, unionTagNamingPolicy = JsonNamingPolicy.CamelCase))

    [<Fact>]
    let ``deserialize AdjacentTag NamedFields with tag policy`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"Case":"ba"}""", adjacentTagNamedFieldsTagPolicyOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"Case":"bb","Fields":{"Item":32}}""", adjacentTagNamedFieldsTagPolicyOptions))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""{"Case":"bc","Fields":{"x":"test","Item2":true}}""", adjacentTagNamedFieldsTagPolicyOptions))

    [<Fact>]
    let ``serialize AdjacentTag NamedFields with tag policy`` () =
        Assert.Equal("""{"Case":"ba"}""", JsonSerializer.Serialize(Ba, adjacentTagNamedFieldsTagPolicyOptions))
        Assert.Equal("""{"Case":"bb","Fields":{"Item":32}}""", JsonSerializer.Serialize(Bb 32, adjacentTagNamedFieldsTagPolicyOptions))
        Assert.Equal("""{"Case":"bc","Fields":{"x":"test","Item2":true}}""", JsonSerializer.Serialize(Bc("test", true), adjacentTagNamedFieldsTagPolicyOptions))

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

    let externalTagNamedFieldsTagPolicyOptions = JsonSerializerOptions()
    externalTagNamedFieldsTagPolicyOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.ExternalTag ||| JsonUnionEncoding.NamedFields, unionTagNamingPolicy = JsonNamingPolicy.CamelCase))

    [<Fact>]
    let ``deserialize ExternalTag NamedFields with tag policy`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"ba":{}}""", externalTagNamedFieldsTagPolicyOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"bb":{"Item":32}}""", externalTagNamedFieldsTagPolicyOptions))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""{"bc":{"x":"test","Item2":true}}""", externalTagNamedFieldsTagPolicyOptions))

    [<Fact>]
    let ``serialize ExternalTag NamedFields with tag policy`` () =
        Assert.Equal("""{"ba":{}}""", JsonSerializer.Serialize(Ba, externalTagNamedFieldsTagPolicyOptions))
        Assert.Equal("""{"bb":{"Item":32}}""", JsonSerializer.Serialize(Bb 32, externalTagNamedFieldsTagPolicyOptions))
        Assert.Equal("""{"bc":{"x":"test","Item2":true}}""", JsonSerializer.Serialize(Bc("test", true), externalTagNamedFieldsTagPolicyOptions))

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

    let internalTagNamedFieldsTagPolicyOptions = JsonSerializerOptions()
    internalTagNamedFieldsTagPolicyOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.InternalTag ||| JsonUnionEncoding.NamedFields, unionTagNamingPolicy = JsonNamingPolicy.CamelCase))

    [<Fact>]
    let ``deserialize InternalTag NamedFields with tag policy`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"Case":"ba"}""", internalTagNamedFieldsTagPolicyOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"Case":"bb","Item":32}""", internalTagNamedFieldsTagPolicyOptions))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""{"Case":"bc","x":"test","Item2":true}""", internalTagNamedFieldsTagPolicyOptions))

    [<Fact>]
    let ``serialize InternalTag NamedFields with tag policy`` () =
        Assert.Equal("""{"Case":"ba"}""", JsonSerializer.Serialize(Ba, internalTagNamedFieldsTagPolicyOptions))
        Assert.Equal("""{"Case":"bb","Item":32}""", JsonSerializer.Serialize(Bb 32, internalTagNamedFieldsTagPolicyOptions))
        Assert.Equal("""{"Case":"bc","x":"test","Item2":true}""", JsonSerializer.Serialize(Bc("test", true), internalTagNamedFieldsTagPolicyOptions))

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

    let adjacentTagConfiguredFieldsOptions = JsonSerializerOptions()
    adjacentTagConfiguredFieldsOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.AdjacentTag, "type", "args"))

    [<Fact>]
    let ``deserialize AdjacentTag NamedFields alternative Fields`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"type":"Ba"}""", adjacentTagConfiguredFieldsOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"type":"Bb","args":[32]}""", adjacentTagConfiguredFieldsOptions))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""{"type":"Bc","args":["test",true]}""", adjacentTagConfiguredFieldsOptions))

    [<Fact>]
    let ``serialize AdjacentTag NamedFields alternative Fields`` () =
        Assert.Equal("""{"type":"Ba"}""", JsonSerializer.Serialize(Ba, adjacentTagConfiguredFieldsOptions))
        Assert.Equal("""{"type":"Bb","args":[32]}""", JsonSerializer.Serialize(Bb 32, adjacentTagConfiguredFieldsOptions))
        Assert.Equal("""{"type":"Bc","args":["test",true]}""", JsonSerializer.Serialize(Bc("test", true), adjacentTagConfiguredFieldsOptions))

    let bareFieldlessTagsOptions = JsonSerializerOptions()
    bareFieldlessTagsOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.BareFieldlessTags))

    [<Struct>]
    type S = { b: B }

    [<Fact>]
    let ``deserialize BareFieldlessTags`` () =
        Assert.Equal({b=Ba}, JsonSerializer.Deserialize("""{"b":"Ba"}""", bareFieldlessTagsOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"Case":"Bb","Fields":[32]}""", bareFieldlessTagsOptions))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""{"Case":"Bc","Fields":["test",true]}""", bareFieldlessTagsOptions))

    [<Fact>]
    let ``serialize BareFieldlessTags`` () =
        Assert.Equal("""{"b":"Ba"}""", JsonSerializer.Serialize({b=Ba}, bareFieldlessTagsOptions))
        Assert.Equal("""{"Case":"Bb","Fields":[32]}""", JsonSerializer.Serialize(Bb 32, bareFieldlessTagsOptions))
        Assert.Equal("""{"Case":"Bc","Fields":["test",true]}""", JsonSerializer.Serialize(Bc("test", true), bareFieldlessTagsOptions))

    let bareFieldlessTagsTagPolicyOptions = JsonSerializerOptions()
    bareFieldlessTagsTagPolicyOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.BareFieldlessTags, unionTagNamingPolicy = JsonNamingPolicy.CamelCase))

    [<Fact>]
    let ``deserialize BareFieldlessTags with tag policy`` () =
        Assert.Equal({b=Ba}, JsonSerializer.Deserialize("""{"b":"ba"}""", bareFieldlessTagsTagPolicyOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"Case":"bb","Fields":[32]}""", bareFieldlessTagsTagPolicyOptions))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""{"Case":"bc","Fields":["test",true]}""", bareFieldlessTagsTagPolicyOptions))

    [<Fact>]
    let ``serialize BareFieldlessTags with tag policy`` () =
        Assert.Equal("""{"b":"ba"}""", JsonSerializer.Serialize({b=Ba}, bareFieldlessTagsTagPolicyOptions))
        Assert.Equal("""{"Case":"bb","Fields":[32]}""", JsonSerializer.Serialize(Bb 32, bareFieldlessTagsTagPolicyOptions))
        Assert.Equal("""{"Case":"bc","Fields":["test",true]}""", JsonSerializer.Serialize(Bc("test", true), bareFieldlessTagsTagPolicyOptions))

    [<Struct>]
    type UnionWithPropertyNames =
        | [<JsonPropertyName "nullary">] NamedNullary
        | [<JsonPropertyName "withArgs">] NamedWithArgs of int

    [<Fact>]
    let ``deserialize with JsonPropertyName on case`` () =
        Assert.Equal(NamedNullary, JsonSerializer.Deserialize("\"nullary\"", bareFieldlessTagsOptions))
        Assert.Equal(NamedWithArgs 42, JsonSerializer.Deserialize("""{"Case":"withArgs","Fields":[42]}""", bareFieldlessTagsOptions))

    [<Fact>]
    let ``serialize with JsonPropertyName on case`` () =
        Assert.Equal("\"nullary\"", JsonSerializer.Serialize(NamedNullary, bareFieldlessTagsOptions))
        Assert.Equal("""{"Case":"withArgs","Fields":[42]}""", JsonSerializer.Serialize(NamedWithArgs 42, bareFieldlessTagsOptions))

    let ignoreNullOptions = JsonSerializerOptions(IgnoreNullValues = true)
    ignoreNullOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.InternalTag ||| JsonUnionEncoding.NamedFields))

    [<AllowNullLiteral>]
    type Cls() = class end

    [<Struct>]
    type UnionWithNullableArgument =
        | Foo of x: int * y: Cls

    [<Fact>]
    let ``deserialize with IgnoreNullValues`` () =
        let actual = JsonSerializer.Deserialize("""{"Case":"Foo","x":1}""", ignoreNullOptions)
        Assert.Equal(Foo(1, null), actual)

    [<Fact>]
    let ``serialize with IgnoreNullValues`` () =
        let actual = JsonSerializer.Serialize(Foo(1, null), ignoreNullOptions)
        Assert.Equal("""{"Case":"Foo","x":1}""", actual)

    let propertyNamingPolicyOptions = JsonSerializerOptions(PropertyNamingPolicy = JsonNamingPolicy.CamelCase)
    propertyNamingPolicyOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.Untagged))

    [<Struct>]
    type CamelCase =
        | CCA of CcFirst: int * CcSecond: string

    [<Fact>]
    let ``deserialize with property naming policy`` () =
        let actual = JsonSerializer.Deserialize("""{"ccFirst":1,"ccSecond":"a"}""", propertyNamingPolicyOptions)
        Assert.Equal(CCA(1, "a"), actual)

    [<Fact>]
    let ``serialize with property naming policy`` () =
        let actual = JsonSerializer.Serialize(CCA(1, "a"), propertyNamingPolicyOptions)
        Assert.Equal("""{"ccFirst":1,"ccSecond":"a"}""", actual)

    [<Struct>]
    type Erased = Erased of string

    [<Fact>]
    let ``deserialize erased single-case`` () =
        Assert.Equal(Erased "foo", JsonSerializer.Deserialize("\"foo\"", options))

    [<Fact>]
    let ``serialize erased single-case`` () =
        Assert.Equal("\"foo\"", JsonSerializer.Serialize(Erased "foo", options))

    let noNewtypeOptions = JsonSerializerOptions()
    noNewtypeOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.Default &&& ~~~JsonUnionEncoding.EraseSingleCaseUnions))

    [<Fact>]
    let ``deserialize non-erased single-case`` () =
        Assert.Equal(Erased "foo", JsonSerializer.Deserialize("""{"Case":"Erased","Fields":["foo"]}""", noNewtypeOptions))

    [<Fact>]
    let ``serialize non-erased single-case`` () =
        Assert.Equal("""{"Case":"Erased","Fields":["foo"]}""", JsonSerializer.Serialize(Erased "foo", noNewtypeOptions))
