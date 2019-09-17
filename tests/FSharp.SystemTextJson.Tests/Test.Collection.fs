module Tests.Collection

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

[<Property>]
let ``deserialize set of ints`` (s: Set<int>) =
    let ser = "[" + String.concat "," (Seq.map string s) + "]"
    let actual = JsonSerializer.Deserialize<Set<int>>(ser, options)
    Assert.Equal<Set<int>>(s, actual)

[<Property>]
let ``serialize set of ints`` (s: Set<int>) =
    let expected = "[" + String.concat "," (Seq.map string s) + "]"
    let actual = JsonSerializer.Serialize(s, options)
    Assert.Equal(expected, actual)
