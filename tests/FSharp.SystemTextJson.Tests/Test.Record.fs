module Tests.Record

open Xunit
open System.Text.Json.Serialization
open System.Text.Json

module NonStruct =

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
    options.Converters.Add(JsonRecordConverter())

    [<Fact>]
    let ``deserialize via options`` () =
        let actual = JsonSerializer.Deserialize("""{"bx":1,"by":"b"}""", options)
        Assert.Equal({bx=1;by="b"}, actual)

    [<Fact>]
    let ``serialize via options`` () =
        let actual = JsonSerializer.Serialize({bx=1;by="b"}, options)
        Assert.Equal("""{"bx":1,"by":"b"}""", actual)

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
