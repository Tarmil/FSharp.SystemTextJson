namespace System.Text.Json.Serialization

open System
open FSharp.Reflection

type JsonFSharpConverter() =
    inherit JsonConverterFactory()

    override this.CanConvert(typeToConvert) =
        JsonRecordConverter.CanConvert(typeToConvert) ||
        JsonUnionConverter.CanConvert(typeToConvert)

    static member internal CreateConverter(typeToConvert) =
        if JsonRecordConverter.CanConvert(typeToConvert) then
            JsonRecordConverter.CreateConverter(typeToConvert)
        elif JsonUnionConverter.CanConvert(typeToConvert) then
            JsonUnionConverter.CreateConverter(typeToConvert)
        else
            invalidOp ("Not an F# record or union type: " + typeToConvert.FullName)

    override this.CreateConverter(typeToConvert) =
        JsonFSharpConverter.CreateConverter(typeToConvert)

[<AttributeUsage(AttributeTargets.Class ||| AttributeTargets.Struct)>]
type JsonFSharpConverterAttribute() =
    inherit JsonConverterAttribute()

    override __.CreateConverter(typeToConvert) =
        JsonFSharpConverter.CreateConverter(typeToConvert)
