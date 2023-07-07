module Tests.Record

open Xunit
open System.Text.Json.Serialization
open System.Text.Json

module NonStruct =

    [<JsonFSharpConverter>]
    type A = { ax: int; ay: string }

    [<Fact>]
    let ``deserialize via explicit converter`` () =
        let actual = JsonSerializer.Deserialize """{"ax":1,"ay":"b"}"""
        Assert.Equal({ ax = 1; ay = "b" }, actual)

    [<Fact>]
    let ``deserialize empty record with ignore-null-values on`` () =
        let options = JsonSerializerOptions(DefaultIgnoreCondition = Serialization.JsonIgnoreCondition.WhenWritingNull)
        try
            JsonSerializer.Deserialize<A>("{}", options) |> ignore
        with
            | :? System.NullReferenceException -> failwith "Unexpected NRE."
            | ex when ex.Message.Contains("Missing field for record type") -> () // It's expected to fail since the record requires its fields to be initialized.

    [<Fact>]
    let ``serialize via explicit converter`` () =
        let actual = JsonSerializer.Serialize { ax = 1; ay = "b" }
        Assert.Equal("""{"ax":1,"ay":"b"}""", actual)

    type B = { bx: int; by: string }

    let options = JsonFSharpOptions().ToJsonSerializerOptions()

    [<Fact>]
    let ``deserialize via options`` () =
        let actual = JsonSerializer.Deserialize("""{"bx":1,"by":"b"}""", options)
        Assert.Equal({ bx = 1; by = "b" }, actual)

    [<Fact>]
    let ``serialize via options`` () =
        let actual = JsonSerializer.Serialize({ bx = 1; by = "b" }, options)
        Assert.Equal("""{"bx":1,"by":"b"}""", actual)

    [<Fact>]
    let ``disallow null records`` () =
        Assert.Throws<JsonException>(fun () -> JsonSerializer.Deserialize<B>("null", options) |> ignore)
        |> ignore

    type internal Internal = { ix: int }

    [<Fact>]
    let ``serialize non-public record`` () =
        let actual = JsonSerializer.Serialize({ ix = 1 }, options)
        Assert.Equal("""{"ix":1}""", actual)

    [<Fact>]
    let ``deserialize non-public record`` () =
        let actual = JsonSerializer.Deserialize<Internal>("""{"ix":1}""", options)
        Assert.Equal({ ix = 1 }, actual)

    type PrivateFields = private { px: int }

    [<Fact>]
    let ``serialize record with private fields`` () =
        let actual = JsonSerializer.Serialize({ px = 1 }, options)
        Assert.Equal("""{"px":1}""", actual)

    [<Fact>]
    let ``deserialize record with private fields`` () =
        let actual = JsonSerializer.Deserialize<PrivateFields>("""{"px":1}""", options)
        Assert.Equal({ px = 1 }, actual)

    [<Fact>]
    let ``not fill in nulls`` () =
        try
            JsonSerializer.Deserialize<B>("""{"bx": 1, "by": null}""", options) |> ignore
            failwith "Deserialization was supposed to fail on the line above"
        with :? JsonException as e ->
            Assert.Equal("B.by was expected to be of type String, but was null.", e.Message)

    [<Fact>]
    let ``get a good error message when omitting non-optional fields in a record`` () =
        try
            JsonSerializer.Deserialize<B>("""{"bx": 1}""", options) |> ignore
            failwith "Deserialization was supposed to fail on the line above"
        with :? JsonException as e ->
            Assert.Equal("Missing field for record type Tests.Record+NonStruct+B: by", e.Message)

    type SomeSearchApi =
        { filter: string voption
          limit: int option
          offset: int option }

    [<Fact>]
    let ``dont allow omitting fields that are optional`` () =
        Assert.Throws<JsonException>(fun () -> JsonSerializer.Deserialize<SomeSearchApi>("""{}""", options) |> ignore)
        |> ignore
        Assert.Throws<JsonException>(fun () ->
            JsonSerializer.Deserialize<SomeSearchApi>("""{"limit": 50}""", options)
            |> ignore
        )
        |> ignore

    [<Fact>]
    let allowNullFields () =
        let options = JsonFSharpOptions().WithAllowNullFields().ToJsonSerializerOptions()
        let actual = JsonSerializer.Deserialize("""{"bx":1,"by":null}""", options)
        Assert.Equal({ bx = 1; by = null }, actual)

    type Sk =
        { sa: int
          sb: Skippable<int>
          sc: Skippable<int option>
          sd: Skippable<int voption> }

    [<Fact>]
    let ``deserialize skippable field`` () =
        let actual = JsonSerializer.Deserialize("""{"sa":1}""", options)
        Assert.Equal({ sa = 1; sb = Skip; sc = Skip; sd = Skip }, actual)
        let actual =
            JsonSerializer.Deserialize("""{"sa":1,"sb":2,"sc":null,"sd":null}""", options)
        Assert.Equal(
            { sa = 1
              sb = Include 2
              sc = Include None
              sd = Include ValueNone },
            actual
        )
        let actual =
            JsonSerializer.Deserialize("""{"sa":1,"sb":2,"sc":3,"sd":4}""", options)
        Assert.Equal(
            { sa = 1
              sb = Include 2
              sc = Include(Some 3)
              sd = Include(ValueSome 4) },
            actual
        )

    [<Fact>]
    let ``serialize skippable field`` () =
        let actual =
            JsonSerializer.Serialize({ sa = 1; sb = Skip; sc = Skip; sd = Skip }, options)
        Assert.Equal("""{"sa":1}""", actual)
        let actual =
            JsonSerializer.Serialize(
                { sa = 1
                  sb = Include 2
                  sc = Include None
                  sd = Include ValueNone },
                options
            )
        Assert.Equal("""{"sa":1,"sb":2,"sc":null,"sd":null}""", actual)
        let actual =
            JsonSerializer.Serialize(
                { sa = 1
                  sb = Include 2
                  sc = Include(Some 3)
                  sd = Include(ValueSome 4) },
                options
            )
        Assert.Equal("""{"sa":1,"sb":2,"sc":3,"sd":4}""", actual)

    type SkO =
        { sa: int
          sb: int option
          sc: int option voption
          sd: int voption option }

    [<Fact>]
    let ``deserialize skippable union field`` () =
        let options =
            JsonFSharpOptions
                .Default()
                .WithSkippableOptionFields()
                .ToJsonSerializerOptions()
        let actual = JsonSerializer.Deserialize("""{"sa":1}""", options)
        Assert.Equal({ sa = 1; sb = None; sc = ValueNone; sd = None }, actual)
        let actual =
            JsonSerializer.Deserialize("""{"sa":1,"sb":2,"sc":null,"sd":null}""", options)
        Assert.Equal(
            { sa = 1
              sb = Some 2
              sc = ValueSome None
              sd = Some ValueNone },
            actual
        )
        let actual =
            JsonSerializer.Deserialize("""{"sa":1,"sb":2,"sc":3,"sd":4}""", options)
        Assert.Equal(
            { sa = 1
              sb = Some 2
              sc = ValueSome(Some 3)
              sd = Some(ValueSome 4) },
            actual
        )

    [<Fact>]
    let ``serialize skippable union field`` () =
        let options =
            JsonFSharpOptions
                .Default()
                .WithSkippableOptionFields()
                .ToJsonSerializerOptions()
        let actual =
            JsonSerializer.Serialize({ sa = 1; sb = None; sc = ValueNone; sd = None }, options)
        Assert.Equal("""{"sa":1}""", actual)
        let actual =
            JsonSerializer.Serialize(
                { sa = 1
                  sb = Some 2
                  sc = ValueSome None
                  sd = Some ValueNone },
                options
            )
        Assert.Equal("""{"sa":1,"sb":2,"sc":null,"sd":null}""", actual)
        let actual =
            JsonSerializer.Serialize(
                { sa = 1
                  sb = Some 2
                  sc = ValueSome(Some 3)
                  sd = Some(ValueSome 4) },
                options
            )
        Assert.Equal("""{"sa":1,"sb":2,"sc":3,"sd":4}""", actual)

    type C = { cx: B }

    [<Fact>]
    let ``deserialize nested`` () =
        let actual = JsonSerializer.Deserialize("""{"cx":{"bx":1,"by":"b"}}""", options)
        Assert.Equal({ cx = { bx = 1; by = "b" } }, actual)

    [<Fact>]
    let ``deserialize anonymous`` () =
        let actual = JsonSerializer.Deserialize("""{"x":1,"y":"b"}""", options)
        Assert.Equal({| x = 1; y = "b" |}, actual)

    [<Fact>]
    let ``serialize anonymous`` () =
        let actual = JsonSerializer.Serialize({| x = 1; y = "b" |}, options)
        Assert.Equal("""{"x":1,"y":"b"}""", actual)

    [<Fact>]
    let ``deserialize empty anonymous`` () =
        let actual = JsonSerializer.Deserialize("{}", options)
        Assert.Equal({|  |}, actual)

    [<Fact>]
    let ``serialize empty anonymous`` () =
        let actual = JsonSerializer.Serialize({|  |}, options)
        Assert.Equal("{}", actual)

    type PropName =
        { unnamedX: int
          [<JsonPropertyName "namedY">]
          unnamedY: string }

    [<Fact>]
    let ``deserialize with JsonPropertyName`` () =
        let actual = JsonSerializer.Deserialize("""{"unnamedX":1,"namedY":"b"}""", options)
        Assert.Equal({ unnamedX = 1; unnamedY = "b" }, actual)

    [<Fact>]
    let ``serialize with JsonPropertyName`` () =
        let actual = JsonSerializer.Serialize({ unnamedX = 1; unnamedY = "b" }, options)
        Assert.Equal("""{"unnamedX":1,"namedY":"b"}""", actual)

    type PropJsonName =
        { unnamedA: int
          [<JsonName("namedB", "namedB2")>]
          unnamedB: string }

    [<Fact>]
    let ``deserialize with JsonName`` () =
        let actual = JsonSerializer.Deserialize("""{"unnamedA":1,"namedB":"b"}""", options)
        Assert.Equal({ unnamedA = 1; unnamedB = "b" }, actual)
        let actual = JsonSerializer.Deserialize("""{"unnamedA":1,"namedB2":"b"}""", options)
        Assert.Equal({ unnamedA = 1; unnamedB = "b" }, actual)

    [<Fact>]
    let ``serialize with JsonName`` () =
        let actual = JsonSerializer.Serialize({ unnamedA = 1; unnamedB = "b" }, options)
        Assert.Equal("""{"unnamedA":1,"namedB":"b"}""", actual)

    type IgnoreField =
        { unignoredX: int
          [<JsonIgnore>]
          ignoredY: string }

    [<Fact>]
    let ``deserialize with JsonIgnore`` () =
        let actual =
            JsonSerializer.Deserialize("""{"unignoredX":1,"ignoredY":"test"}""", options)
        Assert.Equal({ unignoredX = 1; ignoredY = null }, actual)

    [<Fact>]
    let ``serialize with JsonIgnore`` () =
        let actual = JsonSerializer.Serialize({ unignoredX = 1; ignoredY = "b" }, options)
        Assert.Equal("""{"unignoredX":1}""", actual)

    let ignoreNullOptions =
        JsonFSharpOptions()
            .WithAllowNullFields()
            .ToJsonSerializerOptions(IgnoreNullValues = true)

    let newIgnoreNullOptions =
        JsonFSharpOptions()
            .WithAllowNullFields()
            .ToJsonSerializerOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)

    [<AllowNullLiteral>]
    type Cls() =
        class
        end

    type RecordWithNullableField = { cls: Cls; y: int }

    [<Fact>]
    let ``deserialize with IgnoreNullValues`` () =
        let actual = JsonSerializer.Deserialize("""{"y":1}""", ignoreNullOptions)
        Assert.Equal({ cls = null; y = 1 }, actual)

    [<Fact>]
    let ``serialize with IgnoreNullValues`` () =
        let actual = JsonSerializer.Serialize({ cls = null; y = 1 }, ignoreNullOptions)
        Assert.Equal("""{"y":1}""", actual)

    [<Fact>]
    let ``deserialize with JsonIgnoreCondition.WhenWritingNull`` () =
        let actual = JsonSerializer.Deserialize("""{"y":1}""", newIgnoreNullOptions)
        Assert.Equal({ cls = null; y = 1 }, actual)

    [<Fact>]
    let ``serialize with JsonIgnoreCondition.WhenWritingNull`` () =
        let actual = JsonSerializer.Serialize({ cls = null; y = 1 }, newIgnoreNullOptions)
        Assert.Equal("""{"y":1}""", actual)

    let propertyNamingPolicyOptions =
        JsonFSharpOptions()
            .ToJsonSerializerOptions(PropertyNamingPolicy = JsonNamingPolicy.CamelCase)

    type CamelCase = { CcFirst: int; CcSecond: string }

    [<Fact>]
    let ``deserialize with property naming policy`` () =
        let actual =
            JsonSerializer.Deserialize("""{"ccFirst":1,"ccSecond":"a"}""", propertyNamingPolicyOptions)
        Assert.Equal({ CcFirst = 1; CcSecond = "a" }, actual)

    [<Fact>]
    let ``serialize with property naming policy`` () =
        let actual =
            JsonSerializer.Serialize({ CcFirst = 1; CcSecond = "a" }, propertyNamingPolicyOptions)
        Assert.Equal("""{"ccFirst":1,"ccSecond":"a"}""", actual)

    let propertyNameCaseInsensitiveOptions =
        JsonFSharpOptions().ToJsonSerializerOptions(PropertyNameCaseInsensitive = true)

    [<Fact>]
    let ``deserialize with property case insensitive`` () =
        let actual =
            JsonSerializer.Deserialize("""{"ccfIRst":1,"cCsEcOnD":"a"}""", propertyNameCaseInsensitiveOptions)
        Assert.Equal({ CcFirst = 1; CcSecond = "a" }, actual)

    [<Fact>]
    let ``serialize with property case insensitive`` () =
        let actual =
            JsonSerializer.Serialize({ CcFirst = 1; CcSecond = "a" }, propertyNameCaseInsensitiveOptions)
        Assert.Equal("""{"CcFirst":1,"CcSecond":"a"}""", actual)

    type D = { dx: int; dy: int option }

    [<Fact>]
    let ``should fail if missing required field`` () =
        try
            JsonSerializer.Deserialize<D>("""{"dy": 1}""", options) |> ignore
            failwith "Deserialization was supposed to fail on the line above"
        with :? JsonException as e ->
            Assert.Equal("Missing field for record type Tests.Record+NonStruct+D: dx", e.Message)

    [<JsonFSharpConverter(AllowNullFields = false)>]
    type Override = { x: string }

    [<Fact>]
    let ``should not override allowNullFields in attribute if AllowOverride = false`` () =
        let o = JsonFSharpOptions().WithAllowNullFields().ToJsonSerializerOptions()
        Assert.Equal({ x = null }, JsonSerializer.Deserialize<Override>("""{"x":null}""", o))

    [<Fact>]
    let ``should override allowNullFields in attribute if AllowOverride = true`` () =
        let o =
            JsonFSharpOptions()
                .WithAllowNullFields()
                .WithAllowOverride()
                .ToJsonSerializerOptions()
        Assert.Throws<JsonException>(fun () -> JsonSerializer.Deserialize<Override>("""{"x":null}""", o) |> ignore)
        |> ignore

    type OrderedClass() =
        [<JsonPropertyOrder 2>]
        member val LastName = "" with get, set
        member val Country = "" with get, set
        member val City = "" with get, set
        [<JsonPropertyOrder -1>]
        member val Id = 0 with get, set
        [<JsonPropertyOrder 1>]
        member val FirstName = "" with get, set

    type Ordered =
        { [<JsonPropertyOrder 2>]
          LastName: string
          Country: string
          City: string
          [<JsonPropertyOrder -1>]
          Id: int
          [<JsonPropertyOrder 1>]
          FirstName: string }

    [<Fact>]
    let ``should respect JsonPropertyOrder`` () =
        let expected =
            OrderedClass(LastName = "Dupont", City = "Paris", Country = "France", Id = 123, FirstName = "Jean")
        let actual =
            { LastName = "Dupont"
              City = "Paris"
              Country = "France"
              Id = 123
              FirstName = "Jean" }
        Assert.Equal(JsonSerializer.Serialize(expected), JsonSerializer.Serialize(actual, options))

    type RecordWithReadOnlyMember =
        { CcFirst: int
          CcSecond: string }
        member _.Member = "b"
        [<JsonIgnore>]
        member _.IgnoredMember = "c"

    let includeRecordPropertiesOptions =
        JsonFSharpOptions()
            .WithIncludeRecordProperties()
            .ToJsonSerializerOptions(PropertyNameCaseInsensitive = true)

    let dontIncludeRecordPropertiesOptions =
        JsonFSharpOptions()
            .WithIncludeRecordProperties(false)
            .ToJsonSerializerOptions(PropertyNameCaseInsensitive = true)

    [<Fact>]
    let ``serialize record properties`` () =
        let actual =
            JsonSerializer.Serialize({ CcFirst = 1; CcSecond = "a" }, includeRecordPropertiesOptions)
        Assert.Equal("""{"CcFirst":1,"CcSecond":"a","Member":"b"}""", actual)

    [<Fact>]
    let ``don't serialize record properties`` () =
        let actual =
            JsonSerializer.Serialize({ CcFirst = 1; CcSecond = "a" }, dontIncludeRecordPropertiesOptions)
        Assert.Equal("""{"CcFirst":1,"CcSecond":"a"}""", actual)

    [<Fact>]
    let ``deserialize with includeRecordProperties`` () =
        let actual =
            JsonSerializer.Deserialize("""{"CcFirst":1,"CcSecond":"a"}""", includeRecordPropertiesOptions)
        Assert.Equal({ CcFirst = 1; CcSecond = "a" }, actual)

    type RecordWithInclude =
        { incX: int
          incY: string }

        [<JsonInclude>]
        member _.incZ = 42

        member _.incT = "t"

    [<Fact>]
    let ``serialize with JsonInclude property`` () =
        let actual =
            JsonSerializer.Serialize({ incX = 1; incY = "a" }, dontIncludeRecordPropertiesOptions)
        Assert.Equal("""{"incX":1,"incY":"a","incZ":42}""", actual)

module Struct =

    [<Struct; JsonFSharpConverter>]
    type A = { ax: int; ay: string }

    [<Fact>]
    let ``deserialize via explicit converter`` () =
        let actual = JsonSerializer.Deserialize """{"ax":1,"ay":"b"}"""
        Assert.Equal({ ax = 1; ay = "b" }, actual)

    [<Fact>]
    let ``serialize via explicit converter`` () =
        let actual = JsonSerializer.Serialize { ax = 1; ay = "b" }
        Assert.Equal("""{"ax":1,"ay":"b"}""", actual)

    [<Struct>]
    type B = { bx: int; by: string }

    let options = JsonFSharpOptions().ToJsonSerializerOptions()

    [<Fact>]
    let ``deserialize via options`` () =
        let actual = JsonSerializer.Deserialize("""{"bx":1,"by":"b"}""", options)
        Assert.Equal({ bx = 1; by = "b" }, actual)

    [<Fact>]
    let ``serialize via options`` () =
        let actual = JsonSerializer.Serialize({ bx = 1; by = "b" }, options)
        Assert.Equal("""{"bx":1,"by":"b"}""", actual)

    [<Fact>]
    let ``disallow null records`` () =
        Assert.Throws<JsonException>(fun () -> JsonSerializer.Deserialize<B>("null", options) |> ignore)
        |> ignore

    [<Struct>]
    type internal Internal = { ix: int }

    [<Fact>]
    let ``serialize non-public record`` () =
        let actual = JsonSerializer.Serialize({ ix = 1 }, options)
        Assert.Equal("""{"ix":1}""", actual)

    [<Fact>]
    let ``deserialize non-public record`` () =
        let actual = JsonSerializer.Deserialize<Internal>("""{"ix":1}""", options)
        Assert.Equal({ ix = 1 }, actual)

    [<Struct>]
    type PrivateFields = private { px: int }

    [<Fact>]
    let ``serialize record with private fields`` () =
        let actual = JsonSerializer.Serialize({ px = 1 }, options)
        Assert.Equal("""{"px":1}""", actual)

    [<Fact>]
    let ``deserialize record with private fields`` () =
        let actual = JsonSerializer.Deserialize<PrivateFields>("""{"px":1}""", options)
        Assert.Equal({ px = 1 }, actual)

    [<Fact>]
    let ``not fill in nulls`` () =
        try
            JsonSerializer.Deserialize<B>("""{"bx": 1, "by": null}""", options) |> ignore
            failwith "Deserialization was supposed to fail on the line above"
        with :? JsonException as e ->
            Assert.Equal("B.by was expected to be of type String, but was null.", e.Message)

    [<Fact>]
    let ``get a good error message when omitting non-optional fields in a record`` () =
        try
            JsonSerializer.Deserialize<B>("""{"bx": 1}""", options) |> ignore
            failwith "Deserialization was supposed to fail on the line above"
        with :? JsonException as e ->
            Assert.Equal("Missing field for record type Tests.Record+Struct+B: by", e.Message)

    [<Struct>]
    type SomeSearchApi =
        { filter: string voption
          limit: int option
          offset: int option }

    [<Fact>]
    let ``dont allow omitting fields that are optional`` () =
        Assert.Throws<JsonException>(fun () -> JsonSerializer.Deserialize<SomeSearchApi>("""{}""", options) |> ignore)
        |> ignore
        Assert.Throws<JsonException>(fun () ->
            JsonSerializer.Deserialize<SomeSearchApi>("""{"limit": 50}""", options)
            |> ignore
        )
        |> ignore

    [<Fact>]
    let allowNullFields () =
        let options = JsonFSharpOptions().WithAllowNullFields().ToJsonSerializerOptions()
        let actual = JsonSerializer.Deserialize("""{"bx":1,"by":null}""", options)
        Assert.Equal({ bx = 1; by = null }, actual)

    [<Struct>]
    type Sk =
        { sa: int
          sb: Skippable<int>
          sc: Skippable<int option>
          sd: Skippable<int voption> }

    [<Fact>]
    let ``deserialize skippable field`` () =
        let actual = JsonSerializer.Deserialize("""{"sa":1}""", options)
        Assert.Equal({ sa = 1; sb = Skip; sc = Skip; sd = Skip }, actual)
        let actual =
            JsonSerializer.Deserialize("""{"sa":1,"sb":2,"sc":null,"sd":null}""", options)
        Assert.Equal(
            { sa = 1
              sb = Include 2
              sc = Include None
              sd = Include ValueNone },
            actual
        )
        let actual =
            JsonSerializer.Deserialize("""{"sa":1,"sb":2,"sc":3,"sd":4}""", options)
        Assert.Equal(
            { sa = 1
              sb = Include 2
              sc = Include(Some 3)
              sd = Include(ValueSome 4) },
            actual
        )

    [<Fact>]
    let ``serialize skippable field`` () =
        let actual =
            JsonSerializer.Serialize({ sa = 1; sb = Skip; sc = Skip; sd = Skip }, options)
        Assert.Equal("""{"sa":1}""", actual)
        let actual =
            JsonSerializer.Serialize(
                { sa = 1
                  sb = Include 2
                  sc = Include None
                  sd = Include ValueNone },
                options
            )
        Assert.Equal("""{"sa":1,"sb":2,"sc":null,"sd":null}""", actual)
        let actual =
            JsonSerializer.Serialize(
                { sa = 1
                  sb = Include 2
                  sc = Include(Some 3)
                  sd = Include(ValueSome 4) },
                options
            )
        Assert.Equal("""{"sa":1,"sb":2,"sc":3,"sd":4}""", actual)

    [<Struct>]
    type SkO =
        { sa: int
          sb: int option
          sc: int option voption
          sd: int voption option }

    [<Fact>]
    let ``deserialize skippable union field`` () =
        let options =
            JsonFSharpOptions
                .Default()
                .WithSkippableOptionFields()
                .ToJsonSerializerOptions()
        let actual = JsonSerializer.Deserialize("""{"sa":1}""", options)
        Assert.Equal({ sa = 1; sb = None; sc = ValueNone; sd = None }, actual)
        let actual =
            JsonSerializer.Deserialize("""{"sa":1,"sb":2,"sc":null,"sd":null}""", options)
        Assert.Equal(
            { sa = 1
              sb = Some 2
              sc = ValueSome None
              sd = Some ValueNone },
            actual
        )
        let actual =
            JsonSerializer.Deserialize("""{"sa":1,"sb":2,"sc":3,"sd":4}""", options)
        Assert.Equal(
            { sa = 1
              sb = Some 2
              sc = ValueSome(Some 3)
              sd = Some(ValueSome 4) },
            actual
        )

    [<Fact>]
    let ``serialize skippable union field`` () =
        let options =
            JsonFSharpOptions
                .Default()
                .WithSkippableOptionFields()
                .ToJsonSerializerOptions()
        let actual =
            JsonSerializer.Serialize({ sa = 1; sb = None; sc = ValueNone; sd = None }, options)
        Assert.Equal("""{"sa":1}""", actual)
        let actual =
            JsonSerializer.Serialize(
                { sa = 1
                  sb = Some 2
                  sc = ValueSome None
                  sd = Some ValueNone },
                options
            )
        Assert.Equal("""{"sa":1,"sb":2,"sc":null,"sd":null}""", actual)
        let actual =
            JsonSerializer.Serialize(
                { sa = 1
                  sb = Some 2
                  sc = ValueSome(Some 3)
                  sd = Some(ValueSome 4) },
                options
            )
        Assert.Equal("""{"sa":1,"sb":2,"sc":3,"sd":4}""", actual)

    [<Struct>]
    type C = { cx: B }

    [<Fact>]
    let ``deserialize nested`` () =
        let actual = JsonSerializer.Deserialize("""{"cx":{"bx":1,"by":"b"}}""", options)
        Assert.Equal({ cx = { bx = 1; by = "b" } }, actual)

    [<Fact>]
    let ``deserialize anonymous`` () =
        let actual = JsonSerializer.Deserialize("""{"x":1,"y":"b"}""", options)
        Assert.Equal(struct {| x = 1; y = "b" |}, actual)

    [<Fact>]
    let ``serialize anonymous`` () =
        let actual = JsonSerializer.Serialize(struct {| x = 1; y = "b" |}, options)
        Assert.Equal("""{"x":1,"y":"b"}""", actual)

    [<Struct>]
    type PropName =
        { unnamedX: int
          [<JsonPropertyName "namedY">]
          unnamedY: string }

    [<Fact>]
    let ``deserialize with JsonPropertyName`` () =
        let actual = JsonSerializer.Deserialize("""{"unnamedX":1,"namedY":"b"}""", options)
        Assert.Equal({ unnamedX = 1; unnamedY = "b" }, actual)

    [<Fact>]
    let ``serialize with JsonPropertyName`` () =
        let actual = JsonSerializer.Serialize({ unnamedX = 1; unnamedY = "b" }, options)
        Assert.Equal("""{"unnamedX":1,"namedY":"b"}""", actual)

    [<Struct>]
    type PropJsonName =
        { unnamedA: int
          [<JsonName("namedB", "namedB2")>]
          unnamedB: string }

    [<Fact>]
    let ``deserialize with JsonName`` () =
        let actual = JsonSerializer.Deserialize("""{"unnamedA":1,"namedB":"b"}""", options)
        Assert.Equal({ unnamedA = 1; unnamedB = "b" }, actual)
        let actual = JsonSerializer.Deserialize("""{"unnamedA":1,"namedB2":"b"}""", options)
        Assert.Equal({ unnamedA = 1; unnamedB = "b" }, actual)

    [<Fact>]
    let ``serialize with JsonName`` () =
        let actual = JsonSerializer.Serialize({ unnamedA = 1; unnamedB = "b" }, options)
        Assert.Equal("""{"unnamedA":1,"namedB":"b"}""", actual)

    [<Struct>]
    type IgnoreField =
        { unignoredX: int
          [<JsonIgnore>]
          ignoredY: string }

    [<Fact>]
    let ``deserialize with JsonIgnore`` () =
        let actual =
            JsonSerializer.Deserialize("""{"unignoredX":1,"ignoredY":"test"}""", options)
        Assert.Equal({ unignoredX = 1; ignoredY = null }, actual)

    [<Fact>]
    let ``serialize with JsonIgnore`` () =
        let actual = JsonSerializer.Serialize({ unignoredX = 1; ignoredY = "b" }, options)
        Assert.Equal("""{"unignoredX":1}""", actual)

    let ignoreNullOptions =
        JsonFSharpOptions()
            .WithAllowNullFields()
            .ToJsonSerializerOptions(IgnoreNullValues = true)

    let newIgnoreNullOptions =
        JsonFSharpOptions()
            .WithAllowNullFields()
            .ToJsonSerializerOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)

    [<AllowNullLiteral>]
    type Cls() =
        class
        end

    [<Struct>]
    type RecordWithNullableField = { cls: Cls; y: int }

    [<Fact>]
    let ``deserialize with IgnoreNullValues`` () =
        let actual = JsonSerializer.Deserialize("""{"y":1}""", ignoreNullOptions)
        Assert.Equal({ cls = null; y = 1 }, actual)

    [<Fact>]
    let ``serialize with IgnoreNullValues`` () =
        let actual = JsonSerializer.Serialize({ cls = null; y = 1 }, ignoreNullOptions)
        Assert.Equal("""{"y":1}""", actual)

    [<Fact>]
    let ``deserialize with JsonIgnoreCondition.WhenWritingNull`` () =
        let actual = JsonSerializer.Deserialize("""{"y":1}""", newIgnoreNullOptions)
        Assert.Equal({ cls = null; y = 1 }, actual)

    [<Fact>]
    let ``serialize with JsonIgnoreCondition.WhenWritingNull`` () =
        let actual = JsonSerializer.Serialize({ cls = null; y = 1 }, newIgnoreNullOptions)
        Assert.Equal("""{"y":1}""", actual)

    let propertyNamingPolicyOptions =
        JsonFSharpOptions()
            .ToJsonSerializerOptions(PropertyNamingPolicy = JsonNamingPolicy.CamelCase)

    [<Struct>]
    type CamelCase = { CcFirst: int; CcSecond: string }

    [<Fact>]
    let ``deserialize with property naming policy`` () =
        let actual =
            JsonSerializer.Deserialize("""{"ccFirst":1,"ccSecond":"a"}""", propertyNamingPolicyOptions)
        Assert.Equal({ CcFirst = 1; CcSecond = "a" }, actual)

    [<Fact>]
    let ``serialize with property naming policy`` () =
        let actual =
            JsonSerializer.Serialize({ CcFirst = 1; CcSecond = "a" }, propertyNamingPolicyOptions)
        Assert.Equal("""{"ccFirst":1,"ccSecond":"a"}""", actual)

    let propertyNameCaseInsensitiveOptions =
        JsonFSharpOptions().ToJsonSerializerOptions(PropertyNameCaseInsensitive = true)

    [<Fact>]
    let ``deserialize with property case insensitive`` () =
        let actual =
            JsonSerializer.Deserialize("""{"ccfIRst":1,"cCsEcOnD":"a"}""", propertyNameCaseInsensitiveOptions)
        Assert.Equal({ CcFirst = 1; CcSecond = "a" }, actual)

    [<Fact>]
    let ``serialize with property case insensitive`` () =
        let actual =
            JsonSerializer.Serialize({ CcFirst = 1; CcSecond = "a" }, propertyNameCaseInsensitiveOptions)
        Assert.Equal("""{"CcFirst":1,"CcSecond":"a"}""", actual)

    [<JsonFSharpConverter(AllowNullFields = false)>]
    [<Struct>]
    type Override = { x: string }

    [<Fact>]
    let ``should not override allowNullFields in attribute if AllowOverride = false`` () =
        let o = JsonFSharpOptions().WithAllowNullFields().ToJsonSerializerOptions()
        Assert.Equal({ x = null }, JsonSerializer.Deserialize<Override>("""{"x":null}""", o))

    [<Fact>]
    let ``should override allowNullFields in attribute if AllowOverride = true`` () =
        let o =
            JsonFSharpOptions()
                .WithAllowNullFields()
                .WithAllowOverride()
                .ToJsonSerializerOptions()
        Assert.Throws<JsonException>(fun () -> JsonSerializer.Deserialize<Override>("""{"x":null}""", o) |> ignore)
        |> ignore

    [<Struct>]
    type RecordWithReadOnlyMember =
        { CcFirst: int
          CcSecond: string }
        member _.Member = "b"
        [<JsonIgnore>]
        member _.IgnoredMember = "c"

    let includeRecordPropertiesOptions =
        JsonFSharpOptions()
            .WithIncludeRecordProperties()
            .ToJsonSerializerOptions(PropertyNameCaseInsensitive = true)

    let dontIncludeRecordPropertiesOptions =
        JsonFSharpOptions()
            .WithIncludeRecordProperties(false)
            .ToJsonSerializerOptions(PropertyNameCaseInsensitive = true)

    [<Fact>]
    let ``serialize record properties`` () =
        let actual =
            JsonSerializer.Serialize({ CcFirst = 1; CcSecond = "a" }, includeRecordPropertiesOptions)
        Assert.Equal("""{"CcFirst":1,"CcSecond":"a","Member":"b"}""", actual)

    [<Fact>]
    let ``don't serialize record properties`` () =
        let actual =
            JsonSerializer.Serialize({ CcFirst = 1; CcSecond = "a" }, dontIncludeRecordPropertiesOptions)
        Assert.Equal("""{"CcFirst":1,"CcSecond":"a"}""", actual)

    [<Fact>]
    let ``deserialize with includeRecordProperties`` () =
        let actual =
            JsonSerializer.Deserialize("""{"CcFirst":1,"CcSecond":"a"}""", includeRecordPropertiesOptions)
        Assert.Equal({ CcFirst = 1; CcSecond = "a" }, actual)

    [<Struct>]
    type RecordWithInclude =
        { incX: int
          incY: string }

        [<JsonInclude>]
        member _.incZ = 42

        member _.incT = "t"

    [<Fact>]
    let ``serialize with JsonInclude property`` () =
        let actual =
            JsonSerializer.Serialize({ incX = 1; incY = "a" }, dontIncludeRecordPropertiesOptions)
        Assert.Equal("""{"incX":1,"incY":"a","incZ":42}""", actual)
