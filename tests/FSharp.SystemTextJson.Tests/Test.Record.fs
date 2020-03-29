module Tests.Record

open Xunit
open System.Text.Json.Serialization
open System.Text.Json
open System.Linq.Expressions

module NonStruct =

    type Foo() =
        [<JsonPropertyName "Baz">]
        member val FooBar = 1 with get, set

    [<JsonFSharpConverter>]
    type A =
        {
            ax: int
            ay: string
        }

    [<Fact>]
    let ``deserialize via explicit converter`` () =
        let actual = JsonSerializer.Deserialize """{"ax":1,"ay":"b"}"""
        Assert.Equal({ax=1;ay="b"}, actual)

    [<Fact>]
    let ``serialize via explicit converter`` () =
        let actual = JsonSerializer.Serialize {ax=1;ay="b"}
        Assert.Equal("""{"ax":1,"ay":"b"}""", actual)

    type B =
        {
            bx: int
            by: string
        }

    let options = JsonSerializerOptions()
    options.Converters.Add(JsonFSharpConverter())

    [<Fact>]
    let ``deserialize via options`` () =
        let actual = JsonSerializer.Deserialize("""{"bx":1,"by":"b"}""", options)
        Assert.Equal({bx=1;by="b"}, actual)

    [<Fact>]
    let ``serialize via options`` () =
        let actual = JsonSerializer.Serialize({bx=1;by="b"}, options)
        Assert.Equal("""{"bx":1,"by":"b"}""", actual)

    [<Fact>]
    let ``not fill in nulls`` () =
        try
            JsonSerializer.Deserialize<B>("""{"bx": 1, "by": null}""", options) |> ignore
            failwith "Deserialization was supposed to fail on the line above"
        with
        | :? JsonException as e -> Assert.Equal("B.by was expected to be of type String, but was null.", e.Message)

    [<Fact>]
    let ``get a good error message when omitting non-optional fields in a record`` () =
        try
            JsonSerializer.Deserialize<B>("""{"bx": 1}""", options) |> ignore
            failwith "Deserialization was supposed to fail on the line above"
        with
        | :? JsonException as e -> Assert.Equal("Missing field for record type Tests.Record+NonStruct+B: by", e.Message)

    type SomeSearchApi =
        {
            filter: string option
            limit: int option
            offset: int option
        }

    let ``allow omitting fields that are optional`` () =
        let result  = JsonSerializer.Deserialize<SomeSearchApi>("""{}""", options)
        Assert.Equal(result, {filter=None; limit=None; offset=None})
        let result = JsonSerializer.Deserialize<SomeSearchApi>("""{"limit": 50}""", options)
        Assert.Equal(result, {filter=None; limit=Some 50; offset=None})

    [<Fact>]
    let ``allowNullFields`` () =
        let options = JsonSerializerOptions()
        options.Converters.Add(JsonFSharpConverter(allowNullFields = true))
        let actual = JsonSerializer.Deserialize("""{"bx":1,"by":null}""", options)
        Assert.Equal({bx=1;by=null}, actual)

    type S =
        {
            sa: int
            sb: Skippable<int>
            sc: Skippable<int option>
            sd: Skippable<int voption>
        }

    [<Fact>]
    let ``deserialize skippable field`` () =
        let actual = JsonSerializer.Deserialize("""{"sa":1}""", options)
        Assert.Equal({sa=1;sb=Skip;sc=Skip;sd=Skip}, actual)
        let actual = JsonSerializer.Deserialize("""{"sa":1,"sb":2,"sc":null,"sd":null}""", options)
        Assert.Equal({sa=1;sb=Include 2;sc=Include None;sd=Include ValueNone}, actual)
        let actual = JsonSerializer.Deserialize("""{"sa":1,"sb":2,"sc":3,"sd":4}""", options)
        Assert.Equal({sa=1;sb=Include 2;sc=Include(Some 3);sd=Include(ValueSome 4)}, actual)

    [<Fact>]
    let ``serialize skippable field`` () =
        let actual = JsonSerializer.Serialize({sa=1;sb=Skip;sc=Skip;sd=Skip}, options)
        Assert.Equal("""{"sa":1}""", actual)
        let actual = JsonSerializer.Serialize({sa=1;sb=Include 2;sc=Include None;sd=Include ValueNone}, options)
        Assert.Equal("""{"sa":1,"sb":2,"sc":null,"sd":null}""", actual)
        let actual = JsonSerializer.Serialize({sa=1;sb=Include 2;sc=Include(Some 3);sd=Include(ValueSome 4)}, options)
        Assert.Equal("""{"sa":1,"sb":2,"sc":3,"sd":4}""", actual)

    type C =
        {
            cx: B
        }

    [<Fact>]
    let ``deserialize nested`` () =
        let actual = JsonSerializer.Deserialize("""{"cx":{"bx":1,"by":"b"}}""", options)
        Assert.Equal({cx={bx=1;by="b"}}, actual)

    [<Fact>]
    let ``deserialize anonymous`` () =
        let actual = JsonSerializer.Deserialize("""{"x":1,"y":"b"}""", options)
        Assert.Equal({|x=1;y="b"|}, actual)

    [<Fact>]
    let ``serialize anonymous`` () =
        let actual = JsonSerializer.Serialize({|x=1;y="b"|}, options)
        Assert.Equal("""{"x":1,"y":"b"}""", actual)

    type PropName =
        {
            unnamedX: int
            [<JsonPropertyName "namedY">] unnamedY: string
        }

    [<Fact>]
    let ``deserialize with JsonPropertyName`` () =
        let actual = JsonSerializer.Deserialize("""{"unnamedX":1,"namedY":"b"}""", options)
        Assert.Equal({unnamedX = 1; unnamedY = "b"}, actual)

    [<Fact>]
    let ``serialize with JsonPropertyName`` () =
        let actual = JsonSerializer.Serialize({unnamedX = 1;unnamedY = "b"}, options)
        Assert.Equal("""{"unnamedX":1,"namedY":"b"}""", actual)

    type IgnoreField =
        {
            unignoredX: int
            [<JsonIgnore>] ignoredY: string
        }

    [<Fact>]
    let ``deserialize with JsonIgnore`` () =
        let actual = JsonSerializer.Deserialize("""{"unignoredX":1,"ignoredY":"test"}""", options)
        Assert.Equal({unignoredX = 1; ignoredY = null}, actual)

    [<Fact>]
    let ``serialize with JsonIgnore`` () =
        let actual = JsonSerializer.Serialize({unignoredX = 1; ignoredY = "b"}, options)
        Assert.Equal("""{"unignoredX":1}""", actual)

    let ignoreNullOptions = JsonSerializerOptions(IgnoreNullValues = true)
    ignoreNullOptions.Converters.Add(JsonFSharpConverter())

    [<AllowNullLiteral>]
    type Cls() = class end

    type RecordWithNullableField =
        {
            cls: Cls
            y: int
        }

    [<Fact>]
    let ``deserialize with IgnoreNullValues`` () =
        let actual = JsonSerializer.Deserialize("""{"y":1}""", ignoreNullOptions)
        Assert.Equal({cls = null; y = 1}, actual)

    [<Fact>]
    let ``serialize with IgnoreNullValues`` () =
        let actual = JsonSerializer.Serialize({cls = null; y = 1}, ignoreNullOptions)
        Assert.Equal("""{"y":1}""", actual)

    let propertyNamingPolicyOptions = JsonSerializerOptions(PropertyNamingPolicy = JsonNamingPolicy.CamelCase)
    propertyNamingPolicyOptions.Converters.Add(JsonFSharpConverter())

    type CamelCase =
        {
            CcFirst: int
            CcSecond: string
        }

    [<Fact>]
    let ``deserialize with property naming policy`` () =
        let actual = JsonSerializer.Deserialize("""{"ccFirst":1,"ccSecond":"a"}""", propertyNamingPolicyOptions)
        Assert.Equal({CcFirst = 1; CcSecond = "a"}, actual)

    [<Fact>]
    let ``serialize with property naming policy`` () =
        let actual = JsonSerializer.Serialize({CcFirst = 1; CcSecond = "a"}, propertyNamingPolicyOptions)
        Assert.Equal("""{"ccFirst":1,"ccSecond":"a"}""", actual)

    let propertyNameCaseInsensitiveOptions = JsonSerializerOptions(PropertyNameCaseInsensitive = true)
    propertyNameCaseInsensitiveOptions.Converters.Add(JsonFSharpConverter())

    [<Fact>]
    let ``deserialize with property case insensitive`` () =
        let actual = JsonSerializer.Deserialize("""{"ccfIRst":1,"cCsEcOnD":"a"}""", propertyNameCaseInsensitiveOptions)
        Assert.Equal({CcFirst = 1; CcSecond = "a"}, actual)

    [<Fact>]
    let ``serialize with property case insensitive`` () =
        let actual = JsonSerializer.Serialize({CcFirst = 1; CcSecond = "a"}, propertyNameCaseInsensitiveOptions)
        Assert.Equal("""{"CcFirst":1,"CcSecond":"a"}""", actual)

module Struct =

    [<Struct; JsonFSharpConverter>]
    type A =
        {
            ax: int
            ay: string
        }

    [<Fact>]
    let ``deserialize via explicit converter`` () =
        let actual = JsonSerializer.Deserialize """{"ax":1,"ay":"b"}"""
        Assert.Equal({ax=1;ay="b"}, actual)

    [<Fact>]
    let ``serialize via explicit converter`` () =
        let actual = JsonSerializer.Serialize {ax=1;ay="b"}
        Assert.Equal("""{"ax":1,"ay":"b"}""", actual)

    [<Struct>]
    type B =
        {
            bx: int
            by: string
        }

    let options = JsonSerializerOptions()
    options.Converters.Add(JsonFSharpConverter())

    [<Fact>]
    let ``deserialize via options`` () =
        let actual = JsonSerializer.Deserialize("""{"bx":1,"by":"b"}""", options)
        Assert.Equal({bx=1;by="b"}, actual)

    [<Fact>]
    let ``serialize via options`` () =
        let actual = JsonSerializer.Serialize({bx=1;by="b"}, options)
        Assert.Equal("""{"bx":1,"by":"b"}""", actual)

    [<Fact>]
    let ``not fill in nulls`` () =
        try
            JsonSerializer.Deserialize<B>("""{"bx": 1, "by": null}""", options) |> ignore
            failwith "Deserialization was supposed to fail on the line above"
        with
        | :? JsonException as e -> Assert.Equal("B.by was expected to be of type String, but was null.", e.Message)

    [<Fact>]
    let ``get a good error message when omitting non-optional fields in a record`` () =
        try
            JsonSerializer.Deserialize<B>("""{"bx": 1}""", options) |> ignore
            failwith "Deserialization was supposed to fail on the line above"
        with
        | :? JsonException as e -> Assert.Equal("Missing field for record type Tests.Record+Struct+B: by", e.Message)

    [<Struct>]
    type SomeSearchApi =
        {
            filter: string option
            limit: int option
            offset: int option
        }

    let ``allow omitting fields that are optional`` () =
        let result  = JsonSerializer.Deserialize<SomeSearchApi>("""{}""", options)
        Assert.Equal(result, {filter=None; limit=None; offset=None})
        let result = JsonSerializer.Deserialize<SomeSearchApi>("""{"limit": 50}""", options)
        Assert.Equal(result, {filter=None; limit=Some 50; offset=None})

    [<Fact>]
    let ``allowNullFields`` () =
        let options = JsonSerializerOptions()
        options.Converters.Add(JsonFSharpConverter(allowNullFields = true))
        let actual = JsonSerializer.Deserialize("""{"bx":1,"by":null}""", options)
        Assert.Equal({bx=1;by=null}, actual)

    [<Struct>]
    type S =
        {
            sa: int
            sb: Skippable<int>
            sc: Skippable<int option>
            sd: Skippable<int voption>
        }

    [<Fact>]
    let ``deserialize skippable field`` () =
        let actual = JsonSerializer.Deserialize("""{"sa":1}""", options)
        Assert.Equal({sa=1;sb=Skip;sc=Skip;sd=Skip}, actual)
        let actual = JsonSerializer.Deserialize("""{"sa":1,"sb":2,"sc":null,"sd":null}""", options)
        Assert.Equal({sa=1;sb=Include 2;sc=Include None;sd=Include ValueNone}, actual)
        let actual = JsonSerializer.Deserialize("""{"sa":1,"sb":2,"sc":3,"sd":4}""", options)
        Assert.Equal({sa=1;sb=Include 2;sc=Include(Some 3);sd=Include(ValueSome 4)}, actual)

    [<Fact>]
    let ``serialize skippable field`` () =
        let actual = JsonSerializer.Serialize({sa=1;sb=Skip;sc=Skip;sd=Skip}, options)
        Assert.Equal("""{"sa":1}""", actual)
        let actual = JsonSerializer.Serialize({sa=1;sb=Include 2;sc=Include None;sd=Include ValueNone}, options)
        Assert.Equal("""{"sa":1,"sb":2,"sc":null,"sd":null}""", actual)
        let actual = JsonSerializer.Serialize({sa=1;sb=Include 2;sc=Include(Some 3);sd=Include(ValueSome 4)}, options)
        Assert.Equal("""{"sa":1,"sb":2,"sc":3,"sd":4}""", actual)

    [<Struct>]
    type C =
        {
            cx: B
        }

    [<Fact>]
    let ``deserialize nested`` () =
        let actual = JsonSerializer.Deserialize("""{"cx":{"bx":1,"by":"b"}}""", options)
        Assert.Equal({cx={bx=1;by="b"}}, actual)

    [<Fact>]
    let ``deserialize anonymous`` () =
        let actual = JsonSerializer.Deserialize("""{"x":1,"y":"b"}""", options)
        Assert.Equal(struct {|x=1;y="b"|}, actual)

    [<Fact>]
    let ``serialize anonymous`` () =
        let actual = JsonSerializer.Serialize(struct {|x=1;y="b"|}, options)
        Assert.Equal("""{"x":1,"y":"b"}""", actual)

    [<Struct>]
    type PropName =
        {
            unnamedX: int
            [<JsonPropertyName "namedY">] unnamedY: string
        }

    [<Fact>]
    let ``deserialize with JsonPropertyName`` () =
        let actual = JsonSerializer.Deserialize("""{"unnamedX":1,"namedY":"b"}""", options)
        Assert.Equal({unnamedX = 1; unnamedY = "b"}, actual)

    [<Fact>]
    let ``serialize with JsonPropertyName`` () =
        let actual = JsonSerializer.Serialize({unnamedX = 1;unnamedY = "b"}, options)
        Assert.Equal("""{"unnamedX":1,"namedY":"b"}""", actual)

    [<Struct>]
    type IgnoreField =
        {
            unignoredX: int
            [<JsonIgnore>] ignoredY: string
        }

    [<Fact>]
    let ``deserialize with JsonIgnore`` () =
        let actual = JsonSerializer.Deserialize("""{"unignoredX":1,"ignoredY":"test"}""", options)
        Assert.Equal({unignoredX = 1; ignoredY = null}, actual)

    [<Fact>]
    let ``serialize with JsonIgnore`` () =
        let actual = JsonSerializer.Serialize({unignoredX = 1; ignoredY = "b"}, options)
        Assert.Equal("""{"unignoredX":1}""", actual)

    let ignoreNullOptions = JsonSerializerOptions(IgnoreNullValues = true)
    ignoreNullOptions.Converters.Add(JsonFSharpConverter())

    [<AllowNullLiteral>]
    type Cls() = class end

    [<Struct>]
    type RecordWithNullableField =
        {
            cls: Cls
            y: int
        }

    [<Fact>]
    let ``deserialize with IgnoreNullValues`` () =
        let actual = JsonSerializer.Deserialize("""{"y":1}""", ignoreNullOptions)
        Assert.Equal({cls = null; y = 1}, actual)

    [<Fact>]
    let ``serialize with IgnoreNullValues`` () =
        let actual = JsonSerializer.Serialize({cls = null; y = 1}, ignoreNullOptions)
        Assert.Equal("""{"y":1}""", actual)

    let propertyNamingPolicyOptions = JsonSerializerOptions(PropertyNamingPolicy = JsonNamingPolicy.CamelCase)
    propertyNamingPolicyOptions.Converters.Add(JsonFSharpConverter())

    [<Struct>]
    type CamelCase =
        {
            CcFirst: int
            CcSecond: string
        }

    [<Fact>]
    let ``deserialize with property naming policy`` () =
        let actual = JsonSerializer.Deserialize("""{"ccFirst":1,"ccSecond":"a"}""", propertyNamingPolicyOptions)
        Assert.Equal({CcFirst = 1; CcSecond = "a"}, actual)

    [<Fact>]
    let ``serialize with property naming policy`` () =
        let actual = JsonSerializer.Serialize({CcFirst = 1; CcSecond = "a"}, propertyNamingPolicyOptions)
        Assert.Equal("""{"ccFirst":1,"ccSecond":"a"}""", actual)

    let propertyNameCaseInsensitiveOptions = JsonSerializerOptions(PropertyNameCaseInsensitive = true)
    propertyNameCaseInsensitiveOptions.Converters.Add(JsonFSharpConverter())

    [<Fact>]
    let ``deserialize with property case insensitive`` () =
        let actual = JsonSerializer.Deserialize("""{"ccfIRst":1,"cCsEcOnD":"a"}""", propertyNameCaseInsensitiveOptions)
        Assert.Equal({CcFirst = 1; CcSecond = "a"}, actual)

    [<Fact>]
    let ``serialize with property case insensitive`` () =
        let actual = JsonSerializer.Serialize({CcFirst = 1; CcSecond = "a"}, propertyNameCaseInsensitiveOptions)
        Assert.Equal("""{"CcFirst":1,"CcSecond":"a"}""", actual)
