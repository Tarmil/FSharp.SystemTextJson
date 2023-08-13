module Tests.Collection

open System
open System.Collections.Generic
open System.Text.Json.Serialization
open System.Text.Json
open Xunit
open FsCheck
open FsCheck.Xunit

let options = JsonFSharpOptions().ToJsonSerializerOptions()

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

let tryblock thunk =
    try
        thunk () |> Ok
    with e ->
        Error e.Message

module NullsInsideList =
    [<Fact>]
    let ``allowed when the target type is a list of options`` () =
        let ser = "[0, null, 2, 3]"
        let actual = JsonSerializer.Deserialize<int option list>(ser, options)
        Assert.Equal<int option list>([ Some 0; None; Some 2; Some 3 ], actual)

    [<Fact>]
    let ``forbid nulls inside array when target type is not option`` () =
        let ser = """["hello", null]"""
        let actual =
            tryblock (fun _ -> JsonSerializer.Deserialize<string list>(ser, options))
        match actual with
        | Error msg -> Assert.Equal("Unexpected null inside array. Expected only elements of type String", msg)
        | _ -> failwith "expected failure"

    [<Fact>]
    let ``allow nulls inside array if allowNullFields is true `` () =
        let options = JsonFSharpOptions().WithAllowNullFields().ToJsonSerializerOptions()
        let ser = """["hello", null]"""
        let actual = JsonSerializer.Deserialize<string list>(ser, options)
        Assert.Equal<string list>([ "hello"; null ], actual)

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

type UserId = UserId of string

let serKV1_1 (KeyValue (UserId k, v: int)) =
    JsonSerializer.Serialize(k) + ":" + JsonSerializer.Serialize(v)

[<Property>]
let ``deserialize newtype-string-keyed map`` (m: Map<NonNull<string>, int>) =
    let m = (Map.empty, m) ||> Map.fold (fun m (NonNull k) v -> Map.add (UserId k) v m)
    let ser = "{" + String.concat "," (Seq.map serKV1_1 m) + "}"
    let actual = JsonSerializer.Deserialize<Map<UserId, int>>(ser, options)
    Assert.Equal<Map<UserId, int>>(m, actual)

[<Property>]
let ``serialize newtype-string-keyed map`` (m: Map<NonNull<string>, int>) =
    let m = (Map.empty, m) ||> Map.fold (fun m (NonNull k) v -> Map.add (UserId k) v m)
    let expected = "{" + String.concat "," (Seq.map serKV1_1 m) + "}"
    let actual = JsonSerializer.Serialize(m, options)
    Assert.Equal(expected, actual)

[<Property>]
let ``deserialize newtype-string-keyed dictionary`` (d': Dictionary<NonNull<string>, int>) =
    let d = Dictionary()
    for KeyValue (NonNull k, v) in d' do
        d[UserId k] <- v
    let ser = "{" + String.concat "," (Seq.map serKV1_1 d) + "}"
    let actual = JsonSerializer.Deserialize<Dictionary<UserId, int>>(ser, options)
    Assert.Equal<Dictionary<UserId, int>>(d, actual)

[<Property>]
let ``serialize newtype-string-keyed dictionary`` (d': Dictionary<NonNull<string>, int>) =
    let d = Dictionary()
    for KeyValue (NonNull k, v) in d' do
        d[UserId k] <- v
    let expected = "{" + String.concat "," (Seq.map serKV1_1 d) + "}"
    let actual = JsonSerializer.Serialize(d, options)
    Assert.Equal(expected, actual)

[<Struct>]
type SUserId = SUserId of string

let serKV1_2 (KeyValue (SUserId k, v: int)) =
    JsonSerializer.Serialize(k) + ":" + JsonSerializer.Serialize(v)

[<Property>]
let ``deserialize struct-newtype-string-keyed map`` (m: Map<NonNull<string>, int>) =
    let m = (Map.empty, m) ||> Map.fold (fun m (NonNull k) v -> Map.add (SUserId k) v m)
    let ser = "{" + String.concat "," (Seq.map serKV1_2 m) + "}"
    let actual = JsonSerializer.Deserialize<Map<SUserId, int>>(ser, options)
    Assert.Equal<Map<SUserId, int>>(m, actual)

[<Property>]
let ``serialize struct-newtype-string-keyed map`` (m: Map<NonNull<string>, int>) =
    let m = (Map.empty, m) ||> Map.fold (fun m (NonNull k) v -> Map.add (SUserId k) v m)
    let expected = "{" + String.concat "," (Seq.map serKV1_2 m) + "}"
    let actual = JsonSerializer.Serialize(m, options)
    Assert.Equal(expected, actual)

[<Property>]
let ``deserialize struct-newtype-string-keyed dictionary`` (d': Dictionary<NonNull<string>, int>) =
    let d = Dictionary()
    for KeyValue (NonNull k, v) in d' do
        d[SUserId k] <- v
    let ser = "{" + String.concat "," (Seq.map serKV1_2 d) + "}"
    let actual = JsonSerializer.Deserialize<Dictionary<SUserId, int>>(ser, options)
    Assert.Equal<Dictionary<SUserId, int>>(d, actual)

[<Property>]
let ``serialize struct-newtype-string-keyed dictionary`` (d': Dictionary<NonNull<string>, int>) =
    let d = Dictionary()
    for KeyValue (NonNull k, v) in d' do
        d[SUserId k] <- v
    let expected = "{" + String.concat "," (Seq.map serKV1_2 d) + "}"
    let actual = JsonSerializer.Serialize(d, options)
    Assert.Equal(expected, actual)

let keyPolicyOptions =
    JsonFSharpOptions()
        .ToJsonSerializerOptions(DictionaryKeyPolicy = JsonNamingPolicy.CamelCase)

[<Property>]
let ``deserialize string-keyed map with key policy`` (m: Map<NonNull<string>, int>) =
    let m = (Map.empty, m) ||> Map.fold (fun m (NonNull k) v -> Map.add k v m)
    let ser = "{" + String.concat "," (Seq.map serKV1 m) + "}"
    let actual = JsonSerializer.Deserialize<Map<string, int>>(ser, keyPolicyOptions)
    Assert.Equal<Map<string, int>>(m, actual)

[<Property>]
let ``serialize string-keyed map with key policy`` (m: Map<NonNull<string>, int>) =
    let m = (Map.empty, m) ||> Map.fold (fun m (NonNull k) v -> Map.add k v m)
    let ccm =
        m
        |> Seq.map (fun (KeyValue (k, v)) -> KeyValuePair(JsonNamingPolicy.CamelCase.ConvertName k, v))
    let expected = "{" + String.concat "," (Seq.map serKV1 ccm) + "}"
    let actual = JsonSerializer.Serialize(m, keyPolicyOptions)
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

let objectOptions =
    JsonFSharpOptions().WithMapFormat(MapFormat.Object).ToJsonSerializerOptions()

[<Property>]
let ``deserialize string-keyed map with Object`` (m: Map<NonNull<string>, int>) =
    let m = (Map.empty, m) ||> Map.fold (fun m (NonNull k) v -> Map.add k v m)
    let ser =
        m
        |> Seq.map (fun (KeyValue (k, v)) -> $"{JsonSerializer.Serialize(k)}:{v}")
        |> String.concat ","
        |> sprintf "{%s}"
    let actual = JsonSerializer.Deserialize<Map<string, int>>(ser, objectOptions)
    Assert.Equal<Map<string, int>>(m, actual)

[<Property>]
let ``serialize string-keyed map with Object`` (m: Map<NonNull<string>, int>) =
    let m = (Map.empty, m) ||> Map.fold (fun m (NonNull k) v -> Map.add k v m)
    let expected =
        m
        |> Seq.map (fun (KeyValue (k, v)) -> $"{JsonSerializer.Serialize(k)}:{v}")
        |> String.concat ","
        |> sprintf "{%s}"
    let actual = JsonSerializer.Serialize(m, objectOptions)
    Assert.Equal(expected, actual)

[<Property>]
let ``deserialize newtype-string-keyed map with Object`` (m: Map<NonNull<string>, int>) =
    let m = (Map.empty, m) ||> Map.fold (fun m (NonNull k) v -> Map.add (UserId k) v m)
    let ser =
        m
        |> Seq.map (fun (KeyValue (UserId k, v)) -> $"{JsonSerializer.Serialize(k)}:{v}")
        |> String.concat ","
        |> sprintf "{%s}"
    let actual = JsonSerializer.Deserialize<Map<UserId, int>>(ser, objectOptions)
    Assert.Equal<Map<UserId, int>>(m, actual)

[<Property>]
let ``serialize newtype-string-keyed map with Object`` (m: Map<NonNull<string>, int>) =
    let m = (Map.empty, m) ||> Map.fold (fun m (NonNull k) v -> Map.add (UserId k) v m)
    let expected =
        m
        |> Seq.map (fun (KeyValue (UserId k, v)) -> $"{JsonSerializer.Serialize(k)}:{v}")
        |> String.concat ","
        |> sprintf "{%s}"
    let actual = JsonSerializer.Serialize(m, objectOptions)
    Assert.Equal(expected, actual)

[<Property>]
let ``deserialize Guid-keyed map with Object`` (m: Map<Guid, int>) =
    let ser =
        m
        |> Seq.map (fun (KeyValue (k, v)) -> $"\"{k}\":{v}")
        |> String.concat ","
        |> sprintf "{%s}"
    let actual = JsonSerializer.Deserialize<Map<Guid, int>>(ser, objectOptions)
    Assert.Equal<Map<Guid, int>>(m, actual)

[<Property>]
let ``serialize Guid-keyed map with Object`` (m: Map<Guid, int>) =
    let expected =
        m
        |> Seq.map (fun (KeyValue (k, v)) -> $"\"{k}\":{v}")
        |> String.concat ","
        |> sprintf "{%s}"
    let actual = JsonSerializer.Serialize(m, objectOptions)
    Assert.Equal(expected, actual)

let arrayOfPairsOptions =
    JsonFSharpOptions()
        .WithMapFormat(MapFormat.ArrayOfPairs)
        .ToJsonSerializerOptions()

[<Property>]
let ``deserialize string-keyed map with ArrayOfPairs`` (m: Map<NonNull<string>, int>) =
    let m = (Map.empty, m) ||> Map.fold (fun m (NonNull k) v -> Map.add k v m)
    let ser =
        m
        |> Seq.map (fun (KeyValue (k, v)) -> $"[{JsonSerializer.Serialize(k)},{v}]")
        |> String.concat ","
        |> sprintf "[%s]"
    let actual = JsonSerializer.Deserialize<Map<string, int>>(ser, arrayOfPairsOptions)
    Assert.Equal<Map<string, int>>(m, actual)

[<Property>]
let ``serialize string-keyed map with ArrayOfPairs`` (m: Map<NonNull<string>, int>) =
    let m = (Map.empty, m) ||> Map.fold (fun m (NonNull k) v -> Map.add k v m)
    let expected =
        m
        |> Seq.map (fun (KeyValue (k, v)) -> $"[{JsonSerializer.Serialize(k)},{v}]")
        |> String.concat ","
        |> sprintf "[%s]"
    let actual = JsonSerializer.Serialize(m, arrayOfPairsOptions)
    Assert.Equal(expected, actual)

[<Property>]
let ``deserialize newtype-string-keyed map with ArrayOfPairs`` (m: Map<NonNull<string>, int>) =
    let m = (Map.empty, m) ||> Map.fold (fun m (NonNull k) v -> Map.add (UserId k) v m)
    let ser =
        m
        |> Seq.map (fun (KeyValue (UserId k, v)) -> $"[{JsonSerializer.Serialize(k)},{v}]")
        |> String.concat ","
        |> sprintf "[%s]"
    let actual = JsonSerializer.Deserialize<Map<UserId, int>>(ser, arrayOfPairsOptions)
    Assert.Equal<Map<UserId, int>>(m, actual)

[<Property>]
let ``serialize newtype-string-keyed map with ArrayOfPairs`` (m: Map<NonNull<string>, int>) =
    let m = (Map.empty, m) ||> Map.fold (fun m (NonNull k) v -> Map.add (UserId k) v m)
    let expected =
        m
        |> Seq.map (fun (KeyValue (UserId k, v)) -> $"[{JsonSerializer.Serialize(k)},{v}]")
        |> String.concat ","
        |> sprintf "[%s]"
    let actual = JsonSerializer.Serialize(m, arrayOfPairsOptions)
    Assert.Equal(expected, actual)

[<Property>]
let ``deserialize Guid-keyed map with ArrayOfPairs`` (m: Map<Guid, int>) =
    let ser =
        m
        |> Seq.map (fun (KeyValue (k, v)) -> $"[\"{k}\",{v}]")
        |> String.concat ","
        |> sprintf "[%s]"
    let actual = JsonSerializer.Deserialize<Map<Guid, int>>(ser, arrayOfPairsOptions)
    Assert.Equal<Map<Guid, int>>(m, actual)

[<Property>]
let ``serialize Guid-keyed map with ArrayOfPairs`` (m: Map<Guid, int>) =
    let expected =
        m
        |> Seq.map (fun (KeyValue (k, v)) -> $"[\"{k}\",{v}]")
        |> String.concat ","
        |> sprintf "[%s]"
    let actual = JsonSerializer.Serialize(m, arrayOfPairsOptions)
    Assert.Equal(expected, actual)

[<Property>]
let ``deserialize 2-tuple`` (a, b as t: int * string) =
    let ser = sprintf "[%i,%s]" a (JsonSerializer.Serialize b)
    let result =
        tryblock (fun () -> JsonSerializer.Deserialize<int * string>(ser, options))
    match b, result with
    | null, Ok _ -> failwith "Deserializing null for a non-nullable field should fail"
    | _, Ok actual -> Assert.Equal(t, actual)
    | null, Error msg ->
        Assert.Equal((sprintf "Unexpected null inside tuple-array. Expected type String, but got null."), msg)
    | _, Error msg -> failwithf "Unexpected deserialization error %s" msg

[<Property>]
let ``deserialize 2-tuple with unit`` (a, b as t: int * unit) =
    let ser = sprintf "[%i,%s]" a (JsonSerializer.Serialize b)
    let result =
        tryblock (fun () -> JsonSerializer.Deserialize<int * unit>(ser, options))
    match result with
    | Ok actual -> Assert.Equal(t, actual)
    | Error msg -> failwithf "Unexpected deserialization error %s" msg

[<Property>]
let ``serialize 2-tuple`` (a, b as t: int * string) =
    let expected = sprintf "[%i,%s]" a (JsonSerializer.Serialize b)
    let actual = JsonSerializer.Serialize(t, options)
    Assert.Equal(expected, actual)

[<Property>]
let ``deserialize 8-tuple`` (a, b, c, d, e, f, g, h as t: int * int * int * int * int * int * int * int) =
    let ser = sprintf "[%i,%i,%i,%i,%i,%i,%i,%i]" a b c d e f g h
    let actual =
        JsonSerializer.Deserialize<int * int * int * int * int * int * int * int>(ser, options)
    Assert.Equal(t, actual)

[<Property>]
let ``serialize 8-tuple`` (a, b, c, d, e, f, g, h as t: int * int * int * int * int * int * int * int) =
    let expected = sprintf "[%i,%i,%i,%i,%i,%i,%i,%i]" a b c d e f g h
    let actual = JsonSerializer.Serialize(t, options)
    Assert.Equal(expected, actual)

[<Property>]
let ``deserialize struct 2-tuple`` (a, b as t: struct (int * string)) =
    let ser = sprintf "[%i,%s]" a (JsonSerializer.Serialize b)
    let result =
        tryblock (fun () -> JsonSerializer.Deserialize<struct (int * string)>(ser, options))
    match b, result with
    | null, Ok _ -> failwith "Deserializing null for a non-nullable field should fail"
    | _, Ok actual -> Assert.Equal(t, actual)
    | null, Error msg ->
        Assert.Equal((sprintf "Unexpected null inside tuple-array. Expected type String, but got null."), msg)
    | _, Error msg -> failwithf "Unexpected deserialization error %s" msg


[<Property>]
let ``serialize struct 2-tuple`` (a, b as t: struct (int * string)) =
    let expected = sprintf "[%i,%s]" a (JsonSerializer.Serialize b)
    let actual = JsonSerializer.Serialize(t, options)
    Assert.Equal(expected, actual)

[<Property>]
let ``deserialize struct 8-tuple``
    (a, b, c, d, e, f, g, h as t: struct (int * int * int * int * int * int * int * int))
    =
    let ser = sprintf "[%i,%i,%i,%i,%i,%i,%i,%i]" a b c d e f g h
    let actual =
        JsonSerializer.Deserialize<struct (int * int * int * int * int * int * int * int)>(ser, options)
    Assert.Equal(t, actual)

[<Property>]
let ``serialize struct 8-tuple`` (a, b, c, d, e, f, g, h as t: struct (int * int * int * int * int * int * int * int)) =
    let expected = sprintf "[%i,%i,%i,%i,%i,%i,%i,%i]" a b c d e f g h
    let actual = JsonSerializer.Serialize(t, options)
    Assert.Equal(expected, actual)

module NullCollections =

    [<Fact>]
    let ``disallow null list`` () =
        Assert.Throws<JsonException>(fun () -> JsonSerializer.Deserialize<int list>("null", options) |> ignore)
        |> ignore

    [<Fact>]
    let ``disallow null set`` () =
        Assert.Throws<JsonException>(fun () -> JsonSerializer.Deserialize<Set<int>>("null", options) |> ignore)
        |> ignore

    [<Fact>]
    let ``disallow null string maps`` () =
        Assert.Throws<JsonException>(fun () -> JsonSerializer.Deserialize<Map<string, int>>("null", options) |> ignore)
        |> ignore

    [<Fact>]
    let ``disallow null non-string maps`` () =
        Assert.Throws<JsonException>(fun () -> JsonSerializer.Deserialize<Map<int, int>>("null", options) |> ignore)
        |> ignore

    [<Fact>]
    let ``disallow null 2-tuple`` () =
        Assert.Throws<JsonException>(fun () -> JsonSerializer.Deserialize<int * int>("null", options) |> ignore)
        |> ignore

    [<Fact>]
    let ``disallow null 8-tuple`` () =
        Assert.Throws<JsonException>(fun () ->
            JsonSerializer.Deserialize<int * int * int * int * int * int * int * int>("null", options)
            |> ignore
        )
        |> ignore

    [<Fact>]
    let ``disallow null struct 2-tuple`` () =
        Assert.Throws<JsonException>(fun () -> JsonSerializer.Deserialize<struct (int * int)>("null", options) |> ignore
        )
        |> ignore

    [<Fact>]
    let ``disallow null struct 8-tuple`` () =
        Assert.Throws<JsonException>(fun () ->
            JsonSerializer.Deserialize<struct (int * int * int * int * int * int * int * int)>("null", options)
            |> ignore
        )
        |> ignore
