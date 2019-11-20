module internal System.Text.Json.Serialization.Helpers

open System
open System.Text.Json

let fail expected (reader: byref<Utf8JsonReader>) (ty: Type) =
    sprintf "Failed to parse type %s: expected %s, found %A"
        ty.FullName expected reader.TokenType
    |> JsonException
    |> raise

let expectAlreadyRead expectedTokenType expectedLabel (reader: byref<Utf8JsonReader>) ty =
    if reader.TokenType <> expectedTokenType then
        fail expectedLabel &reader ty

let readExpecting expectedTokenType expectedLabel (reader: byref<Utf8JsonReader>) ty =
    if not (reader.Read()) || reader.TokenType <> expectedTokenType then
        fail expectedLabel &reader ty

let readExpectingPropertyNamed (expectedPropertyName: string) (reader: byref<Utf8JsonReader>) ty =
    if not (reader.Read()) || reader.TokenType <> JsonTokenType.PropertyName || not (reader.ValueTextEquals expectedPropertyName) then
        fail ("\"" + expectedPropertyName + "\"") &reader ty

let writePropertyName (writer: Utf8JsonWriter) (options: JsonSerializerOptions) propertyName =
    propertyName |> options.PropertyNamingPolicy.ConvertName |> writer.WritePropertyName