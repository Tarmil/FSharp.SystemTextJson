module Tests.Collection

open System.Text.Json.Serialization
open System.Text.Json
open Xunit
open FsCheck
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

let serKV1 (KeyValue (k: string, v: int)) =
    JsonSerializer.Serialize(k) + ":" + JsonSerializer.Serialize(v)

[<Property>]
let ``deserialize string-keyed map`` (m: Map<NonNull<string>, int>) =
    let m = (Map.empty, m) ||> Map.fold (fun m (NonNull k) v -> Map.add k v m)
    let ser = "{" + String.concat "," (Seq.map serKV1 m) + "}"
    let actual = JsonSerializer.Deserialize<Map<string, int>>(ser, options)
    Assert.Equal<Map<string, int>>(m, actual)

[<Property>]
let ``serialize string-keyed map`` (m: Map<NonNull<string>, int>) =
    let m = (Map.empty, m) ||> Map.fold (fun m (NonNull k) v -> Map.add k v m)
    let expected = "{" + String.concat "," (Seq.map serKV1 m) + "}"
    let actual = JsonSerializer.Serialize(m, options)
    Assert.Equal(expected, actual)

let serKV2 (KeyValue (k: int, v: string)) =
    "[" + JsonSerializer.Serialize(k) + "," + JsonSerializer.Serialize(v) + "]"

[<Property>]
let ``deserialize int-keyed map`` (m: Map<int, string>) =
    let ser = "[" + String.concat "," (Seq.map serKV2 m) + "]"
    let actual = JsonSerializer.Deserialize<Map<int, string>>(ser, options)
    Assert.Equal<Map<int, string>>(m, actual)

[<Property>]
let ``serialize int-keyed map`` (m: Map<int, string>) =
    let expected = "[" + String.concat "," (Seq.map serKV2 m) + "]"
    let actual = JsonSerializer.Serialize(m, options)
    Assert.Equal(expected, actual)
