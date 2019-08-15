module Tests.Union

open Xunit
open System.Text.Json.Serialization
open System.Text.Json

module NonStruct =

    [<JsonFSharpConverter>]
    type A =
        | Aa
        | Ab of int
        | Ac of string * bool

    [<Fact>]
    let ``deserialize via explicit converter`` () =
        Assert.Equal(Aa, JsonSerializer.Deserialize """{"Case":"Aa"}""")
        Assert.Equal(Ab 32, JsonSerializer.Deserialize """{"Case":"Ab","Fields":[32]}""")
        Assert.Equal(Ac("test", true), JsonSerializer.Deserialize """{"Case":"Ac","Fields":["test",true]}""")

    [<Fact>]
    let ``serialize via explicit converter`` () =
        Assert.Equal("""{"Case":"Aa"}""", JsonSerializer.Serialize Aa)
        Assert.Equal("""{"Case":"Ab","Fields":[32]}""", JsonSerializer.Serialize(Ab 32))
        Assert.Equal("""{"Case":"Ac","Fields":["test",true]}""", JsonSerializer.Serialize(Ac("test", true)))

    type B =
        | Ba
        | Bb of int
        | Bc of string * bool

    let options = JsonSerializerOptions()
    options.Converters.Add(JsonFSharpConverter())

    [<Fact>]
    let ``deserialize via options`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"Case":"Ba"}""", options))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"Case":"Bb","Fields":[32]}""", options))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""{"Case":"Bc","Fields":["test",true]}""", options))

    [<Fact>]
    let ``serialize via options`` () =
        Assert.Equal("""{"Case":"Ba"}""", JsonSerializer.Serialize(Ba, options))
        Assert.Equal("""{"Case":"Bb","Fields":[32]}""", JsonSerializer.Serialize(Bb 32, options))
        Assert.Equal("""{"Case":"Bc","Fields":["test",true]}""", JsonSerializer.Serialize(Bc("test", true), options))

    [<CompilationRepresentation(CompilationRepresentationFlags.UseNullAsTrueValue)>]
    type C =
        | Ca
        | Cb of int

    [<Fact>]
    let ``deserialize UseNull`` () =
        Assert.Equal(Ca, JsonSerializer.Deserialize("""null""", options))
        Assert.Equal(Cb 32, JsonSerializer.Deserialize("""{"Case":"Cb","Fields":[32]}""", options))

    [<Fact>]
    let ``serialize UseNull`` () =
        Assert.Equal("""null""", JsonSerializer.Serialize(Ca, options))
        Assert.Equal("""{"Case":"Cb","Fields":[32]}""", JsonSerializer.Serialize(Cb 32, options))

    [<JsonFSharpConverter(JsonUnionEncoding.ExternalTag)>]
    type D =
        | Da
        | Db of int
        | Dc of string * bool

    [<Fact>]
    let ``deserialize ExternalTag`` () =
        Assert.Equal(Da, JsonSerializer.Deserialize """{"Da":[]}""")
        Assert.Equal(Db 32, JsonSerializer.Deserialize """{"Db":[32]}""")
        Assert.Equal(Dc("test", true), JsonSerializer.Deserialize """{"Dc":["test",true]}""")

    [<Fact>]
    let ``serialize ExternalTag`` () =
        Assert.Equal("""{"Da":[]}""", JsonSerializer.Serialize Da)
        Assert.Equal("""{"Db":[32]}""", JsonSerializer.Serialize(Db 32))
        Assert.Equal("""{"Dc":["test",true]}""", JsonSerializer.Serialize(Dc("test", true)))

    [<JsonFSharpConverter(JsonUnionEncoding.InternalTag)>]
    type E =
        | Ea
        | Eb of int
        | Ec of string * bool

    [<Fact>]
    let ``deserialize InternalTag`` () =
        Assert.Equal(Ea, JsonSerializer.Deserialize """["Ea"]""")
        Assert.Equal(Eb 32, JsonSerializer.Deserialize """["Eb",32]""")
        Assert.Equal(Ec("test", true), JsonSerializer.Deserialize """["Ec","test",true]""")

    [<Fact>]
    let ``serialize InternalTag`` () =
        Assert.Equal("""["Ea"]""", JsonSerializer.Serialize Ea)
        Assert.Equal("""["Eb",32]""", JsonSerializer.Serialize(Eb 32))
        Assert.Equal("""["Ec","test",true]""", JsonSerializer.Serialize(Ec("test", true)))

module Struct =

    [<Struct; JsonFSharpConverter>]
    type A =
        | Aa
        | Ab of int
        | Ac of string * bool

    [<Fact>]
    let ``deserialize via explicit converter`` () =
        Assert.Equal(Aa, JsonSerializer.Deserialize """{"Case":"Aa"}""")
        Assert.Equal(Ab 32, JsonSerializer.Deserialize """{"Case":"Ab","Fields":[32]}""")
        Assert.Equal(Ac("test", true), JsonSerializer.Deserialize """{"Case":"Ac","Fields":["test",true]}""")

    [<Fact>]
    let ``serialize via explicit converter`` () =
        Assert.Equal("""{"Case":"Aa"}""", JsonSerializer.Serialize Aa)
        Assert.Equal("""{"Case":"Ab","Fields":[32]}""", JsonSerializer.Serialize(Ab 32))
        Assert.Equal("""{"Case":"Ac","Fields":["test",true]}""", JsonSerializer.Serialize(Ac("test", true)))

    [<Struct>]
    type B =
        | Ba
        | Bb of int
        | Bc of string * bool

    let options = JsonSerializerOptions()
    options.Converters.Add(JsonFSharpConverter())

    [<Fact>]
    let ``deserialize via options`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"Case":"Ba"}""", options))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"Case":"Bb","Fields":[32]}""", options))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""{"Case":"Bc","Fields":["test",true]}""", options))

    [<Fact>]
    let ``serialize via options`` () =
        Assert.Equal("""{"Case":"Ba"}""", JsonSerializer.Serialize(Ba, options))
        Assert.Equal("""{"Case":"Bb","Fields":[32]}""", JsonSerializer.Serialize(Bb 32, options))
        Assert.Equal("""{"Case":"Bc","Fields":["test",true]}""", JsonSerializer.Serialize(Bc("test", true), options))

    [<Struct; JsonFSharpConverter(JsonUnionEncoding.ExternalTag)>]
    type D =
        | Da
        | Db of int
        | Dc of string * bool

    [<Fact>]
    let ``deserialize ExternalTag`` () =
        Assert.Equal(Da, JsonSerializer.Deserialize """{"Da":[]}""")
        Assert.Equal(Db 32, JsonSerializer.Deserialize """{"Db":[32]}""")
        Assert.Equal(Dc("test", true), JsonSerializer.Deserialize """{"Dc":["test",true]}""")

    [<Fact>]
    let ``serialize ExternalTag`` () =
        Assert.Equal("""{"Da":[]}""", JsonSerializer.Serialize Da)
        Assert.Equal("""{"Db":[32]}""", JsonSerializer.Serialize(Db 32))
        Assert.Equal("""{"Dc":["test",true]}""", JsonSerializer.Serialize(Dc("test", true)))

    [<Struct; JsonFSharpConverter(JsonUnionEncoding.InternalTag)>]
    type E =
        | Ea
        | Eb of int
        | Ec of string * bool

    [<Fact>]
    let ``deserialize InternalTag`` () =
        Assert.Equal(Ea, JsonSerializer.Deserialize """["Ea"]""")
        Assert.Equal(Eb 32, JsonSerializer.Deserialize """["Eb",32]""")
        Assert.Equal(Ec("test", true), JsonSerializer.Deserialize """["Ec","test",true]""")

    [<Fact>]
    let ``serialize InternalTag`` () =
        Assert.Equal("""["Ea"]""", JsonSerializer.Serialize Ea)
        Assert.Equal("""["Eb",32]""", JsonSerializer.Serialize(Eb 32))
        Assert.Equal("""["Ec","test",true]""", JsonSerializer.Serialize(Ec("test", true)))
