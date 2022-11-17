﻿module internal System.Text.Json.Serialization.Helpers

#nowarn "44" // JsonSerializerOptions.IgnoreNullValues is obsolete for users but still relevant for converters.

open System
open System.Collections.Generic
open System.Reflection
open System.Text.Json
open System.Text.Json.Serialization
open FSharp.Reflection

let failf format =
    Printf.kprintf (JsonException >> raise) format

let failExpecting expected (reader: byref<Utf8JsonReader>) (ty: Type) =
    failf "Failed to parse type %s: expected %s, found %A" ty.FullName expected reader.TokenType

let expectAlreadyRead expectedTokenType expectedLabel (reader: byref<Utf8JsonReader>) ty =
    if reader.TokenType <> expectedTokenType then
        failExpecting expectedLabel &reader ty

let readExpecting expectedTokenType expectedLabel (reader: byref<Utf8JsonReader>) ty =
    if not (reader.Read()) || reader.TokenType <> expectedTokenType then
        failExpecting expectedLabel &reader ty

let inline readIsExpectingPropertyNamed (expectedPropertyName: string) (reader: byref<Utf8JsonReader>) ty =
    reader.Read()
    && reader.TokenType = JsonTokenType.PropertyName
    && (reader.ValueTextEquals expectedPropertyName)

let readExpectingPropertyNamed (expectedPropertyName: string) (reader: byref<Utf8JsonReader>) ty =
    if not <| readIsExpectingPropertyNamed expectedPropertyName &reader ty then
        failExpecting ("\"" + expectedPropertyName + "\"") &reader ty

let isNullableUnion (ty: Type) =
    ty.GetCustomAttributes(typeof<CompilationRepresentationAttribute>, false)
    |> Array.exists (fun x ->
        let x = (x :?> CompilationRepresentationAttribute)
        x.Flags.HasFlag(CompilationRepresentationFlags.UseNullAsTrueValue)
    )

let isSkippableType (ty: Type) =
    ty.IsGenericType && ty.GetGenericTypeDefinition() = typedefof<Skippable<_>>

let isValueOptionType (ty: Type) =
    ty.IsGenericType && ty.GetGenericTypeDefinition() = typedefof<ValueOption<_>>

let isSkip (ty: Type) =
    if isSkippableType ty then
        let getTag = FSharpValue.PreComputeUnionTagReader(ty)
        fun x -> getTag x = 0
    else
        fun _ -> false

[<AutoOpen>]
type Helper =
    static member tryGetUnionCases(ty: Type, cases: UnionCaseInfo[] outref) =
        let isUnion = FSharpType.IsUnion(ty, true)
        if isUnion then cases <- FSharpType.GetUnionCases(ty, true)
        isUnion

    static member tryGetUnionCaseSingleProperty(case: UnionCaseInfo, property: PropertyInfo outref) =
        let properties = case.GetFields()
        let isSingle = properties.Length = 1
        if isSingle then property <- properties[0]
        isSingle

    static member tryGetUnwrappedSingleCaseField
        (
            fsOptions: JsonFSharpOptions,
            ty: Type,
            cases: UnionCaseInfo[] outref,
            property: PropertyInfo outref
        ) =
        tryGetUnionCases (ty, &cases)
        && fsOptions.UnionEncoding.HasFlag JsonUnionEncoding.UnwrapSingleCaseUnions
        && cases.Length = 1
        && tryGetUnionCaseSingleProperty (cases[0], &property)

/// If null is a valid JSON representation for ty,
/// then return ValueSome with the value represented by null,
/// else return ValueNone.
let rec tryGetNullValue (fsOptions: JsonFSharpOptions) (ty: Type) : obj voption =
    if isNullableUnion ty then
        ValueSome null
    elif ty.IsEnum then
        ValueSome(Enum.ToObject(ty, 0))
    elif ty = typeof<unit> then
        ValueSome()
    elif fsOptions.UnionEncoding.HasFlag JsonUnionEncoding.UnwrapOption
         && isValueOptionType ty then
        ValueSome(FSharpValue.MakeUnion(FSharpType.GetUnionCases(ty, true)[0], [||], true))
    elif isSkippableType ty then
        tryGetNullValue fsOptions (ty.GetGenericArguments()[0])
        |> ValueOption.map (fun x -> FSharpValue.MakeUnion(FSharpType.GetUnionCases(ty, true)[1], [| x |], true))
    elif
        fsOptions.AllowNullFields
        && not (FSharpType.IsUnion(ty, true))
        && not (FSharpType.IsRecord(ty, true))
        && not (FSharpType.IsTuple(ty))
    then
        ValueSome(if ty.IsValueType then Activator.CreateInstance(ty) else null)
    else
        match tryGetUnwrappedSingleCaseField (fsOptions, ty) with
        | true, _, field ->
            tryGetNullValue fsOptions field.PropertyType
            |> ValueOption.map (fun x -> FSharpValue.MakeUnion(FSharpType.GetUnionCases(ty, true)[0], [| x |], true))
        | false, _, _ -> ValueNone

let rec isNullableFieldType (fsOptions: JsonFSharpOptions) (ty: Type) =
    tryGetNullValue fsOptions ty |> ValueOption.isSome

let overrideOptions (ty: Type) (defaultOptions: JsonFSharpOptions) (overrides: IDictionary<Type, JsonFSharpOptions>) =
    let inheritUnionEncoding (options: JsonFSharpOptions) =
        if options.UnionEncoding.HasFlag(JsonUnionEncoding.Inherit) then
            options.WithUnionEncoding(defaultOptions.UnionEncoding)
        else
            options

    let applyAttributeOverride () =
        if defaultOptions.AllowOverride then
            match
                ty.GetCustomAttributes(typeof<IJsonFSharpConverterAttribute>, true)
                |> Array.tryHead
                with
            | Some (:? IJsonFSharpConverterAttribute as attr) -> attr.Options |> inheritUnionEncoding
            | _ -> defaultOptions
        else
            defaultOptions

    if isNull overrides then
        applyAttributeOverride ()
    else
        match overrides.TryGetValue(ty) with
        | true, options -> options |> inheritUnionEncoding
        | false, _ -> applyAttributeOverride ()

let ignoreNullValues (options: JsonSerializerOptions) =
    options.IgnoreNullValues
    || options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull

let convertName (policy: JsonNamingPolicy) (name: string) =
    match policy with
    | null -> name
    | policy -> policy.ConvertName(name)

let getJsonNames kind (getAttributes: Type -> obj[]) =
    match getAttributes typeof<JsonNameAttribute>
          |> Array.choose (
              function
              | :? JsonNameAttribute as attr when isNull attr.Field -> Some attr
              | _ -> None
          )
        with
    | [||] ->
        match getAttributes typeof<JsonPropertyNameAttribute> with
        | [| :? JsonPropertyNameAttribute as attr |] -> ValueSome [| JsonName.String attr.Name |]
        | _ -> ValueNone
    | [| attr |] -> ValueSome attr.AllNames
    | _ ->
        failf "To provide multiple names for the same %s, use a single JsonNameAttribute with multiple arguments" kind

let getJsonFieldNames (getAttributes: Type -> obj[]) =
    getAttributes typeof<JsonNameAttribute>
    |> Seq.choose (
        function
        | :? JsonNameAttribute as attr when not (isNull attr.Field) -> Some(attr.Field, attr.AllNames)
        | _ -> None
    )
    |> readOnlyDict
