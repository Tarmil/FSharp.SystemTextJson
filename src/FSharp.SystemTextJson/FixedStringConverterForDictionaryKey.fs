namespace System.Text.Json.Serialization

/// Pre-8.0, the built-in StringConverter doesn't support {Read|Write}AsPropertyName() correctly.
/// See https://github.com/dotnet/runtime/issues/77326
type internal FixedStringConverterForDictionaryKey() =
    inherit JsonConverter<string>()

    override this.Read(reader, _typeToConvert, _options) =
        reader.GetString()

    override this.Write(writer, value, _options) =
        writer.WriteStringValue(value)

    override this.ReadAsPropertyName(reader, _typeToConvert, _options) =
        reader.GetString()

    override this.WriteAsPropertyName(writer, value, options) =
        match options.DictionaryKeyPolicy with
        | null -> value
        | p -> p.ConvertName value
        |> writer.WritePropertyName
