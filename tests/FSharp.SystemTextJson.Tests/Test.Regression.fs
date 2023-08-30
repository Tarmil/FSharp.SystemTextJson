module Tests.Regression

open Xunit
open System.Text.Json.Serialization
open System.Text.Json

type Color =
    | Red
    | Blue
    | Green

[<Fact>]
let ``regression #33`` () =
    let serializerOptions = JsonSerializerOptions()
    serializerOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.UnwrapFieldlessTags))
    let actual =
        JsonSerializer.Deserialize<Color list>("""[ "Red", "Blue"] """, serializerOptions)
    Assert.Equal<Color>([ Red; Blue ], actual)
    let actual =
        JsonSerializer.Deserialize("""{"a":"Red","b":"Blue"}""", serializerOptions)
    Assert.Equal({| a = Red; b = Blue |}, actual)

type A = { A: int; B: string }

[<Fact>]
let ``regression #87`` () =
    let options = JsonSerializerOptions()
    options.Converters.Add(JsonFSharpConverter())
    let ex1 =
        Assert.Throws<JsonException>(fun () ->
            JsonSerializer.Deserialize<A>("""{ "A": 2, "B": null }""", options) |> ignore
        )
    Assert.Equal("A.B was expected to be of type String, but was null.", ex1.Message)
    let ex2 =
        Assert.Throws<JsonException>(fun () ->
            JsonSerializer.Deserialize<A>("""{ "A": null, "B": "a" }""", options) |> ignore
        )
    Assert.Equal("A.A was expected to be of type Int32, but was null.", ex2.Message)

type R100Rec1 = { Case: string }
type R100Rec2 = { Case: string; other: int }

type R100U =
    | Rec1 of R100Rec1
    | Rec2 of R100Rec2

[<Fact>]
let ``regression #100`` () =
    let options = JsonSerializerOptions()
    options.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.InternalTag ||| JsonUnionEncoding.UnwrapRecordCases))
    Assert.Equal(Rec1 { Case = "Rec1" }, JsonSerializer.Deserialize("""{"Case":"Rec1"}""", options))
    Assert.Equal(
        Rec2 { Case = "Rec2"; other = 42 },
        JsonSerializer.Deserialize("""{"Case":"Rec2","other":42}""", options)
    )

type MyRecord = { MyUnion: MyUnion }

and MyUnion = MyUnion of int voption

[<Fact>]
let ``regression #106`` () =
    let options = JsonSerializerOptions()
    options.Converters.Add(JsonFSharpConverter())
    Assert.Equal({ MyUnion = MyUnion ValueNone }, JsonSerializer.Deserialize("""{"MyUnion":null}""", options))

type Person = { FirstName: string; LastName: string option; age: int }

[<Fact>]
let ``regression #123`` () =
    let fsOptions =
        JsonFSharpConverter(
            JsonUnionEncoding.UnwrapSingleCaseUnions
            ||| JsonUnionEncoding.UnwrapOption
            ||| JsonUnionEncoding.NamedFields
            ||| JsonUnionEncoding.UnwrapFieldlessTags
        )

    let noSkipOptions = JsonSerializerOptions()
    noSkipOptions.Converters.Add(fsOptions)
    let ex =
        Assert.Throws<JsonException>(fun () ->
            JsonSerializer.Deserialize<Person>("""{"FirstName": "yarr", "age": 5 }""", noSkipOptions)
            |> ignore
        )
    Assert.Equal("Missing field for record type Tests.Regression+Person: LastName", ex.Message)

    let skipOptions =
        JsonSerializerOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)
    skipOptions.Converters.Add(fsOptions)
    Assert.Equal(
        { FirstName = "yarr"; LastName = None; age = 5 },
        JsonSerializer.Deserialize<Person>("""{"FirstName": "yarr", "age": 5 }""", skipOptions)
    )

    let skipOptions2 = JsonSerializerOptions(IgnoreNullValues = true)
    skipOptions2.Converters.Add(fsOptions)
    Assert.Equal(
        { FirstName = "yarr"; LastName = None; age = 5 },
        JsonSerializer.Deserialize<Person>("""{"FirstName": "yarr", "age": 5 }""", skipOptions2)
    )

type R = { x: int option }
type RV = { x: int voption }

[<Fact>]
let ``regression #154`` () =
    let o =
        JsonFSharpOptions().WithSkippableOptionFields(false).ToJsonSerializerOptions()
    Assert.Throws<JsonException>(fun () -> JsonSerializer.Deserialize<R>("{}", o) |> ignore)
    |> ignore
    Assert.Throws<JsonException>(fun () -> JsonSerializer.Deserialize<RV>("{}", o) |> ignore)
    |> ignore

type X = { X: X }
type UY = UY of int * int
type Y = { Y: UY }

[<Fact>]
let ``regression #172`` () =
    let x = { X = Unchecked.defaultof<_> }
    let y = { Y = Unchecked.defaultof<_> }
    let options = JsonSerializerOptions()
    options.Converters.Add(JsonFSharpConverter())
    Assert.Equal("{\"X\":null}", JsonSerializer.Serialize(x, options))
    Assert.Equal("{\"Y\":null}", JsonSerializer.Serialize(y, options))
