﻿namespace System.Text.Json.Serialization

open System
open System.Runtime.Serialization
open System.Text.Json
open FSharp.Reflection
open System.Collections.Generic

type JsonRecordConverter<'T>() =
    inherit JsonConverter<'T>()

    static let ty = typeof<'T>

    static let fields = RecordReflection.fields<'T>()

    static let fieldIndices = Dictionary(StringComparer.InvariantCulture)
    static do fields |> Array.iteri (fun i f ->
        fieldIndices.[f.Name] <- f)

    static let expectedFieldCount =
        fields
        |> Seq.filter (fun p -> not p.Ignore)
        |> Seq.length

    static let ctor =
        if ty.IsValueType then
            fun () -> Unchecked.defaultof<'T>
        else
            fun () -> FormatterServices.GetUninitializedObject(ty) :?> 'T

    static let fieldIndex (reader: byref<Utf8JsonReader>) =
        let mutable found = ValueNone
        let mutable i = 0
        while found.IsNone && i < fields.Length do
            let p = fields.[i]
            if reader.ValueTextEquals(p.Name.AsSpan()) then
                found <- ValueSome p
            else
                i <- i + 1
        found

    override __.Read(reader, typeToConvert, options) =
        if reader.TokenType <> JsonTokenType.StartObject then
            raise (JsonException("Failed to parse record type " + typeToConvert.FullName + ", expected JSON object, found " + string reader.TokenType))

        let mutable res = ctor()
        let mutable cont = true
        let mutable fieldsFound = 0
        while cont && reader.Read() do
            match reader.TokenType with
            | JsonTokenType.EndObject ->
                cont <- false
            | JsonTokenType.PropertyName ->
                match fieldIndex &reader with
                | ValueSome p when not p.Ignore ->
                    fieldsFound <- fieldsFound + 1
                    match p.Deserialize with
                    | DStruct p -> p.Invoke(&reader, &res, options)
                    | DRefobj p -> p.Invoke(&reader, res, options)
                | _ ->
                    reader.Skip()
            | _ -> ()

        if fieldsFound < expectedFieldCount then
            raise (JsonException("Missing field for record type " + typeToConvert.FullName))
        res

    override __.Write(writer, value, options) =
        writer.WriteStartObject()
        for p in fields do
            if not p.Ignore then
                writer.WritePropertyName(p.Name)
                match p.Serialize with
                | SStruct p -> p.Invoke(writer, &value, options)
                | SRefobj p -> p.Invoke(writer, value, options)
        writer.WriteEndObject()

type JsonRecordConverter() =
    inherit JsonConverterFactory()

    static member internal CanConvert(typeToConvert) =
        TypeCache.isRecord typeToConvert

    static member internal CreateConverter(typeToConvert) =
        typedefof<JsonRecordConverter<_>>
            .MakeGenericType([|typeToConvert|])
            .GetConstructor([||])
            .Invoke([||])
        :?> JsonConverter

    override __.CanConvert(typeToConvert) =
        JsonRecordConverter.CanConvert(typeToConvert)

    override __.CreateConverter(typeToConvert, _options) =
        JsonRecordConverter.CreateConverter(typeToConvert)
