module Tests.List

open System.Text.Json.Serialization
open System.Text.Json
open Xunit
open FsCheck.Xunit

let options = JsonSerializerOptions()
options.Converters.Add(JsonFSharpConverter())

[<Property>]
let ``deserialize list of ints`` (l: list<int>) =
    let ser = "[" + String.concat "," (List.map string l) + "]"
    let actual = JsonSerializer.Deserialize<list<int>>(ser, options)
    Assert.Equal<list<int>>(l, actual)

[<Property>]
let ``serialize list of ints`` (l: list<int>) =
    let expected = "[" + String.concat "," (List.map string l) + "]"
    let actual = JsonSerializer.Serialize(l, options)
    Assert.Equal(expected, actual)
