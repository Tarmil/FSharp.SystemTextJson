namespace System.Text.Json.Serialization

#nowarn "42"

open System
open System.Collections.Generic

[<RequireQualifiedAccess>]
type JsonName =
    | String of string
    | Int of int
    | Bool of bool

    member this.AsString() =
        match this with
        | String name -> name
        | Int name -> string name
        | Bool true -> "true"
        | Bool false -> "false"

[<AttributeUsage(AttributeTargets.Property, AllowMultiple = true)>]
type JsonNameAttribute(name: JsonName) =
    inherit Attribute()

    member _.Name = name

    new(name: string) = JsonNameAttribute(JsonName.String name)

    new(name: int) = JsonNameAttribute(JsonName.Int name)

    new(name: bool) = JsonNameAttribute(JsonName.Bool name)

type JsonNameComparer(stringComparer: StringComparer) =

    static member val OrdinalIgnoreCase = JsonNameComparer(StringComparer.OrdinalIgnoreCase)

    member _.Equals(x, y) =
        match x, y with
        | JsonName.String x, JsonName.String y -> stringComparer.Equals(x, y)
        | JsonName.Int x, JsonName.Int y -> x = y
        | JsonName.Bool x, JsonName.Bool y -> x = y
        | _ -> false

    member _.GetHashCode(x) =
        match x with
        | JsonName.String x -> stringComparer.GetHashCode(x)
        | JsonName.Int x -> x ^^^ 226201
        | JsonName.Bool b -> (# "" b : int #)

    interface IEqualityComparer<JsonName> with

        member this.Equals(x, y) =
            this.Equals(x, y)

        member this.GetHashCode(obj) =
            this.GetHashCode(obj)
