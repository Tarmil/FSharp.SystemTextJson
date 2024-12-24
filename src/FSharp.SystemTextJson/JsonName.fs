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

[<AttributeUsage(AttributeTargets.Property ||| AttributeTargets.Method, AllowMultiple = true)>]
type JsonNameAttribute(name: JsonName, otherNames: JsonName[]) =
    inherit Attribute()

    static let convertName (name: obj) =
        match name with
        | :? string as s -> JsonName.String s
        | :? int as i -> JsonName.Int i
        | :? bool as b -> JsonName.Bool b
        | _ -> invalidArg "otherNames" "JsonName must be a string, int or bool"

    member _.Name = name

    member _.OtherNames = otherNames

    member _.AllNames = Array.append [| name |] otherNames

    /// The name of the union field that this name applies to.
    member val Field = null: string with get, set

    new(name: string, [<ParamArray>] otherNames: obj[]) =
        JsonNameAttribute(JsonName.String name, Array.map convertName otherNames)

    new(name: int, [<ParamArray>] otherNames: obj[]) =
        JsonNameAttribute(JsonName.Int name, Array.map convertName otherNames)

    new(name: bool, [<ParamArray>] otherNames: obj[]) =
        JsonNameAttribute(JsonName.Bool name, Array.map convertName otherNames)

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
