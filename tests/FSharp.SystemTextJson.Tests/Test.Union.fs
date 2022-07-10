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
        Assert.Equal(Ac("test", true), JsonSerializer.Deserialize """{"Fields":["test",true],"Case":"Ac"}""")

    [<Fact>]
    let ``serialize via explicit converter`` () =
        Assert.Equal("""{"Case":"Aa"}""", JsonSerializer.Serialize Aa)
        Assert.Equal("""{"Case":"Ab","Fields":[32]}""", JsonSerializer.Serialize(Ab 32))
        Assert.Equal("""{"Case":"Ac","Fields":["test",true]}""", JsonSerializer.Serialize(Ac("test", true)))

    type B =
        | Ba
        | Bb of int
        | Bc of x: string * bool

    type JN =
        | [<JsonName "jstring">] JNs of jnsField: int
        | [<JsonName 42>] JNi of jniField: int
        | [<JsonName true; JsonName "jbool">] JNb of jnbField: int
        | JNn of jnnField: int

    let options = JsonSerializerOptions()
    options.Converters.Add(JsonFSharpConverter())

    [<Fact>]
    let ``deserialize via options`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"Case":"Ba"}""", options))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"Case":"Bb","Fields":[32]}""", options))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""{"Case":"Bc","Fields":["test",true]}""", options))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""{"Fields":["test",true],"Case":"Bc"}""", options))

    [<Fact>]
    let ``serialize via options`` () =
        Assert.Equal("""{"Case":"Ba"}""", JsonSerializer.Serialize(Ba, options))
        Assert.Equal("""{"Case":"Bb","Fields":[32]}""", JsonSerializer.Serialize(Bb 32, options))
        Assert.Equal("""{"Case":"Bc","Fields":["test",true]}""", JsonSerializer.Serialize(Bc("test", true), options))

    [<Fact>]
    let ``not fill in nulls`` () =
        try
            JsonSerializer.Deserialize<B>("""{"Case":"Bc","Fields":[null,true]}""", options)
            |> ignore
            failwith "Deserialization was supposed to fail on the line above"
        with :? JsonException as e ->
            Assert.Equal("B.Bc(x) was expected to be of type String, but was null.", e.Message)

    [<Fact>]
    let allowNullFields () =
        let options = JsonSerializerOptions()
        options.Converters.Add(JsonFSharpConverter(allowNullFields = true))
        let actual =
            JsonSerializer.Deserialize("""{"Case":"Bc","Fields":[null,true]}""", options)
        Assert.Equal(Bc(null, true), actual)

    let tagPolicyOptions = JsonSerializerOptions()
    tagPolicyOptions.Converters.Add(JsonFSharpConverter(unionTagNamingPolicy = JsonNamingPolicy.CamelCase))

    [<Fact>]
    let ``deserialize AdjacentTag with tag policy`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"Case":"ba"}""", tagPolicyOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"Case":"bb","Fields":[32]}""", tagPolicyOptions))
        Assert.Equal(
            Bc("test", true),
            JsonSerializer.Deserialize("""{"Case":"bc","Fields":["test",true]}""", tagPolicyOptions)
        )
        Assert.Equal(
            Bc("test", true),
            JsonSerializer.Deserialize("""{"Fields":["test",true],"Case":"bc"}""", tagPolicyOptions)
        )

    [<Fact>]
    let ``serialize AdjacentTag with tag policy`` () =
        Assert.Equal("""{"Case":"ba"}""", JsonSerializer.Serialize(Ba, tagPolicyOptions))
        Assert.Equal("""{"Case":"bb","Fields":[32]}""", JsonSerializer.Serialize(Bb 32, tagPolicyOptions))
        Assert.Equal(
            """{"Case":"bc","Fields":["test",true]}""",
            JsonSerializer.Serialize(Bc("test", true), tagPolicyOptions)
        )

    let tagCaseInsensitiveOptions = JsonSerializerOptions()
    tagCaseInsensitiveOptions.Converters.Add(JsonFSharpConverter(unionTagCaseInsensitive = true))

    [<Fact>]
    let ``deserialize AdjacentTag with case insensitive tag`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"Case":"bA"}""", tagCaseInsensitiveOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"Case":"bB", "Fields":[32]}""", tagCaseInsensitiveOptions))
        Assert.Equal(
            Bc("test", true),
            JsonSerializer.Deserialize("""{"Case":"bC", "Fields":["test",true]}""", tagCaseInsensitiveOptions)
        )
        Assert.Equal(
            Bc("test", true),
            JsonSerializer.Deserialize("""{"Fields":["test",true],"Case":"bC"}""", tagCaseInsensitiveOptions)
        )

    [<Fact>]
    let ``serialize AdjacentTag with case insensitive tag`` () =
        Assert.Equal("""{"Case":"Ba"}""", JsonSerializer.Serialize(Ba, tagCaseInsensitiveOptions))
        Assert.Equal("""{"Case":"Bb","Fields":[32]}""", JsonSerializer.Serialize(Bb 32, tagCaseInsensitiveOptions))
        Assert.Equal(
            """{"Case":"Bc","Fields":["test",true]}""",
            JsonSerializer.Serialize(Bc("test", true), tagCaseInsensitiveOptions)
        )

    [<Fact>]
    let ``deserialize AdjacentTag with JsonName`` () =
        Assert.Equal(JNs 1, JsonSerializer.Deserialize("""{"Case":"jstring","Fields":[1]}""", options))
        Assert.Equal(JNi 1, JsonSerializer.Deserialize("""{"Case":42,"Fields":[1]}""", options))
        Assert.Equal(JNb 1, JsonSerializer.Deserialize("""{"Case":true,"Fields":[1]}""", options))
        Assert.Equal(JNb 1, JsonSerializer.Deserialize("""{"Case":"jbool","Fields":[1]}""", options))
        Assert.Equal(JNn 1, JsonSerializer.Deserialize("""{"Case":"JNn","Fields":[1]}""", options))

    [<Fact>]
    let ``serialize AdjacentTag with JsonName`` () =
        Assert.Equal("""{"Case":"jstring","Fields":[1]}""", JsonSerializer.Serialize(JNs 1, options))
        Assert.Equal("""{"Case":42,"Fields":[1]}""", JsonSerializer.Serialize(JNi 1, options))
        Assert.Equal("""{"Case":true,"Fields":[1]}""", JsonSerializer.Serialize(JNb 1, options))
        Assert.Equal("""{"Case":"JNn","Fields":[1]}""", JsonSerializer.Serialize(JNn 1, options))

    [<CompilationRepresentation(CompilationRepresentationFlags.UseNullAsTrueValue)>]
    type C =
        | Ca
        | Cb of int

    [<Fact>]
    let ``deserialize UseNull`` () =
        Assert.Equal(Ca, JsonSerializer.Deserialize("""null""", options))
        Assert.Equal(Cb 32, JsonSerializer.Deserialize("""{"Case":"Cb","Fields":[32]}""", options))
        Assert.Equal(Cb 32, JsonSerializer.Deserialize("""{"Fields":[32],"Case":"Cb"}""", options))

    [<Fact>]
    let ``serialize UseNull`` () =
        Assert.Equal("""null""", JsonSerializer.Serialize(Ca, options))
        Assert.Equal("""{"Case":"Cb","Fields":[32]}""", JsonSerializer.Serialize(Cb 32, options))

    let externalTagOptions = JsonSerializerOptions()
    externalTagOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.ExternalTag))

    [<Fact>]
    let ``deserialize ExternalTag`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"Ba":[]}""", externalTagOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"Bb":[32]}""", externalTagOptions))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""{"Bc":["test",true]}""", externalTagOptions))

    [<Fact>]
    let ``serialize ExternalTag`` () =
        Assert.Equal("""{"Ba":[]}""", JsonSerializer.Serialize(Ba, externalTagOptions))
        Assert.Equal("""{"Bb":[32]}""", JsonSerializer.Serialize(Bb 32, externalTagOptions))
        Assert.Equal("""{"Bc":["test",true]}""", JsonSerializer.Serialize(Bc("test", true), externalTagOptions))

    let externalTagPolicyOptions = JsonSerializerOptions()

    externalTagPolicyOptions.Converters.Add(
        JsonFSharpConverter(JsonUnionEncoding.ExternalTag, unionTagNamingPolicy = JsonNamingPolicy.CamelCase)
    )

    [<Fact>]
    let ``deserialize ExternalTag with tag policy`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"ba":[]}""", externalTagPolicyOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"bb":[32]}""", externalTagPolicyOptions))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""{"bc":["test",true]}""", externalTagPolicyOptions))

    [<Fact>]
    let ``serialize ExternalTag with tag policy`` () =
        Assert.Equal("""{"ba":[]}""", JsonSerializer.Serialize(Ba, externalTagPolicyOptions))
        Assert.Equal("""{"bb":[32]}""", JsonSerializer.Serialize(Bb 32, externalTagPolicyOptions))
        Assert.Equal("""{"bc":["test",true]}""", JsonSerializer.Serialize(Bc("test", true), externalTagPolicyOptions))

    [<Fact>]
    let ``deserialize ExternalTag with JsonName`` () =
        Assert.Equal(JNs 1, JsonSerializer.Deserialize("""{"jstring":[1]}""", externalTagOptions))
        Assert.Equal(JNi 1, JsonSerializer.Deserialize("""{"42":[1]}""", externalTagOptions))
        Assert.Equal(JNb 1, JsonSerializer.Deserialize("""{"true":[1]}""", externalTagOptions))
        Assert.Equal(JNb 1, JsonSerializer.Deserialize("""{"jbool":[1]}""", externalTagOptions))
        Assert.Equal(JNn 1, JsonSerializer.Deserialize("""{"JNn":[1]}""", externalTagOptions))

    [<Fact>]
    let ``serialize ExternalTag with JsonName`` () =
        Assert.Equal("""{"jstring":[1]}""", JsonSerializer.Serialize(JNs 1, externalTagOptions))
        Assert.Equal("""{"42":[1]}""", JsonSerializer.Serialize(JNi 1, externalTagOptions))
        Assert.Equal("""{"true":[1]}""", JsonSerializer.Serialize(JNb 1, externalTagOptions))
        Assert.Equal("""{"JNn":[1]}""", JsonSerializer.Serialize(JNn 1, externalTagOptions))

    let internalTagOptions = JsonSerializerOptions()
    internalTagOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.InternalTag))

    [<Fact>]
    let ``deserialize InternalTag`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""["Ba"]""", internalTagOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""["Bb",32]""", internalTagOptions))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""["Bc","test",true]""", internalTagOptions))

    [<Fact>]
    let ``serialize InternalTag`` () =
        Assert.Equal("""["Ba"]""", JsonSerializer.Serialize(Ba, internalTagOptions))
        Assert.Equal("""["Bb",32]""", JsonSerializer.Serialize(Bb 32, internalTagOptions))
        Assert.Equal("""["Bc","test",true]""", JsonSerializer.Serialize(Bc("test", true), internalTagOptions))

    let internalTagPolicyOptions = JsonSerializerOptions()

    internalTagPolicyOptions.Converters.Add(
        JsonFSharpConverter(JsonUnionEncoding.InternalTag, unionTagNamingPolicy = JsonNamingPolicy.CamelCase)
    )

    [<Fact>]
    let ``deserialize InternalTag with tag policy`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""["ba"]""", internalTagPolicyOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""["bb",32]""", internalTagPolicyOptions))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""["bc","test",true]""", internalTagPolicyOptions))

    [<Fact>]
    let ``serialize InternalTag with tag policy`` () =
        Assert.Equal("""["ba"]""", JsonSerializer.Serialize(Ba, internalTagPolicyOptions))
        Assert.Equal("""["bb",32]""", JsonSerializer.Serialize(Bb 32, internalTagPolicyOptions))
        Assert.Equal("""["bc","test",true]""", JsonSerializer.Serialize(Bc("test", true), internalTagPolicyOptions))

    [<Fact>]
    let ``deserialize InternalTag with JsonName`` () =
        Assert.Equal(JNs 1, JsonSerializer.Deserialize("""["jstring",1]""", internalTagOptions))
        Assert.Equal(JNi 1, JsonSerializer.Deserialize("""[42,1]""", internalTagOptions))
        Assert.Equal(JNb 1, JsonSerializer.Deserialize("""[true,1]""", internalTagOptions))
        Assert.Equal(JNb 1, JsonSerializer.Deserialize("""["jbool",1]""", internalTagOptions))
        Assert.Equal(JNn 1, JsonSerializer.Deserialize("""["JNn",1]""", internalTagOptions))

    [<Fact>]
    let ``serialize InternalTag with JsonName`` () =
        Assert.Equal("""["jstring",1]""", JsonSerializer.Serialize(JNs 1, internalTagOptions))
        Assert.Equal("""[42,1]""", JsonSerializer.Serialize(JNi 1, internalTagOptions))
        Assert.Equal("""[true,1]""", JsonSerializer.Serialize(JNb 1, internalTagOptions))
        Assert.Equal("""["JNn",1]""", JsonSerializer.Serialize(JNn 1, internalTagOptions))

    let untaggedOptions = JsonSerializerOptions()
    untaggedOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.Untagged))

    [<Fact>]
    let ``deserialize Untagged`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{}""", untaggedOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"Item":32}""", untaggedOptions))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""{"x":"test","Item2":true}""", untaggedOptions))

    [<Fact>]
    let ``serialize Untagged`` () =
        Assert.Equal("""{}""", JsonSerializer.Serialize(Ba, untaggedOptions))
        Assert.Equal("""{"Item":32}""", JsonSerializer.Serialize(Bb 32, untaggedOptions))
        Assert.Equal("""{"x":"test","Item2":true}""", JsonSerializer.Serialize(Bc("test", true), untaggedOptions))

    [<Fact>]
    let ``deserialize Untagged with JsonName`` () =
        Assert.Equal(JNs 1, JsonSerializer.Deserialize("""{"jnsField":1}""", untaggedOptions))
        Assert.Equal(JNi 1, JsonSerializer.Deserialize("""{"jniField":1}""", untaggedOptions))
        Assert.Equal(JNb 1, JsonSerializer.Deserialize("""{"jnbField":1}""", untaggedOptions))
        Assert.Equal(JNn 1, JsonSerializer.Deserialize("""{"jnnField":1}""", untaggedOptions))

    [<Fact>]
    let ``serialize Untagged with JsonName`` () =
        Assert.Equal("""{"jnsField":1}""", JsonSerializer.Serialize(JNs 1, untaggedOptions))
        Assert.Equal("""{"jniField":1}""", JsonSerializer.Serialize(JNi 1, untaggedOptions))
        Assert.Equal("""{"jnbField":1}""", JsonSerializer.Serialize(JNb 1, untaggedOptions))
        Assert.Equal("""{"jnnField":1}""", JsonSerializer.Serialize(JNn 1, untaggedOptions))

    let adjacentTagNamedFieldsOptions = JsonSerializerOptions()

    adjacentTagNamedFieldsOptions.Converters.Add(
        JsonFSharpConverter(
            JsonUnionEncoding.AdjacentTag
            ||| JsonUnionEncoding.NamedFields
            ||| JsonUnionEncoding.AllowUnorderedTag
        )
    )

    [<Fact>]
    let ``deserialize AdjacentTag NamedFields`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"Case":"Ba"}""", adjacentTagNamedFieldsOptions))
        Assert.Equal(
            Bb 32,
            JsonSerializer.Deserialize("""{"Case":"Bb","Fields":{"Item":32}}""", adjacentTagNamedFieldsOptions)
        )
        Assert.Equal(
            Bc("test", true),
            JsonSerializer.Deserialize(
                """{"Case":"Bc","Fields":{"x":"test","Item2":true}}""",
                adjacentTagNamedFieldsOptions
            )
        )
        Assert.Equal(
            Bc("test", true),
            JsonSerializer.Deserialize(
                """{"Fields":{"x":"test","Item2":true},"Case":"Bc"}""",
                adjacentTagNamedFieldsOptions
            )
        )

    [<Fact>]
    let ``serialize AdjacentTag NamedFields`` () =
        Assert.Equal("""{"Case":"Ba"}""", JsonSerializer.Serialize(Ba, adjacentTagNamedFieldsOptions))
        Assert.Equal(
            """{"Case":"Bb","Fields":{"Item":32}}""",
            JsonSerializer.Serialize(Bb 32, adjacentTagNamedFieldsOptions)
        )
        Assert.Equal(
            """{"Case":"Bc","Fields":{"x":"test","Item2":true}}""",
            JsonSerializer.Serialize(Bc("test", true), adjacentTagNamedFieldsOptions)
        )

    let adjacentTagNamedFieldsTagPolicyOptions = JsonSerializerOptions()

    adjacentTagNamedFieldsTagPolicyOptions.Converters.Add(
        JsonFSharpConverter(
            JsonUnionEncoding.AdjacentTag
            ||| JsonUnionEncoding.NamedFields
            ||| JsonUnionEncoding.AllowUnorderedTag,
            unionTagNamingPolicy = JsonNamingPolicy.CamelCase
        )
    )

    [<Fact>]
    let ``deserialize AdjacentTag NamedFields with tag policy`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"Case":"ba"}""", adjacentTagNamedFieldsTagPolicyOptions))
        Assert.Equal(
            Bb 32,
            JsonSerializer.Deserialize("""{"Case":"bb","Fields":{"Item":32}}""", adjacentTagNamedFieldsTagPolicyOptions)
        )
        Assert.Equal(
            Bc("test", true),
            JsonSerializer.Deserialize(
                """{"Case":"bc","Fields":{"x":"test","Item2":true}}""",
                adjacentTagNamedFieldsTagPolicyOptions
            )
        )
        Assert.Equal(
            Bc("test", true),
            JsonSerializer.Deserialize(
                """{"Fields":{"x":"test","Item2":true},"Case":"bc"}""",
                adjacentTagNamedFieldsTagPolicyOptions
            )
        )

    [<Fact>]
    let ``serialize AdjacentTag NamedFields with tag policy`` () =
        Assert.Equal("""{"Case":"ba"}""", JsonSerializer.Serialize(Ba, adjacentTagNamedFieldsTagPolicyOptions))
        Assert.Equal(
            """{"Case":"bb","Fields":{"Item":32}}""",
            JsonSerializer.Serialize(Bb 32, adjacentTagNamedFieldsTagPolicyOptions)
        )
        Assert.Equal(
            """{"Case":"bc","Fields":{"x":"test","Item2":true}}""",
            JsonSerializer.Serialize(Bc("test", true), adjacentTagNamedFieldsTagPolicyOptions)
        )

    [<Fact>]
    let ``deserialize AdjacentTag NamedFields with JsonName`` () =
        Assert.Equal(
            JNs 1,
            JsonSerializer.Deserialize("""{"Case":"jstring","Fields":{"jnsField":1}}""", adjacentTagNamedFieldsOptions)
        )
        Assert.Equal(
            JNi 1,
            JsonSerializer.Deserialize("""{"Case":42,"Fields":{"jniField":1}}""", adjacentTagNamedFieldsOptions)
        )
        Assert.Equal(
            JNb 1,
            JsonSerializer.Deserialize("""{"Case":true,"Fields":{"jnbField":1}}""", adjacentTagNamedFieldsOptions)
        )
        Assert.Equal(
            JNb 1,
            JsonSerializer.Deserialize("""{"Case":"jbool","Fields":{"jnbField":1}}""", adjacentTagNamedFieldsOptions)
        )
        Assert.Equal(
            JNn 1,
            JsonSerializer.Deserialize("""{"Case":"JNn","Fields":{"jnnField":1}}""", adjacentTagNamedFieldsOptions)
        )

    [<Fact>]
    let ``serialize AdjacentTag NamedFields with JsonName`` () =
        Assert.Equal(
            """{"Case":"jstring","Fields":{"jnsField":1}}""",
            JsonSerializer.Serialize(JNs 1, adjacentTagNamedFieldsOptions)
        )
        Assert.Equal(
            """{"Case":42,"Fields":{"jniField":1}}""",
            JsonSerializer.Serialize(JNi 1, adjacentTagNamedFieldsOptions)
        )
        Assert.Equal(
            """{"Case":true,"Fields":{"jnbField":1}}""",
            JsonSerializer.Serialize(JNb 1, adjacentTagNamedFieldsOptions)
        )
        Assert.Equal(
            """{"Case":"JNn","Fields":{"jnnField":1}}""",
            JsonSerializer.Serialize(JNn 1, adjacentTagNamedFieldsOptions)
        )

    let externalTagNamedFieldsOptions = JsonSerializerOptions()

    externalTagNamedFieldsOptions.Converters.Add(
        JsonFSharpConverter(JsonUnionEncoding.ExternalTag ||| JsonUnionEncoding.NamedFields)
    )

    [<Fact>]
    let ``deserialize ExternalTag NamedFields`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"Ba":{}}""", externalTagNamedFieldsOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"Bb":{"Item":32}}""", externalTagNamedFieldsOptions))
        Assert.Equal(
            Bc("test", true),
            JsonSerializer.Deserialize("""{"Bc":{"x":"test","Item2":true}}""", externalTagNamedFieldsOptions)
        )

    [<Fact>]
    let ``serialize ExternalTag NamedFields`` () =
        Assert.Equal("""{"Ba":{}}""", JsonSerializer.Serialize(Ba, externalTagNamedFieldsOptions))
        Assert.Equal("""{"Bb":{"Item":32}}""", JsonSerializer.Serialize(Bb 32, externalTagNamedFieldsOptions))
        Assert.Equal(
            """{"Bc":{"x":"test","Item2":true}}""",
            JsonSerializer.Serialize(Bc("test", true), externalTagNamedFieldsOptions)
        )

    let externalTagNamedFieldsTagPolicyOptions = JsonSerializerOptions()

    externalTagNamedFieldsTagPolicyOptions.Converters.Add(
        JsonFSharpConverter(
            JsonUnionEncoding.ExternalTag ||| JsonUnionEncoding.NamedFields,
            unionTagNamingPolicy = JsonNamingPolicy.CamelCase
        )
    )

    [<Fact>]
    let ``deserialize ExternalTag NamedFields with tag policy`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"ba":{}}""", externalTagNamedFieldsTagPolicyOptions))
        Assert.Equal(
            Bb 32,
            JsonSerializer.Deserialize("""{"bb":{"Item":32}}""", externalTagNamedFieldsTagPolicyOptions)
        )
        Assert.Equal(
            Bc("test", true),
            JsonSerializer.Deserialize("""{"bc":{"x":"test","Item2":true}}""", externalTagNamedFieldsTagPolicyOptions)
        )

    [<Fact>]
    let ``serialize ExternalTag NamedFields with tag policy`` () =
        Assert.Equal("""{"ba":{}}""", JsonSerializer.Serialize(Ba, externalTagNamedFieldsTagPolicyOptions))
        Assert.Equal("""{"bb":{"Item":32}}""", JsonSerializer.Serialize(Bb 32, externalTagNamedFieldsTagPolicyOptions))
        Assert.Equal(
            """{"bc":{"x":"test","Item2":true}}""",
            JsonSerializer.Serialize(Bc("test", true), externalTagNamedFieldsTagPolicyOptions)
        )

    [<Fact>]
    let ``deserialize ExternalTag NamedFields with JsonName`` () =
        Assert.Equal(JNs 1, JsonSerializer.Deserialize("""{"jstring":{"jnsField":1}}""", externalTagNamedFieldsOptions))
        Assert.Equal(JNi 1, JsonSerializer.Deserialize("""{"42":{"jniField":1}}""", externalTagNamedFieldsOptions))
        Assert.Equal(JNb 1, JsonSerializer.Deserialize("""{"true":{"jnbField":1}}""", externalTagNamedFieldsOptions))
        Assert.Equal(JNb 1, JsonSerializer.Deserialize("""{"jbool":{"jnbField":1}}""", externalTagNamedFieldsOptions))
        Assert.Equal(JNn 1, JsonSerializer.Deserialize("""{"JNn":{"jnnField":1}}""", externalTagNamedFieldsOptions))

    [<Fact>]
    let ``serialize ExternalTag NamedFields with JsonName`` () =
        Assert.Equal("""{"jstring":{"jnsField":1}}""", JsonSerializer.Serialize(JNs 1, externalTagNamedFieldsOptions))
        Assert.Equal("""{"42":{"jniField":1}}""", JsonSerializer.Serialize(JNi 1, externalTagNamedFieldsOptions))
        Assert.Equal("""{"true":{"jnbField":1}}""", JsonSerializer.Serialize(JNb 1, externalTagNamedFieldsOptions))
        Assert.Equal("""{"JNn":{"jnnField":1}}""", JsonSerializer.Serialize(JNn 1, externalTagNamedFieldsOptions))

    let internalTagNamedFieldsOptions = JsonSerializerOptions()

    internalTagNamedFieldsOptions.Converters.Add(
        JsonFSharpConverter(
            JsonUnionEncoding.InternalTag
            ||| JsonUnionEncoding.NamedFields
            ||| JsonUnionEncoding.Default
        )
    )

    [<Fact>]
    let ``deserialize InternalTag NamedFields`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"Case":"Ba"}""", internalTagNamedFieldsOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"Case":"Bb","Item":32}""", internalTagNamedFieldsOptions))
        Assert.Equal(
            Bc("test", true),
            JsonSerializer.Deserialize("""{"Case":"Bc","x":"test","Item2":true}""", internalTagNamedFieldsOptions)
        )
        Assert.Equal(
            Bc("test", true),
            JsonSerializer.Deserialize("""{"x":"test","Item2":true,"Case":"Bc"}""", internalTagNamedFieldsOptions)
        )

    [<Fact>]
    let ``serialize InternalTag NamedFields`` () =
        Assert.Equal("""{"Case":"Ba"}""", JsonSerializer.Serialize(Ba, internalTagNamedFieldsOptions))
        Assert.Equal("""{"Case":"Bb","Item":32}""", JsonSerializer.Serialize(Bb 32, internalTagNamedFieldsOptions))
        Assert.Equal(
            """{"Case":"Bc","x":"test","Item2":true}""",
            JsonSerializer.Serialize(Bc("test", true), internalTagNamedFieldsOptions)
        )

    type Sk = S of a: int * b: Skippable<int> * c: Skippable<int option> * d: Skippable<int voption>

    [<Fact>]
    let ``deserialize InternalTag NamedFields with Skippable fields`` () =
        Assert.Equal(
            S(1, Skip, Skip, Skip),
            JsonSerializer.Deserialize("""{"Case":"S","a":1}""", internalTagNamedFieldsOptions)
        )
        Assert.Equal(
            S(1, Include 2, Include None, Include ValueNone),
            JsonSerializer.Deserialize("""{"Case":"S","a":1,"b":2,"c":null,"d":null}""", internalTagNamedFieldsOptions)
        )
        Assert.Equal(
            S(1, Include 2, Include(Some 3), Include(ValueSome 4)),
            JsonSerializer.Deserialize("""{"Case":"S","a":1,"b":2,"c":3,"d":4}""", internalTagNamedFieldsOptions)
        )
        Assert.Equal(
            S(1, Include 2, Include(Some 3), Include(ValueSome 4)),
            JsonSerializer.Deserialize("""{"a":1,"b":2,"Case":"S","c":3,"d":4}""", internalTagNamedFieldsOptions)
        )

    [<Fact>]
    let ``serialize InternalTag NamedFields with Skippable fields`` () =
        Assert.Equal(
            """{"Case":"S","a":1}""",
            JsonSerializer.Serialize(S(1, Skip, Skip, Skip), internalTagNamedFieldsOptions)
        )
        Assert.Equal(
            """{"Case":"S","a":1,"b":2,"c":null,"d":null}""",
            JsonSerializer.Serialize(S(1, Include 2, Include None, Include ValueNone), internalTagNamedFieldsOptions)
        )
        Assert.Equal(
            """{"Case":"S","a":1,"b":2,"c":3,"d":4}""",
            JsonSerializer.Serialize(
                S(1, Include 2, Include(Some 3), Include(ValueSome 4)),
                internalTagNamedFieldsOptions
            )
        )

    let internalTagNamedFieldsTagPolicyOptions = JsonSerializerOptions()

    internalTagNamedFieldsTagPolicyOptions.Converters.Add(
        JsonFSharpConverter(
            JsonUnionEncoding.InternalTag
            ||| JsonUnionEncoding.NamedFields
            ||| JsonUnionEncoding.AllowUnorderedTag,
            unionTagNamingPolicy = JsonNamingPolicy.CamelCase
        )
    )

    [<Fact>]
    let ``deserialize InternalTag NamedFields with tag policy`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"Case":"ba"}""", internalTagNamedFieldsTagPolicyOptions))
        Assert.Equal(
            Bb 32,
            JsonSerializer.Deserialize("""{"Case":"bb","Item":32}""", internalTagNamedFieldsTagPolicyOptions)
        )
        Assert.Equal(
            Bc("test", true),
            JsonSerializer.Deserialize(
                """{"Case":"bc","x":"test","Item2":true}""",
                internalTagNamedFieldsTagPolicyOptions
            )
        )
        Assert.Equal(
            Bc("test", true),
            JsonSerializer.Deserialize(
                """{"x":"test","Case":"bc","Item2":true}""",
                internalTagNamedFieldsTagPolicyOptions
            )
        )

    [<Fact>]
    let ``serialize InternalTag NamedFields with tag policy`` () =
        Assert.Equal("""{"Case":"ba"}""", JsonSerializer.Serialize(Ba, internalTagNamedFieldsTagPolicyOptions))
        Assert.Equal(
            """{"Case":"bb","Item":32}""",
            JsonSerializer.Serialize(Bb 32, internalTagNamedFieldsTagPolicyOptions)
        )
        Assert.Equal(
            """{"Case":"bc","x":"test","Item2":true}""",
            JsonSerializer.Serialize(Bc("test", true), internalTagNamedFieldsTagPolicyOptions)
        )

    [<Fact>]
    let ``deserialize InternalTag NamedFields with JsonName`` () =
        Assert.Equal(
            JNs 1,
            JsonSerializer.Deserialize("""{"Case":"jstring","jnsField":1}""", internalTagNamedFieldsOptions)
        )
        Assert.Equal(JNi 1, JsonSerializer.Deserialize("""{"Case":42,"jniField":1}""", internalTagNamedFieldsOptions))
        Assert.Equal(JNb 1, JsonSerializer.Deserialize("""{"Case":true,"jnbField":1}""", internalTagNamedFieldsOptions))
        Assert.Equal(
            JNb 1,
            JsonSerializer.Deserialize("""{"Case":"jbool","jnbField":1}""", internalTagNamedFieldsOptions)
        )
        Assert.Equal(
            JNn 1,
            JsonSerializer.Deserialize("""{"Case":"JNn","jnnField":1}""", internalTagNamedFieldsOptions)
        )

    [<Fact>]
    let ``serialize InternalTag NamedFields with JsonName`` () =
        Assert.Equal(
            """{"Case":"jstring","jnsField":1}""",
            JsonSerializer.Serialize(JNs 1, internalTagNamedFieldsOptions)
        )
        Assert.Equal("""{"Case":42,"jniField":1}""", JsonSerializer.Serialize(JNi 1, internalTagNamedFieldsOptions))
        Assert.Equal("""{"Case":true,"jnbField":1}""", JsonSerializer.Serialize(JNb 1, internalTagNamedFieldsOptions))
        Assert.Equal("""{"Case":"JNn","jnnField":1}""", JsonSerializer.Serialize(JNn 1, internalTagNamedFieldsOptions))

    let internalTagNamedFieldsConfiguredTagOptions = JsonSerializerOptions()

    internalTagNamedFieldsConfiguredTagOptions.Converters.Add(
        JsonFSharpConverter(
            JsonUnionEncoding.InternalTag
            ||| JsonUnionEncoding.NamedFields
            ||| JsonUnionEncoding.AllowUnorderedTag,
            "type"
        )
    )

    [<Fact>]
    let ``deserialize InternalTag NamedFields alternative Tag`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"type":"Ba"}""", internalTagNamedFieldsConfiguredTagOptions))
        Assert.Equal(
            Bb 32,
            JsonSerializer.Deserialize("""{"type":"Bb","Item":32}""", internalTagNamedFieldsConfiguredTagOptions)
        )
        Assert.Equal(
            Bc("test", true),
            JsonSerializer.Deserialize(
                """{"type":"Bc","x":"test","Item2":true}""",
                internalTagNamedFieldsConfiguredTagOptions
            )
        )
        Assert.Equal(
            Bc("test", true),
            JsonSerializer.Deserialize(
                """{"x":"test","type":"Bc","Item2":true}""",
                internalTagNamedFieldsConfiguredTagOptions
            )
        )

    [<Fact>]
    let ``serialize InternalTag NamedFields alternative Tag`` () =
        Assert.Equal("""{"type":"Ba"}""", JsonSerializer.Serialize(Ba, internalTagNamedFieldsConfiguredTagOptions))
        Assert.Equal(
            """{"type":"Bb","Item":32}""",
            JsonSerializer.Serialize(Bb 32, internalTagNamedFieldsConfiguredTagOptions)
        )
        Assert.Equal(
            """{"type":"Bc","x":"test","Item2":true}""",
            JsonSerializer.Serialize(Bc("test", true), internalTagNamedFieldsConfiguredTagOptions)
        )

    let adjacentTagNamedFieldsConfiguredFieldsOptions = JsonSerializerOptions()

    adjacentTagNamedFieldsConfiguredFieldsOptions.Converters.Add(
        JsonFSharpConverter(JsonUnionEncoding.AdjacentTag ||| JsonUnionEncoding.AllowUnorderedTag, "type", "args")
    )

    [<Fact>]
    let ``deserialize AdjacentTag NamedFields alternative Fields`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"type":"Ba"}""", adjacentTagNamedFieldsConfiguredFieldsOptions))
        Assert.Equal(
            Bb 32,
            JsonSerializer.Deserialize("""{"type":"Bb","args":[32]}""", adjacentTagNamedFieldsConfiguredFieldsOptions)
        )
        Assert.Equal(
            Bc("test", true),
            JsonSerializer.Deserialize(
                """{"type":"Bc","args":["test",true]}""",
                adjacentTagNamedFieldsConfiguredFieldsOptions
            )
        )
        Assert.Equal(
            Bc("test", true),
            JsonSerializer.Deserialize(
                """{"args":["test",true],"type":"Bc"}""",
                adjacentTagNamedFieldsConfiguredFieldsOptions
            )
        )

    [<Fact>]
    let ``serialize AdjacentTag NamedFields alternative Fields`` () =
        Assert.Equal("""{"type":"Ba"}""", JsonSerializer.Serialize(Ba, adjacentTagNamedFieldsConfiguredFieldsOptions))
        Assert.Equal(
            """{"type":"Bb","args":[32]}""",
            JsonSerializer.Serialize(Bb 32, adjacentTagNamedFieldsConfiguredFieldsOptions)
        )
        Assert.Equal(
            """{"type":"Bc","args":["test",true]}""",
            JsonSerializer.Serialize(Bc("test", true), adjacentTagNamedFieldsConfiguredFieldsOptions)
        )

    let unwrapSingleFieldCasesOptions = JsonSerializerOptions()

    unwrapSingleFieldCasesOptions.Converters.Add(
        JsonFSharpConverter(JsonUnionEncoding.Default ||| JsonUnionEncoding.UnwrapSingleFieldCases)
    )

    [<Fact>]
    let ``deserialize unwrapped single-field cases`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"Case":"Ba"}""", unwrapSingleFieldCasesOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"Case":"Bb","Fields":32}""", unwrapSingleFieldCasesOptions))
        Assert.Equal(
            Bc("test", true),
            JsonSerializer.Deserialize("""{"Case":"Bc","Fields":["test",true]}""", unwrapSingleFieldCasesOptions)
        )

    [<Fact>]
    let ``serialize unwrapped single-field cases`` () =
        Assert.Equal("""{"Case":"Ba"}""", JsonSerializer.Serialize(Ba, unwrapSingleFieldCasesOptions))
        Assert.Equal("""{"Case":"Bb","Fields":32}""", JsonSerializer.Serialize(Bb 32, unwrapSingleFieldCasesOptions))
        Assert.Equal(
            """{"Case":"Bc","Fields":["test",true]}""",
            JsonSerializer.Serialize(Bc("test", true), unwrapSingleFieldCasesOptions)
        )

    type O = { o: option<int> }

    [<Fact>]
    let ``deserialize UnwrapOption`` () =
        Assert.Equal("""{"o":123}""", JsonSerializer.Serialize({ o = Some 123 }, options))
        Assert.Equal("""{"o":null}""", JsonSerializer.Serialize({ o = None }, options))

    [<Fact>]
    let ``serialize UnwrapOption`` () =
        Assert.Equal({ o = Some 123 }, JsonSerializer.Deserialize("""{"o":123}""", options))
        Assert.Equal({ o = None }, JsonSerializer.Deserialize("""{"o":null}""", options))

    let unwrapFieldlessTagsOptions = JsonSerializerOptions()
    unwrapFieldlessTagsOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.UnwrapFieldlessTags))

    type S = { b: B }

    [<Fact>]
    let ``deserialize UnwrapFieldlessTags`` () =
        Assert.Equal({ b = Ba }, JsonSerializer.Deserialize("""{"b":"Ba"}""", unwrapFieldlessTagsOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"Case":"Bb","Fields":[32]}""", unwrapFieldlessTagsOptions))
        Assert.Equal(
            Bc("test", true),
            JsonSerializer.Deserialize("""{"Case":"Bc","Fields":["test",true]}""", unwrapFieldlessTagsOptions)
        )

    [<Fact>]
    let ``serialize UnwrapFieldlessTags`` () =
        Assert.Equal("""{"b":"Ba"}""", JsonSerializer.Serialize({ b = Ba }, unwrapFieldlessTagsOptions))
        Assert.Equal("""{"Case":"Bb","Fields":[32]}""", JsonSerializer.Serialize(Bb 32, unwrapFieldlessTagsOptions))
        Assert.Equal(
            """{"Case":"Bc","Fields":["test",true]}""",
            JsonSerializer.Serialize(Bc("test", true), unwrapFieldlessTagsOptions)
        )

    let unwrapFieldlessTagsTagPolicyOptions = JsonSerializerOptions()

    unwrapFieldlessTagsTagPolicyOptions.Converters.Add(
        JsonFSharpConverter(JsonUnionEncoding.UnwrapFieldlessTags, unionTagNamingPolicy = JsonNamingPolicy.CamelCase)
    )

    [<Fact>]
    let ``deserialize UnwrapFieldlessTags with tag policy`` () =
        Assert.Equal({ b = Ba }, JsonSerializer.Deserialize("""{"b":"ba"}""", unwrapFieldlessTagsTagPolicyOptions))
        Assert.Equal(
            Bb 32,
            JsonSerializer.Deserialize("""{"Case":"bb","Fields":[32]}""", unwrapFieldlessTagsTagPolicyOptions)
        )
        Assert.Equal(
            Bc("test", true),
            JsonSerializer.Deserialize("""{"Case":"bc","Fields":["test",true]}""", unwrapFieldlessTagsTagPolicyOptions)
        )

    [<Fact>]
    let ``serialize UnwrapFieldlessTags with tag policy`` () =
        Assert.Equal("""{"b":"ba"}""", JsonSerializer.Serialize({ b = Ba }, unwrapFieldlessTagsTagPolicyOptions))
        Assert.Equal(
            """{"Case":"bb","Fields":[32]}""",
            JsonSerializer.Serialize(Bb 32, unwrapFieldlessTagsTagPolicyOptions)
        )
        Assert.Equal(
            """{"Case":"bc","Fields":["test",true]}""",
            JsonSerializer.Serialize(Bc("test", true), unwrapFieldlessTagsTagPolicyOptions)
        )

    type UnionWithPropertyNames =
        | [<JsonPropertyName "nullary">] NamedNullary
        | [<JsonPropertyName "withArgs">] NamedWithArgs of int

    [<Fact>]
    let ``deserialize with JsonPropertyName on case`` () =
        Assert.Equal(NamedNullary, JsonSerializer.Deserialize("\"nullary\"", unwrapFieldlessTagsOptions))
        Assert.Equal(
            NamedWithArgs 42,
            JsonSerializer.Deserialize("""{"Case":"withArgs","Fields":[42]}""", unwrapFieldlessTagsOptions)
        )

    [<Fact>]
    let ``serialize with JsonPropertyName on case`` () =
        Assert.Equal("\"nullary\"", JsonSerializer.Serialize(NamedNullary, unwrapFieldlessTagsOptions))
        Assert.Equal(
            """{"Case":"withArgs","Fields":[42]}""",
            JsonSerializer.Serialize(NamedWithArgs 42, unwrapFieldlessTagsOptions)
        )

    let nonUnwrapOptionOptions = JsonSerializerOptions()
    nonUnwrapOptionOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.AdjacentTag))

    [<Fact>]
    let ``serialize non-UnwrapOption`` () =
        Assert.Equal(
            """{"o":{"Case":"Some","Fields":[123]}}""",
            JsonSerializer.Serialize({ o = Some 123 }, nonUnwrapOptionOptions)
        )
        Assert.Equal("""{"o":null}""", JsonSerializer.Serialize({ o = None }, nonUnwrapOptionOptions))

    [<Fact>]
    let ``deserialize non-UnwrapOption`` () =
        Assert.Equal(
            { o = Some 123 },
            JsonSerializer.Deserialize("""{"o":{"Case":"Some","Fields":[123]}}""", nonUnwrapOptionOptions)
        )
        Assert.Equal({ o = None }, JsonSerializer.Deserialize("""{"o":null}""", nonUnwrapOptionOptions))

    let ignoreNullOptions = JsonSerializerOptions(IgnoreNullValues = true)

    ignoreNullOptions.Converters.Add(
        JsonFSharpConverter(JsonUnionEncoding.InternalTag ||| JsonUnionEncoding.NamedFields)
    )

    let newIgnoreNullOptions =
        JsonSerializerOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)

    newIgnoreNullOptions.Converters.Add(
        JsonFSharpConverter(JsonUnionEncoding.InternalTag ||| JsonUnionEncoding.NamedFields)
    )

    [<AllowNullLiteral>]
    type Cls() =
        class
        end

    type UnionWithNullableArgument = Foo of x: int * y: Cls

    [<Fact>]
    let ``deserialize with IgnoreNullValues`` () =
        let actual =
            JsonSerializer.Deserialize("""{"Case":"Foo","x":1}""", ignoreNullOptions)
        Assert.Equal(Foo(1, null), actual)

    [<Fact>]
    let ``serialize with IgnoreNullValues`` () =
        let actual = JsonSerializer.Serialize(Foo(1, null), ignoreNullOptions)
        Assert.Equal("""{"Case":"Foo","x":1}""", actual)

    [<Fact>]
    let ``deserialize with JsonIgnoreCondition.WhenWritingNull`` () =
        let actual =
            JsonSerializer.Deserialize("""{"Case":"Foo","x":1}""", newIgnoreNullOptions)
        Assert.Equal(Foo(1, null), actual)

    [<Fact>]
    let ``serialize with JsonIgnoreCondition.WhenWritingNull`` () =
        let actual = JsonSerializer.Serialize(Foo(1, null), newIgnoreNullOptions)
        Assert.Equal("""{"Case":"Foo","x":1}""", actual)

    let propertyNamingPolicyOptions =
        JsonSerializerOptions(PropertyNamingPolicy = JsonNamingPolicy.CamelCase)

    propertyNamingPolicyOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.Untagged))

    type CamelCase = CCA of CcFirst: int * CcSecond: string

    [<Fact>]
    let ``deserialize with property naming policy`` () =
        let actual =
            JsonSerializer.Deserialize("""{"ccFirst":1,"ccSecond":"a"}""", propertyNamingPolicyOptions)
        Assert.Equal(CCA(1, "a"), actual)

    [<Fact>]
    let ``serialize with property naming policy`` () =
        let actual = JsonSerializer.Serialize(CCA(1, "a"), propertyNamingPolicyOptions)
        Assert.Equal("""{"ccFirst":1,"ccSecond":"a"}""", actual)

    let propertyNameCaseInsensitiveOptions =
        JsonSerializerOptions(PropertyNameCaseInsensitive = true)

    propertyNameCaseInsensitiveOptions.Converters.Add(
        JsonFSharpConverter(JsonUnionEncoding.InternalTag ||| JsonUnionEncoding.NamedFields)
    )

    [<Fact>]
    let ``deserialize with property case insensitive`` () =
        let actual =
            JsonSerializer.Deserialize(
                """{"Case":"CCA","cCfIrSt":1,"cCsEcOnD":"a"}""",
                propertyNameCaseInsensitiveOptions
            )
        Assert.Equal(CCA(1, "a"), actual)

    [<Fact>]
    let ``serialize with property case insensitive`` () =
        let actual =
            JsonSerializer.Serialize(CCA(1, "a"), propertyNameCaseInsensitiveOptions)
        Assert.Equal("""{"Case":"CCA","CcFirst":1,"CcSecond":"a"}""", actual)

    let propertyNameCaseInsensitiveUntaggedOptions =
        JsonSerializerOptions(PropertyNameCaseInsensitive = true)

    propertyNameCaseInsensitiveUntaggedOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.Untagged))

    [<Fact>]
    let ``deserialize untagged with property case insensitive`` () =
        let actual =
            JsonSerializer.Deserialize("""{"cCfIrSt":1,"cCsEcOnD":"a"}""", propertyNameCaseInsensitiveUntaggedOptions)
        Assert.Equal(CCA(1, "a"), actual)

    [<Fact>]
    let ``serialize untagged with property case insensitive`` () =
        let actual =
            JsonSerializer.Serialize(CCA(1, "a"), propertyNameCaseInsensitiveUntaggedOptions)
        Assert.Equal("""{"CcFirst":1,"CcSecond":"a"}""", actual)

    type Unwrapped = Unwrapped of string

    [<Fact>]
    let ``deserialize unwrapped single-case`` () =
        Assert.Equal(Unwrapped "foo", JsonSerializer.Deserialize("\"foo\"", options))

    [<Fact>]
    let ``serialize unwrapped single-case`` () =
        Assert.Equal("\"foo\"", JsonSerializer.Serialize(Unwrapped "foo", options))

    let noNewtypeOptions = JsonSerializerOptions()

    noNewtypeOptions.Converters.Add(
        JsonFSharpConverter(JsonUnionEncoding.Default &&& ~~~JsonUnionEncoding.UnwrapSingleCaseUnions)
    )

    [<Fact>]
    let ``deserialize non-unwrapped single-case`` () =
        Assert.Equal(
            Unwrapped "foo",
            JsonSerializer.Deserialize("""{"Case":"Unwrapped","Fields":["foo"]}""", noNewtypeOptions)
        )

    [<Fact>]
    let ``serialize non-unwrapped single-case`` () =
        Assert.Equal(
            """{"Case":"Unwrapped","Fields":["foo"]}""",
            JsonSerializer.Serialize(Unwrapped "foo", noNewtypeOptions)
        )

    type UnionWithUnitField = UWUF of int * unit

    [<Fact>]
    let ``serializes union with unit field`` () =
        let options =
            JsonSerializerOptions(PropertyNamingPolicy = JsonNamingPolicy.CamelCase)
        options.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.AdjacentTag))
        let actual = JsonSerializer.Serialize(UWUF(42, ()), options)
        Assert.Equal("""{"Case":"UWUF","Fields":[42,null]}""", actual)

    [<Fact>]
    let ``deserializes union with unit field`` () =
        let options =
            JsonSerializerOptions(PropertyNamingPolicy = JsonNamingPolicy.CamelCase)
        options.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.AdjacentTag))
        let actual =
            JsonSerializer.Deserialize("""{"Case":"UWUF","Fields":[42,null]}""", options)
        Assert.Equal(UWUF(42, ()), actual)

    module UnwrapRecord =

        type U =
            | U of R
            | V of v: int

        and R = { x: int; y: float }

        let adjacentTagOptions = JsonSerializerOptions()

        adjacentTagOptions.Converters.Add(
            JsonFSharpConverter(JsonUnionEncoding.AdjacentTag ||| JsonUnionEncoding.UnwrapRecordCases)
        )

        [<Fact>]
        let ``serialize with AdjacentTag`` () =
            let actual = JsonSerializer.Serialize(U { x = 1; y = 2. }, adjacentTagOptions)
            Assert.Equal("""{"Case":"U","Fields":{"x":1,"y":2}}""", actual)
            let actual = JsonSerializer.Serialize(V 42, adjacentTagOptions)
            Assert.Equal("""{"Case":"V","Fields":{"v":42}}""", actual)

        [<Fact>]
        let ``deserialize with AdjacentTag`` () =
            let actual =
                JsonSerializer.Deserialize("""{"Case":"U","Fields":{"x":1,"y":2}}""", adjacentTagOptions)
            Assert.Equal(U { x = 1; y = 2. }, actual)
            let actual =
                JsonSerializer.Deserialize("""{"Case":"V","Fields":{"v":42}}""", adjacentTagOptions)
            Assert.Equal(V 42, actual)

        let externalTagOptions = JsonSerializerOptions()

        externalTagOptions.Converters.Add(
            JsonFSharpConverter(JsonUnionEncoding.ExternalTag ||| JsonUnionEncoding.UnwrapRecordCases)
        )

        [<Fact>]
        let ``serialize with externalTag`` () =
            let actual = JsonSerializer.Serialize(U { x = 1; y = 2. }, externalTagOptions)
            Assert.Equal("""{"U":{"x":1,"y":2}}""", actual)
            let actual = JsonSerializer.Serialize(V 42, externalTagOptions)
            Assert.Equal("""{"V":{"v":42}}""", actual)

        [<Fact>]
        let ``deserialize with externalTag`` () =
            let actual =
                JsonSerializer.Deserialize("""{"U":{"x":1,"y":2}}""", externalTagOptions)
            Assert.Equal(U { x = 1; y = 2. }, actual)
            let actual = JsonSerializer.Deserialize("""{"V":{"v":42}}""", externalTagOptions)
            Assert.Equal(V 42, actual)

        let internalTagOptions = JsonSerializerOptions()

        internalTagOptions.Converters.Add(
            JsonFSharpConverter(JsonUnionEncoding.InternalTag ||| JsonUnionEncoding.UnwrapRecordCases)
        )

        [<Fact>]
        let ``serialize with internalTag`` () =
            let actual = JsonSerializer.Serialize(U { x = 1; y = 2. }, internalTagOptions)
            Assert.Equal("""{"Case":"U","x":1,"y":2}""", actual)
            let actual = JsonSerializer.Serialize(V 42, internalTagOptions)
            Assert.Equal("""{"Case":"V","v":42}""", actual)

        [<Fact>]
        let ``deserialize with internalTag`` () =
            let actual =
                JsonSerializer.Deserialize("""{"Case":"U","x":1,"y":2}""", internalTagOptions)
            Assert.Equal(U { x = 1; y = 2. }, actual)
            let actual =
                JsonSerializer.Deserialize("""{"Case":"V","v":42}""", internalTagOptions)
            Assert.Equal(V 42, actual)

        let untaggedOptions = JsonSerializerOptions()

        untaggedOptions.Converters.Add(
            JsonFSharpConverter(JsonUnionEncoding.Untagged ||| JsonUnionEncoding.UnwrapRecordCases)
        )

        [<Fact>]
        let ``serialize with untagged`` () =
            let actual = JsonSerializer.Serialize(U { x = 1; y = 2. }, untaggedOptions)
            Assert.Equal("""{"x":1,"y":2}""", actual)
            let actual = JsonSerializer.Serialize(V 42, untaggedOptions)
            Assert.Equal("""{"v":42}""", actual)

        [<Fact>]
        let ``deserialize with untagged`` () =
            let actual = JsonSerializer.Deserialize("""{"x":1,"y":2}""", untaggedOptions)
            Assert.Equal(U { x = 1; y = 2. }, actual)
            let actual = JsonSerializer.Deserialize("""{"v":42}""", untaggedOptions)
            Assert.Equal(V 42, actual)

    [<JsonFSharpConverter(unionTagName = "tag", unionFieldsName = "val")>]
    type Override = A of x: int * y: string

    [<Fact>]
    let ``should not override tag name in attribute if AllowOverride = false`` () =
        let o = JsonSerializerOptions()
        o.Converters.Add(JsonFSharpConverter())
        Assert.Equal("""{"Case":"A","Fields":[123,"abc"]}""", JsonSerializer.Serialize(Override.A(123, "abc"), o))

    [<Fact>]
    let ``should override tag name in attribute if AllowOverride = true`` () =
        let o = JsonSerializerOptions()
        o.Converters.Add(JsonFSharpConverter(allowOverride = true))
        Assert.Equal("""{"tag":"A","val":[123,"abc"]}""", JsonSerializer.Serialize(Override.A(123, "abc"), o))

    [<Fact>]
    let ``should not override JsonEncoding if not specified`` () =
        let o = JsonSerializerOptions()
        o.Converters.Add(
            JsonFSharpConverter(JsonUnionEncoding.InternalTag ||| JsonUnionEncoding.NamedFields, allowOverride = true)
        )
        Assert.Equal("""{"tag":"A","x":123,"y":"abc"}""", JsonSerializer.Serialize(Override.A(123, "abc"), o))

    [<JsonFSharpConverter(JsonUnionEncoding.InternalTag)>]
    type Override2 = A of int * string

    [<Fact>]
    let ``should override JsonEncoding if specified`` () =
        let o = JsonSerializerOptions()
        o.Converters.Add(JsonFSharpConverter(allowOverride = true))
        Assert.Equal("""["A",123,"abc"]""", JsonSerializer.Serialize(Override2.A(123, "abc"), o))

    [<Fact>]
    let ``should apply explicit overrides if allowOverride = false`` () =
        let o = JsonSerializerOptions()
        o.Converters.Add(
            JsonFSharpConverter(overrides = dict [ typeof<Override>, JsonFSharpOptions(JsonUnionEncoding.InternalTag) ])
        )
        Assert.Equal("""["A",123,"abc"]""", JsonSerializer.Serialize(Override.A(123, "abc"), o))

    [<Fact>]
    let ``should apply explicit overrides if allowOverride = true`` () =
        let o = JsonSerializerOptions()
        o.Converters.Add(
            JsonFSharpConverter(
                allowOverride = true,
                overrides = dict [ typeof<Override>, JsonFSharpOptions(JsonUnionEncoding.InternalTag) ]
            )
        )
        Assert.Equal("""["A",123,"abc"]""", JsonSerializer.Serialize(Override.A(123, "abc"), o))

    [<Fact>]
    let ``should apply explicit overrides inheriting JsonUnionEncoding`` () =
        let o = JsonSerializerOptions()
        o.Converters.Add(
            JsonFSharpConverter(
                JsonUnionEncoding.InternalTag ||| JsonUnionEncoding.NamedFields,
                overrides = dict [ typeof<Override>, JsonFSharpOptions(unionTagName = "tag") ]
            )
        )
        Assert.Equal("""{"tag":"A","x":123,"y":"abc"}""", JsonSerializer.Serialize(Override.A(123, "abc"), o))

    type NamedAfterTypes =
        | NTA of int
        | NTB of int * string
        | NTC of X: int
        | NTD of X: int * Y: string
        | NTE of string * string

    let namedAfterTypesOptions = JsonSerializerOptions()

    namedAfterTypesOptions.Converters.Add(
        JsonFSharpConverter(
            JsonUnionEncoding.InternalTag
            ||| JsonUnionEncoding.NamedFields
            ||| JsonUnionEncoding.UnionFieldNamesFromTypes
        )
    )

    [<Fact>]
    let ``serialize UnionFieldNamesFromTypes`` () =
        Assert.Equal("""{"Case":"NTA","Int32":123}""", JsonSerializer.Serialize(NTA 123, namedAfterTypesOptions))
        Assert.Equal(
            """{"Case":"NTB","Int32":123,"String":"test"}""",
            JsonSerializer.Serialize(NTB(123, "test"), namedAfterTypesOptions)
        )
        Assert.Equal("""{"Case":"NTC","X":123}""", JsonSerializer.Serialize(NTC 123, namedAfterTypesOptions))
        Assert.Equal(
            """{"Case":"NTD","X":123,"Y":"test"}""",
            JsonSerializer.Serialize(NTD(123, "test"), namedAfterTypesOptions)
        )
        Assert.Equal(
            """{"Case":"NTE","String1":"123","String2":"test"}""",
            JsonSerializer.Serialize(NTE("123", "test"), namedAfterTypesOptions)
        )

    [<Fact>]
    let ``deserialize UnionFieldNamesFromTypes`` () =
        Assert.Equal(NTA 123, JsonSerializer.Deserialize("""{"Case":"NTA","Int32":123}""", namedAfterTypesOptions))
        Assert.Equal(
            NTB(123, "test"),
            JsonSerializer.Deserialize("""{"Case":"NTB","Int32":123,"String":"test"}""", namedAfterTypesOptions)
        )
        Assert.Equal(NTC 123, JsonSerializer.Deserialize("""{"Case":"NTC","X":123}""", namedAfterTypesOptions))
        Assert.Equal(
            NTD(123, "test"),
            JsonSerializer.Deserialize("""{"Case":"NTD","X":123,"Y":"test"}""", namedAfterTypesOptions)
        )
        Assert.Equal(
            NTE("123", "test"),
            JsonSerializer.Deserialize("""{"Case":"NTE","String1":"123","String2":"test"}""", namedAfterTypesOptions)
        )

    let namedAfterTypesOptionsWithNamingPolicy = JsonSerializerOptions()

    namedAfterTypesOptionsWithNamingPolicy.Converters.Add(
        JsonFSharpConverter(
            JsonUnionEncoding.InternalTag
            ||| JsonUnionEncoding.NamedFields
            ||| JsonUnionEncoding.UnionFieldNamesFromTypes,
            unionFieldNamingPolicy = JsonNamingPolicy.CamelCase
        )
    )

    [<Fact>]
    let ``serialize UnionFieldNamesFromTypes with unionFieldNamingPolicy`` () =
        Assert.Equal(
            """{"Case":"NTA","int32":123}""",
            JsonSerializer.Serialize(NTA 123, namedAfterTypesOptionsWithNamingPolicy)
        )
        Assert.Equal(
            """{"Case":"NTB","int32":123,"string":"test"}""",
            JsonSerializer.Serialize(NTB(123, "test"), namedAfterTypesOptionsWithNamingPolicy)
        )
        Assert.Equal(
            """{"Case":"NTC","x":123}""",
            JsonSerializer.Serialize(NTC 123, namedAfterTypesOptionsWithNamingPolicy)
        )
        Assert.Equal(
            """{"Case":"NTD","x":123,"y":"test"}""",
            JsonSerializer.Serialize(NTD(123, "test"), namedAfterTypesOptionsWithNamingPolicy)
        )
        Assert.Equal(
            """{"Case":"NTE","string1":"123","string2":"test"}""",
            JsonSerializer.Serialize(NTE("123", "test"), namedAfterTypesOptionsWithNamingPolicy)
        )

    [<Fact>]
    let ``deserialize UnionFieldNamesFromTypes with unionFieldNamingPolicy`` () =
        Assert.Equal(
            NTA 123,
            JsonSerializer.Deserialize("""{"Case":"NTA","int32":123}""", namedAfterTypesOptionsWithNamingPolicy)
        )
        Assert.Equal(
            NTB(123, "test"),
            JsonSerializer.Deserialize(
                """{"Case":"NTB","int32":123,"string":"test"}""",
                namedAfterTypesOptionsWithNamingPolicy
            )
        )
        Assert.Equal(
            NTC 123,
            JsonSerializer.Deserialize("""{"Case":"NTC","x":123}""", namedAfterTypesOptionsWithNamingPolicy)
        )
        Assert.Equal(
            NTD(123, "test"),
            JsonSerializer.Deserialize("""{"Case":"NTD","x":123,"y":"test"}""", namedAfterTypesOptionsWithNamingPolicy)
        )
        Assert.Equal(
            NTE("123", "test"),
            JsonSerializer.Deserialize(
                """{"Case":"NTE","string1":"123","string2":"test"}""",
                namedAfterTypesOptionsWithNamingPolicy
            )
        )

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
        Assert.Equal(Ac("test", true), JsonSerializer.Deserialize """{"Fields":["test",true],"Case":"Ac"}""")

    [<Fact>]
    let ``serialize via explicit converter`` () =
        Assert.Equal("""{"Case":"Aa"}""", JsonSerializer.Serialize Aa)
        Assert.Equal("""{"Case":"Ab","Fields":[32]}""", JsonSerializer.Serialize(Ab 32))
        Assert.Equal("""{"Case":"Ac","Fields":["test",true]}""", JsonSerializer.Serialize(Ac("test", true)))

    [<Struct>]
    type B =
        | Ba
        | Bb of int
        | Bc of x: string * bool

    type JN =
        | [<JsonName "jstring">] JNs of jnsField: int
        | [<JsonName 42>] JNi of jniField: int
        | [<JsonName true; JsonName "jbool">] JNb of jnbField: int
        | JNn of jnnField: int

    let options = JsonSerializerOptions()
    options.Converters.Add(JsonFSharpConverter())

    [<Fact>]
    let ``deserialize via options`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"Case":"Ba"}""", options))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"Case":"Bb","Fields":[32]}""", options))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""{"Case":"Bc","Fields":["test",true]}""", options))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""{"Fields":["test",true],"Case":"Bc"}""", options))

    [<Fact>]
    let ``serialize via options`` () =
        Assert.Equal("""{"Case":"Ba"}""", JsonSerializer.Serialize(Ba, options))
        Assert.Equal("""{"Case":"Bb","Fields":[32]}""", JsonSerializer.Serialize(Bb 32, options))
        Assert.Equal("""{"Case":"Bc","Fields":["test",true]}""", JsonSerializer.Serialize(Bc("test", true), options))

    [<Fact>]
    let ``not fill in nulls`` () =
        try
            JsonSerializer.Deserialize<B>("""{"Case":"Bc","Fields":[null,true]}""", options)
            |> ignore
            failwith "Deserialization was supposed to fail on the line above"
        with :? JsonException as e ->
            Assert.Equal("B.Bc(x) was expected to be of type String, but was null.", e.Message)

    [<Fact>]
    let allowNullFields () =
        let options = JsonSerializerOptions()
        options.Converters.Add(JsonFSharpConverter(allowNullFields = true))
        let actual =
            JsonSerializer.Deserialize("""{"Case":"Bc","Fields":[null,true]}""", options)
        Assert.Equal(Bc(null, true), actual)

    [<Struct>]
    type VO = { vo: voption<int> }

    [<Fact>]
    let ``deserialize voption with UnwrapOption`` () =
        Assert.Equal("""{"vo":123}""", JsonSerializer.Serialize({ vo = ValueSome 123 }, options))
        Assert.Equal("""{"vo":null}""", JsonSerializer.Serialize({ vo = ValueNone }, options))

    [<Fact>]
    let ``serialize voption with UnwrapOption`` () =
        Assert.Equal({ vo = ValueSome 123 }, JsonSerializer.Deserialize("""{"vo":123}""", options))
        Assert.Equal({ vo = ValueNone }, JsonSerializer.Deserialize("""{"vo":null}""", options))

    let tagPolicyOptions = JsonSerializerOptions()
    tagPolicyOptions.Converters.Add(JsonFSharpConverter(unionTagNamingPolicy = JsonNamingPolicy.CamelCase))

    [<Fact>]
    let ``deserialize AdjacentTag with tag policy`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"Case":"ba"}""", tagPolicyOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"Case":"bb","Fields":[32]}""", tagPolicyOptions))
        Assert.Equal(
            Bc("test", true),
            JsonSerializer.Deserialize("""{"Case":"bc","Fields":["test",true]}""", tagPolicyOptions)
        )
        Assert.Equal(
            Bc("test", true),
            JsonSerializer.Deserialize("""{"Fields":["test",true],"Case":"bc"}""", tagPolicyOptions)
        )

    [<Fact>]
    let ``serialize AdjacentTag with tag policy`` () =
        Assert.Equal("""{"Case":"ba"}""", JsonSerializer.Serialize(Ba, tagPolicyOptions))
        Assert.Equal("""{"Case":"bb","Fields":[32]}""", JsonSerializer.Serialize(Bb 32, tagPolicyOptions))
        Assert.Equal(
            """{"Case":"bc","Fields":["test",true]}""",
            JsonSerializer.Serialize(Bc("test", true), tagPolicyOptions)
        )

    let tagCaseInsensitiveOptions = JsonSerializerOptions()
    tagCaseInsensitiveOptions.Converters.Add(JsonFSharpConverter(unionTagCaseInsensitive = true))

    [<Fact>]
    let ``deserialize AdjacentTag with case insensitive tag`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"Case":"bA"}""", tagCaseInsensitiveOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"Case":"bB", "Fields":[32]}""", tagCaseInsensitiveOptions))
        Assert.Equal(
            Bc("test", true),
            JsonSerializer.Deserialize("""{"Case":"bC", "Fields":["test",true]}""", tagCaseInsensitiveOptions)
        )
        Assert.Equal(
            Bc("test", true),
            JsonSerializer.Deserialize("""{"Fields":["test",true], "Case":"bC"}""", tagCaseInsensitiveOptions)
        )

    [<Fact>]
    let ``serialize AdjacentTag with case insensitive tag`` () =
        Assert.Equal("""{"Case":"Ba"}""", JsonSerializer.Serialize(Ba, tagCaseInsensitiveOptions))
        Assert.Equal("""{"Case":"Bb","Fields":[32]}""", JsonSerializer.Serialize(Bb 32, tagCaseInsensitiveOptions))
        Assert.Equal(
            """{"Case":"Bc","Fields":["test",true]}""",
            JsonSerializer.Serialize(Bc("test", true), tagCaseInsensitiveOptions)
        )

    [<Fact>]
    let ``deserialize AdjacentTag with JsonName`` () =
        Assert.Equal(JNs 1, JsonSerializer.Deserialize("""{"Case":"jstring","Fields":[1]}""", options))
        Assert.Equal(JNi 1, JsonSerializer.Deserialize("""{"Case":42,"Fields":[1]}""", options))
        Assert.Equal(JNb 1, JsonSerializer.Deserialize("""{"Case":true,"Fields":[1]}""", options))
        Assert.Equal(JNb 1, JsonSerializer.Deserialize("""{"Case":"jbool","Fields":[1]}""", options))
        Assert.Equal(JNn 1, JsonSerializer.Deserialize("""{"Case":"JNn","Fields":[1]}""", options))

    [<Fact>]
    let ``serialize AdjacentTag with JsonName`` () =
        Assert.Equal("""{"Case":"jstring","Fields":[1]}""", JsonSerializer.Serialize(JNs 1, options))
        Assert.Equal("""{"Case":42,"Fields":[1]}""", JsonSerializer.Serialize(JNi 1, options))
        Assert.Equal("""{"Case":true,"Fields":[1]}""", JsonSerializer.Serialize(JNb 1, options))
        Assert.Equal("""{"Case":"JNn","Fields":[1]}""", JsonSerializer.Serialize(JNn 1, options))

    let externalTagOptions = JsonSerializerOptions()
    externalTagOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.ExternalTag))

    [<Fact>]
    let ``deserialize ExternalTag`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"Ba":[]}""", externalTagOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"Bb":[32]}""", externalTagOptions))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""{"Bc":["test",true]}""", externalTagOptions))

    [<Fact>]
    let ``serialize ExternalTag`` () =
        Assert.Equal("""{"Ba":[]}""", JsonSerializer.Serialize(Ba, externalTagOptions))
        Assert.Equal("""{"Bb":[32]}""", JsonSerializer.Serialize(Bb 32, externalTagOptions))
        Assert.Equal("""{"Bc":["test",true]}""", JsonSerializer.Serialize(Bc("test", true), externalTagOptions))

    let externalTagPolicyOptions = JsonSerializerOptions()

    externalTagPolicyOptions.Converters.Add(
        JsonFSharpConverter(JsonUnionEncoding.ExternalTag, unionTagNamingPolicy = JsonNamingPolicy.CamelCase)
    )

    [<Fact>]
    let ``deserialize ExternalTag with tag policy`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"ba":[]}""", externalTagPolicyOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"bb":[32]}""", externalTagPolicyOptions))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""{"bc":["test",true]}""", externalTagPolicyOptions))

    [<Fact>]
    let ``serialize ExternalTag with tag policy`` () =
        Assert.Equal("""{"ba":[]}""", JsonSerializer.Serialize(Ba, externalTagPolicyOptions))
        Assert.Equal("""{"bb":[32]}""", JsonSerializer.Serialize(Bb 32, externalTagPolicyOptions))
        Assert.Equal("""{"bc":["test",true]}""", JsonSerializer.Serialize(Bc("test", true), externalTagPolicyOptions))

    [<Fact>]
    let ``deserialize ExternalTag with JsonName`` () =
        Assert.Equal(JNs 1, JsonSerializer.Deserialize("""{"jstring":[1]}""", externalTagOptions))
        Assert.Equal(JNi 1, JsonSerializer.Deserialize("""{"42":[1]}""", externalTagOptions))
        Assert.Equal(JNb 1, JsonSerializer.Deserialize("""{"true":[1]}""", externalTagOptions))
        Assert.Equal(JNb 1, JsonSerializer.Deserialize("""{"jbool":[1]}""", externalTagOptions))
        Assert.Equal(JNn 1, JsonSerializer.Deserialize("""{"JNn":[1]}""", externalTagOptions))

    [<Fact>]
    let ``serialize ExternalTag with JsonName`` () =
        Assert.Equal("""{"jstring":[1]}""", JsonSerializer.Serialize(JNs 1, externalTagOptions))
        Assert.Equal("""{"42":[1]}""", JsonSerializer.Serialize(JNi 1, externalTagOptions))
        Assert.Equal("""{"true":[1]}""", JsonSerializer.Serialize(JNb 1, externalTagOptions))
        Assert.Equal("""{"JNn":[1]}""", JsonSerializer.Serialize(JNn 1, externalTagOptions))

    let internalTagOptions = JsonSerializerOptions()
    internalTagOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.InternalTag))

    [<Fact>]
    let ``deserialize InternalTag`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""["Ba"]""", internalTagOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""["Bb",32]""", internalTagOptions))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""["Bc","test",true]""", internalTagOptions))

    [<Fact>]
    let ``serialize InternalTag`` () =
        Assert.Equal("""["Ba"]""", JsonSerializer.Serialize(Ba, internalTagOptions))
        Assert.Equal("""["Bb",32]""", JsonSerializer.Serialize(Bb 32, internalTagOptions))
        Assert.Equal("""["Bc","test",true]""", JsonSerializer.Serialize(Bc("test", true), internalTagOptions))

    let internalTagPolicyOptions = JsonSerializerOptions()

    internalTagPolicyOptions.Converters.Add(
        JsonFSharpConverter(JsonUnionEncoding.InternalTag, unionTagNamingPolicy = JsonNamingPolicy.CamelCase)
    )

    [<Fact>]
    let ``deserialize InternalTag with tag policy`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""["ba"]""", internalTagPolicyOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""["bb",32]""", internalTagPolicyOptions))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""["bc","test",true]""", internalTagPolicyOptions))

    [<Fact>]
    let ``serialize InternalTag with tag policy`` () =
        Assert.Equal("""["ba"]""", JsonSerializer.Serialize(Ba, internalTagPolicyOptions))
        Assert.Equal("""["bb",32]""", JsonSerializer.Serialize(Bb 32, internalTagPolicyOptions))
        Assert.Equal("""["bc","test",true]""", JsonSerializer.Serialize(Bc("test", true), internalTagPolicyOptions))

    [<Fact>]
    let ``deserialize InternalTag with JsonName`` () =
        Assert.Equal(JNs 1, JsonSerializer.Deserialize("""["jstring",1]""", internalTagOptions))
        Assert.Equal(JNi 1, JsonSerializer.Deserialize("""[42,1]""", internalTagOptions))
        Assert.Equal(JNb 1, JsonSerializer.Deserialize("""[true,1]""", internalTagOptions))
        Assert.Equal(JNb 1, JsonSerializer.Deserialize("""["jbool",1]""", internalTagOptions))
        Assert.Equal(JNn 1, JsonSerializer.Deserialize("""["JNn",1]""", internalTagOptions))

    [<Fact>]
    let ``serialize InternalTag with JsonName`` () =
        Assert.Equal("""["jstring",1]""", JsonSerializer.Serialize(JNs 1, internalTagOptions))
        Assert.Equal("""[42,1]""", JsonSerializer.Serialize(JNi 1, internalTagOptions))
        Assert.Equal("""[true,1]""", JsonSerializer.Serialize(JNb 1, internalTagOptions))
        Assert.Equal("""["JNn",1]""", JsonSerializer.Serialize(JNn 1, internalTagOptions))

    let untaggedOptions = JsonSerializerOptions()
    untaggedOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.Untagged))

    [<Fact>]
    let ``deserialize Untagged`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{}""", untaggedOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"Item":32}""", untaggedOptions))
        Assert.Equal(Bc("test", true), JsonSerializer.Deserialize("""{"x":"test","Item2":true}""", untaggedOptions))

    [<Fact>]
    let ``serialize Untagged`` () =
        Assert.Equal("""{}""", JsonSerializer.Serialize(Ba, untaggedOptions))
        Assert.Equal("""{"Item":32}""", JsonSerializer.Serialize(Bb 32, untaggedOptions))
        Assert.Equal("""{"x":"test","Item2":true}""", JsonSerializer.Serialize(Bc("test", true), untaggedOptions))

    [<Fact>]
    let ``deserialize Untagged with JsonName`` () =
        Assert.Equal(JNs 1, JsonSerializer.Deserialize("""{"jnsField":1}""", untaggedOptions))
        Assert.Equal(JNi 1, JsonSerializer.Deserialize("""{"jniField":1}""", untaggedOptions))
        Assert.Equal(JNb 1, JsonSerializer.Deserialize("""{"jnbField":1}""", untaggedOptions))
        Assert.Equal(JNn 1, JsonSerializer.Deserialize("""{"jnnField":1}""", untaggedOptions))

    [<Fact>]
    let ``serialize Untagged with JsonName`` () =
        Assert.Equal("""{"jnsField":1}""", JsonSerializer.Serialize(JNs 1, untaggedOptions))
        Assert.Equal("""{"jniField":1}""", JsonSerializer.Serialize(JNi 1, untaggedOptions))
        Assert.Equal("""{"jnbField":1}""", JsonSerializer.Serialize(JNb 1, untaggedOptions))
        Assert.Equal("""{"jnnField":1}""", JsonSerializer.Serialize(JNn 1, untaggedOptions))

    let adjacentTagNamedFieldsOptions = JsonSerializerOptions()

    adjacentTagNamedFieldsOptions.Converters.Add(
        JsonFSharpConverter(
            JsonUnionEncoding.AdjacentTag
            ||| JsonUnionEncoding.NamedFields
            ||| JsonUnionEncoding.AllowUnorderedTag
        )
    )

    [<Fact>]
    let ``deserialize AdjacentTag NamedFields`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"Case":"Ba"}""", adjacentTagNamedFieldsOptions))
        Assert.Equal(
            Bb 32,
            JsonSerializer.Deserialize("""{"Case":"Bb","Fields":{"Item":32}}""", adjacentTagNamedFieldsOptions)
        )
        Assert.Equal(
            Bc("test", true),
            JsonSerializer.Deserialize(
                """{"Case":"Bc","Fields":{"x":"test","Item2":true}}""",
                adjacentTagNamedFieldsOptions
            )
        )
        Assert.Equal(
            Bc("test", true),
            JsonSerializer.Deserialize(
                """{"Fields":{"x":"test","Item2":true},"Case":"Bc"}""",
                adjacentTagNamedFieldsOptions
            )
        )

    [<Fact>]
    let ``serialize AdjacentTag NamedFields`` () =
        Assert.Equal("""{"Case":"Ba"}""", JsonSerializer.Serialize(Ba, adjacentTagNamedFieldsOptions))
        Assert.Equal(
            """{"Case":"Bb","Fields":{"Item":32}}""",
            JsonSerializer.Serialize(Bb 32, adjacentTagNamedFieldsOptions)
        )
        Assert.Equal(
            """{"Case":"Bc","Fields":{"x":"test","Item2":true}}""",
            JsonSerializer.Serialize(Bc("test", true), adjacentTagNamedFieldsOptions)
        )

    let adjacentTagNamedFieldsTagPolicyOptions = JsonSerializerOptions()

    adjacentTagNamedFieldsTagPolicyOptions.Converters.Add(
        JsonFSharpConverter(
            JsonUnionEncoding.AdjacentTag
            ||| JsonUnionEncoding.NamedFields
            ||| JsonUnionEncoding.AllowUnorderedTag,
            unionTagNamingPolicy = JsonNamingPolicy.CamelCase
        )
    )

    [<Fact>]
    let ``deserialize AdjacentTag NamedFields with tag policy`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"Case":"ba"}""", adjacentTagNamedFieldsTagPolicyOptions))
        Assert.Equal(
            Bb 32,
            JsonSerializer.Deserialize("""{"Case":"bb","Fields":{"Item":32}}""", adjacentTagNamedFieldsTagPolicyOptions)
        )
        Assert.Equal(
            Bc("test", true),
            JsonSerializer.Deserialize(
                """{"Case":"bc","Fields":{"x":"test","Item2":true}}""",
                adjacentTagNamedFieldsTagPolicyOptions
            )
        )
        Assert.Equal(
            Bc("test", true),
            JsonSerializer.Deserialize(
                """{"Fields":{"x":"test","Item2":true},"Case":"bc"}""",
                adjacentTagNamedFieldsTagPolicyOptions
            )
        )

    [<Fact>]
    let ``serialize AdjacentTag NamedFields with tag policy`` () =
        Assert.Equal("""{"Case":"ba"}""", JsonSerializer.Serialize(Ba, adjacentTagNamedFieldsTagPolicyOptions))
        Assert.Equal(
            """{"Case":"bb","Fields":{"Item":32}}""",
            JsonSerializer.Serialize(Bb 32, adjacentTagNamedFieldsTagPolicyOptions)
        )
        Assert.Equal(
            """{"Case":"bc","Fields":{"x":"test","Item2":true}}""",
            JsonSerializer.Serialize(Bc("test", true), adjacentTagNamedFieldsTagPolicyOptions)
        )

    [<Fact>]
    let ``deserialize AdjacentTag NamedFields with JsonName`` () =
        Assert.Equal(
            JNs 1,
            JsonSerializer.Deserialize("""{"Case":"jstring","Fields":{"jnsField":1}}""", adjacentTagNamedFieldsOptions)
        )
        Assert.Equal(
            JNi 1,
            JsonSerializer.Deserialize("""{"Case":42,"Fields":{"jniField":1}}""", adjacentTagNamedFieldsOptions)
        )
        Assert.Equal(
            JNb 1,
            JsonSerializer.Deserialize("""{"Case":true,"Fields":{"jnbField":1}}""", adjacentTagNamedFieldsOptions)
        )
        Assert.Equal(
            JNb 1,
            JsonSerializer.Deserialize("""{"Case":"jbool","Fields":{"jnbField":1}}""", adjacentTagNamedFieldsOptions)
        )
        Assert.Equal(
            JNn 1,
            JsonSerializer.Deserialize("""{"Case":"JNn","Fields":{"jnnField":1}}""", adjacentTagNamedFieldsOptions)
        )

    [<Fact>]
    let ``serialize AdjacentTag NamedFields with JsonName`` () =
        Assert.Equal(
            """{"Case":"jstring","Fields":{"jnsField":1}}""",
            JsonSerializer.Serialize(JNs 1, adjacentTagNamedFieldsOptions)
        )
        Assert.Equal(
            """{"Case":42,"Fields":{"jniField":1}}""",
            JsonSerializer.Serialize(JNi 1, adjacentTagNamedFieldsOptions)
        )
        Assert.Equal(
            """{"Case":true,"Fields":{"jnbField":1}}""",
            JsonSerializer.Serialize(JNb 1, adjacentTagNamedFieldsOptions)
        )
        Assert.Equal(
            """{"Case":"JNn","Fields":{"jnnField":1}}""",
            JsonSerializer.Serialize(JNn 1, adjacentTagNamedFieldsOptions)
        )

    let externalTagNamedFieldsOptions = JsonSerializerOptions()

    externalTagNamedFieldsOptions.Converters.Add(
        JsonFSharpConverter(JsonUnionEncoding.ExternalTag ||| JsonUnionEncoding.NamedFields)
    )

    [<Fact>]
    let ``deserialize ExternalTag NamedFields`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"Ba":{}}""", externalTagNamedFieldsOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"Bb":{"Item":32}}""", externalTagNamedFieldsOptions))
        Assert.Equal(
            Bc("test", true),
            JsonSerializer.Deserialize("""{"Bc":{"x":"test","Item2":true}}""", externalTagNamedFieldsOptions)
        )

    [<Fact>]
    let ``serialize ExternalTag NamedFields`` () =
        Assert.Equal("""{"Ba":{}}""", JsonSerializer.Serialize(Ba, externalTagNamedFieldsOptions))
        Assert.Equal("""{"Bb":{"Item":32}}""", JsonSerializer.Serialize(Bb 32, externalTagNamedFieldsOptions))
        Assert.Equal(
            """{"Bc":{"x":"test","Item2":true}}""",
            JsonSerializer.Serialize(Bc("test", true), externalTagNamedFieldsOptions)
        )

    let externalTagNamedFieldsTagPolicyOptions = JsonSerializerOptions()

    externalTagNamedFieldsTagPolicyOptions.Converters.Add(
        JsonFSharpConverter(
            JsonUnionEncoding.ExternalTag ||| JsonUnionEncoding.NamedFields,
            unionTagNamingPolicy = JsonNamingPolicy.CamelCase
        )
    )

    [<Fact>]
    let ``deserialize ExternalTag NamedFields with tag policy`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"ba":{}}""", externalTagNamedFieldsTagPolicyOptions))
        Assert.Equal(
            Bb 32,
            JsonSerializer.Deserialize("""{"bb":{"Item":32}}""", externalTagNamedFieldsTagPolicyOptions)
        )
        Assert.Equal(
            Bc("test", true),
            JsonSerializer.Deserialize("""{"bc":{"x":"test","Item2":true}}""", externalTagNamedFieldsTagPolicyOptions)
        )

    [<Fact>]
    let ``serialize ExternalTag NamedFields with tag policy`` () =
        Assert.Equal("""{"ba":{}}""", JsonSerializer.Serialize(Ba, externalTagNamedFieldsTagPolicyOptions))
        Assert.Equal("""{"bb":{"Item":32}}""", JsonSerializer.Serialize(Bb 32, externalTagNamedFieldsTagPolicyOptions))
        Assert.Equal(
            """{"bc":{"x":"test","Item2":true}}""",
            JsonSerializer.Serialize(Bc("test", true), externalTagNamedFieldsTagPolicyOptions)
        )

    [<Fact>]
    let ``deserialize ExternalTag NamedFields with JsonName`` () =
        Assert.Equal(JNs 1, JsonSerializer.Deserialize("""{"jstring":{"jnsField":1}}""", externalTagNamedFieldsOptions))
        Assert.Equal(JNi 1, JsonSerializer.Deserialize("""{"42":{"jniField":1}}""", externalTagNamedFieldsOptions))
        Assert.Equal(JNb 1, JsonSerializer.Deserialize("""{"true":{"jnbField":1}}""", externalTagNamedFieldsOptions))
        Assert.Equal(JNb 1, JsonSerializer.Deserialize("""{"jbool":{"jnbField":1}}""", externalTagNamedFieldsOptions))
        Assert.Equal(JNn 1, JsonSerializer.Deserialize("""{"JNn":{"jnnField":1}}""", externalTagNamedFieldsOptions))

    [<Fact>]
    let ``serialize ExternalTag NamedFields with JsonName`` () =
        Assert.Equal("""{"jstring":{"jnsField":1}}""", JsonSerializer.Serialize(JNs 1, externalTagNamedFieldsOptions))
        Assert.Equal("""{"42":{"jniField":1}}""", JsonSerializer.Serialize(JNi 1, externalTagNamedFieldsOptions))
        Assert.Equal("""{"true":{"jnbField":1}}""", JsonSerializer.Serialize(JNb 1, externalTagNamedFieldsOptions))
        Assert.Equal("""{"JNn":{"jnnField":1}}""", JsonSerializer.Serialize(JNn 1, externalTagNamedFieldsOptions))

    let internalTagNamedFieldsOptions = JsonSerializerOptions()

    internalTagNamedFieldsOptions.Converters.Add(
        JsonFSharpConverter(
            JsonUnionEncoding.InternalTag
            ||| JsonUnionEncoding.NamedFields
            ||| JsonUnionEncoding.Default
        )
    )

    [<Fact>]
    let ``deserialize InternalTag NamedFields`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"Case":"Ba"}""", internalTagNamedFieldsOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"Case":"Bb","Item":32}""", internalTagNamedFieldsOptions))
        Assert.Equal(
            Bc("test", true),
            JsonSerializer.Deserialize("""{"Case":"Bc","x":"test","Item2":true}""", internalTagNamedFieldsOptions)
        )
        Assert.Equal(
            Bc("test", true),
            JsonSerializer.Deserialize("""{"x":"test","Case":"Bc","Item2":true}""", internalTagNamedFieldsOptions)
        )

    [<Fact>]
    let ``serialize InternalTag NamedFields`` () =
        Assert.Equal("""{"Case":"Ba"}""", JsonSerializer.Serialize(Ba, internalTagNamedFieldsOptions))
        Assert.Equal("""{"Case":"Bb","Item":32}""", JsonSerializer.Serialize(Bb 32, internalTagNamedFieldsOptions))
        Assert.Equal(
            """{"Case":"Bc","x":"test","Item2":true}""",
            JsonSerializer.Serialize(Bc("test", true), internalTagNamedFieldsOptions)
        )

    [<Struct>]
    type Sk = S of a: int * b: Skippable<int> * c: Skippable<int option> * d: Skippable<int voption>

    [<Fact>]
    let ``deserialize InternalTag NamedFields with Skippable fields`` () =
        Assert.Equal(
            S(1, Skip, Skip, Skip),
            JsonSerializer.Deserialize("""{"Case":"S","a":1}""", internalTagNamedFieldsOptions)
        )
        Assert.Equal(
            S(1, Include 2, Include None, Include ValueNone),
            JsonSerializer.Deserialize("""{"Case":"S","a":1,"b":2,"c":null,"d":null}""", internalTagNamedFieldsOptions)
        )
        Assert.Equal(
            S(1, Include 2, Include(Some 3), Include(ValueSome 4)),
            JsonSerializer.Deserialize("""{"Case":"S","a":1,"b":2,"c":3,"d":4}""", internalTagNamedFieldsOptions)
        )
        Assert.Equal(
            S(1, Include 2, Include(Some 3), Include(ValueSome 4)),
            JsonSerializer.Deserialize("""{"a":1,"b":2,"Case":"S","c":3,"d":4}""", internalTagNamedFieldsOptions)
        )

    [<Fact>]
    let ``serialize InternalTag NamedFields with Skippable fields`` () =
        Assert.Equal(
            """{"Case":"S","a":1}""",
            JsonSerializer.Serialize(S(1, Skip, Skip, Skip), internalTagNamedFieldsOptions)
        )
        Assert.Equal(
            """{"Case":"S","a":1,"b":2,"c":null,"d":null}""",
            JsonSerializer.Serialize(S(1, Include 2, Include None, Include ValueNone), internalTagNamedFieldsOptions)
        )
        Assert.Equal(
            """{"Case":"S","a":1,"b":2,"c":3,"d":4}""",
            JsonSerializer.Serialize(
                S(1, Include 2, Include(Some 3), Include(ValueSome 4)),
                internalTagNamedFieldsOptions
            )
        )

    let internalTagNamedFieldsTagPolicyOptions = JsonSerializerOptions()

    internalTagNamedFieldsTagPolicyOptions.Converters.Add(
        JsonFSharpConverter(
            JsonUnionEncoding.InternalTag
            ||| JsonUnionEncoding.NamedFields
            ||| JsonUnionEncoding.AllowUnorderedTag,
            unionTagNamingPolicy = JsonNamingPolicy.CamelCase
        )
    )

    [<Fact>]
    let ``deserialize InternalTag NamedFields with tag policy`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"Case":"ba"}""", internalTagNamedFieldsTagPolicyOptions))
        Assert.Equal(
            Bb 32,
            JsonSerializer.Deserialize("""{"Case":"bb","Item":32}""", internalTagNamedFieldsTagPolicyOptions)
        )
        Assert.Equal(
            Bc("test", true),
            JsonSerializer.Deserialize(
                """{"Case":"bc","x":"test","Item2":true}""",
                internalTagNamedFieldsTagPolicyOptions
            )
        )
        Assert.Equal(
            Bc("test", true),
            JsonSerializer.Deserialize(
                """{"x":"test","Item2":true,"Case":"bc"}""",
                internalTagNamedFieldsTagPolicyOptions
            )
        )

    [<Fact>]
    let ``serialize InternalTag NamedFields with tag policy`` () =
        Assert.Equal("""{"Case":"ba"}""", JsonSerializer.Serialize(Ba, internalTagNamedFieldsTagPolicyOptions))
        Assert.Equal(
            """{"Case":"bb","Item":32}""",
            JsonSerializer.Serialize(Bb 32, internalTagNamedFieldsTagPolicyOptions)
        )
        Assert.Equal(
            """{"Case":"bc","x":"test","Item2":true}""",
            JsonSerializer.Serialize(Bc("test", true), internalTagNamedFieldsTagPolicyOptions)
        )

    [<Fact>]
    let ``deserialize InternalTag NamedFields with JsonName`` () =
        Assert.Equal(
            JNs 1,
            JsonSerializer.Deserialize("""{"Case":"jstring","jnsField":1}""", internalTagNamedFieldsOptions)
        )
        Assert.Equal(JNi 1, JsonSerializer.Deserialize("""{"Case":42,"jniField":1}""", internalTagNamedFieldsOptions))
        Assert.Equal(JNb 1, JsonSerializer.Deserialize("""{"Case":true,"jnbField":1}""", internalTagNamedFieldsOptions))
        Assert.Equal(
            JNb 1,
            JsonSerializer.Deserialize("""{"Case":"jbool","jnbField":1}""", internalTagNamedFieldsOptions)
        )
        Assert.Equal(
            JNn 1,
            JsonSerializer.Deserialize("""{"Case":"JNn","jnnField":1}""", internalTagNamedFieldsOptions)
        )

    [<Fact>]
    let ``serialize InternalTag NamedFields with JsonName`` () =
        Assert.Equal(
            """{"Case":"jstring","jnsField":1}""",
            JsonSerializer.Serialize(JNs 1, internalTagNamedFieldsOptions)
        )
        Assert.Equal("""{"Case":42,"jniField":1}""", JsonSerializer.Serialize(JNi 1, internalTagNamedFieldsOptions))
        Assert.Equal("""{"Case":true,"jnbField":1}""", JsonSerializer.Serialize(JNb 1, internalTagNamedFieldsOptions))
        Assert.Equal("""{"Case":"JNn","jnnField":1}""", JsonSerializer.Serialize(JNn 1, internalTagNamedFieldsOptions))

    let internalTagNamedFieldsConfiguredTagOptions = JsonSerializerOptions()

    internalTagNamedFieldsConfiguredTagOptions.Converters.Add(
        JsonFSharpConverter(
            JsonUnionEncoding.InternalTag
            ||| JsonUnionEncoding.NamedFields
            ||| JsonUnionEncoding.AllowUnorderedTag,
            "type"
        )
    )

    [<Fact>]
    let ``deserialize InternalTag NamedFields alternative Tag`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"type":"Ba"}""", internalTagNamedFieldsConfiguredTagOptions))
        Assert.Equal(
            Bb 32,
            JsonSerializer.Deserialize("""{"type":"Bb","Item":32}""", internalTagNamedFieldsConfiguredTagOptions)
        )
        Assert.Equal(
            Bc("test", true),
            JsonSerializer.Deserialize(
                """{"type":"Bc","x":"test","Item2":true}""",
                internalTagNamedFieldsConfiguredTagOptions
            )
        )
        Assert.Equal(
            Bc("test", true),
            JsonSerializer.Deserialize(
                """{"x":"test","type":"Bc","Item2":true}""",
                internalTagNamedFieldsConfiguredTagOptions
            )
        )

    [<Fact>]
    let ``serialize InternalTag NamedFields alternative Tag`` () =
        Assert.Equal("""{"type":"Ba"}""", JsonSerializer.Serialize(Ba, internalTagNamedFieldsConfiguredTagOptions))
        Assert.Equal(
            """{"type":"Bb","Item":32}""",
            JsonSerializer.Serialize(Bb 32, internalTagNamedFieldsConfiguredTagOptions)
        )
        Assert.Equal(
            """{"type":"Bc","x":"test","Item2":true}""",
            JsonSerializer.Serialize(Bc("test", true), internalTagNamedFieldsConfiguredTagOptions)
        )

    let adjacentTagConfiguredFieldsOptions = JsonSerializerOptions()

    adjacentTagConfiguredFieldsOptions.Converters.Add(
        JsonFSharpConverter(JsonUnionEncoding.AdjacentTag ||| JsonUnionEncoding.AllowUnorderedTag, "type", "args")
    )

    [<Fact>]
    let ``deserialize AdjacentTag NamedFields alternative Fields`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"type":"Ba"}""", adjacentTagConfiguredFieldsOptions))
        Assert.Equal(
            Bb 32,
            JsonSerializer.Deserialize("""{"type":"Bb","args":[32]}""", adjacentTagConfiguredFieldsOptions)
        )
        Assert.Equal(
            Bc("test", true),
            JsonSerializer.Deserialize("""{"type":"Bc","args":["test",true]}""", adjacentTagConfiguredFieldsOptions)
        )
        Assert.Equal(
            Bc("test", true),
            JsonSerializer.Deserialize("""{"args":["test",true],"type":"Bc"}""", adjacentTagConfiguredFieldsOptions)
        )

    [<Fact>]
    let ``serialize AdjacentTag NamedFields alternative Fields`` () =
        Assert.Equal("""{"type":"Ba"}""", JsonSerializer.Serialize(Ba, adjacentTagConfiguredFieldsOptions))
        Assert.Equal(
            """{"type":"Bb","args":[32]}""",
            JsonSerializer.Serialize(Bb 32, adjacentTagConfiguredFieldsOptions)
        )
        Assert.Equal(
            """{"type":"Bc","args":["test",true]}""",
            JsonSerializer.Serialize(Bc("test", true), adjacentTagConfiguredFieldsOptions)
        )

    let unwrapSingleFieldCasesOptions = JsonSerializerOptions()

    unwrapSingleFieldCasesOptions.Converters.Add(
        JsonFSharpConverter(JsonUnionEncoding.Default ||| JsonUnionEncoding.UnwrapSingleFieldCases)
    )

    [<Fact>]
    let ``deserialize unwrapped single-field cases`` () =
        Assert.Equal(Ba, JsonSerializer.Deserialize("""{"Case":"Ba"}""", unwrapSingleFieldCasesOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"Case":"Bb","Fields":32}""", unwrapSingleFieldCasesOptions))
        Assert.Equal(
            Bc("test", true),
            JsonSerializer.Deserialize("""{"Case":"Bc","Fields":["test",true]}""", unwrapSingleFieldCasesOptions)
        )
        Assert.Equal(
            Bc("test", true),
            JsonSerializer.Deserialize("""{"Fields":["test",true],"Case":"Bc"}""", unwrapSingleFieldCasesOptions)
        )

    [<Fact>]
    let ``serialize unwrapped single-field cases`` () =
        Assert.Equal("""{"Case":"Ba"}""", JsonSerializer.Serialize(Ba, unwrapSingleFieldCasesOptions))
        Assert.Equal("""{"Case":"Bb","Fields":32}""", JsonSerializer.Serialize(Bb 32, unwrapSingleFieldCasesOptions))
        Assert.Equal(
            """{"Case":"Bc","Fields":["test",true]}""",
            JsonSerializer.Serialize(Bc("test", true), unwrapSingleFieldCasesOptions)
        )

    let unwrapFieldlessTagsOptions = JsonSerializerOptions()
    unwrapFieldlessTagsOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.UnwrapFieldlessTags))

    [<Struct>]
    type S = { b: B }

    [<Fact>]
    let ``deserialize UnwrapFieldlessTags`` () =
        Assert.Equal({ b = Ba }, JsonSerializer.Deserialize("""{"b":"Ba"}""", unwrapFieldlessTagsOptions))
        Assert.Equal(Bb 32, JsonSerializer.Deserialize("""{"Case":"Bb","Fields":[32]}""", unwrapFieldlessTagsOptions))
        Assert.Equal(
            Bc("test", true),
            JsonSerializer.Deserialize("""{"Case":"Bc","Fields":["test",true]}""", unwrapFieldlessTagsOptions)
        )

    [<Fact>]
    let ``serialize UnwrapFieldlessTags`` () =
        Assert.Equal("""{"b":"Ba"}""", JsonSerializer.Serialize({ b = Ba }, unwrapFieldlessTagsOptions))
        Assert.Equal("""{"Case":"Bb","Fields":[32]}""", JsonSerializer.Serialize(Bb 32, unwrapFieldlessTagsOptions))
        Assert.Equal(
            """{"Case":"Bc","Fields":["test",true]}""",
            JsonSerializer.Serialize(Bc("test", true), unwrapFieldlessTagsOptions)
        )

    let unwrapFieldlessTagsTagPolicyOptions = JsonSerializerOptions()

    unwrapFieldlessTagsTagPolicyOptions.Converters.Add(
        JsonFSharpConverter(JsonUnionEncoding.UnwrapFieldlessTags, unionTagNamingPolicy = JsonNamingPolicy.CamelCase)
    )

    [<Fact>]
    let ``deserialize UnwrapFieldlessTags with tag policy`` () =
        Assert.Equal({ b = Ba }, JsonSerializer.Deserialize("""{"b":"ba"}""", unwrapFieldlessTagsTagPolicyOptions))
        Assert.Equal(
            Bb 32,
            JsonSerializer.Deserialize("""{"Case":"bb","Fields":[32]}""", unwrapFieldlessTagsTagPolicyOptions)
        )
        Assert.Equal(
            Bc("test", true),
            JsonSerializer.Deserialize("""{"Case":"bc","Fields":["test",true]}""", unwrapFieldlessTagsTagPolicyOptions)
        )

    [<Fact>]
    let ``serialize UnwrapFieldlessTags with tag policy`` () =
        Assert.Equal("""{"b":"ba"}""", JsonSerializer.Serialize({ b = Ba }, unwrapFieldlessTagsTagPolicyOptions))
        Assert.Equal(
            """{"Case":"bb","Fields":[32]}""",
            JsonSerializer.Serialize(Bb 32, unwrapFieldlessTagsTagPolicyOptions)
        )
        Assert.Equal(
            """{"Case":"bc","Fields":["test",true]}""",
            JsonSerializer.Serialize(Bc("test", true), unwrapFieldlessTagsTagPolicyOptions)
        )

    [<Struct>]
    type UnionWithPropertyNames =
        | [<JsonPropertyName "nullary">] NamedNullary
        | [<JsonPropertyName "withArgs">] NamedWithArgs of int

    [<Fact>]
    let ``deserialize with JsonPropertyName on case`` () =
        Assert.Equal(NamedNullary, JsonSerializer.Deserialize("\"nullary\"", unwrapFieldlessTagsOptions))
        Assert.Equal(
            NamedWithArgs 42,
            JsonSerializer.Deserialize("""{"Case":"withArgs","Fields":[42]}""", unwrapFieldlessTagsOptions)
        )

    [<Fact>]
    let ``serialize with JsonPropertyName on case`` () =
        Assert.Equal("\"nullary\"", JsonSerializer.Serialize(NamedNullary, unwrapFieldlessTagsOptions))
        Assert.Equal(
            """{"Case":"withArgs","Fields":[42]}""",
            JsonSerializer.Serialize(NamedWithArgs 42, unwrapFieldlessTagsOptions)
        )

    let nonUnwrapOptionOptions = JsonSerializerOptions()
    nonUnwrapOptionOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.AdjacentTag))

    [<Fact>]
    let ``serialize non-UnwrapOption`` () =
        Assert.Equal(
            """{"vo":{"Case":"ValueSome","Fields":[123]}}""",
            JsonSerializer.Serialize({ vo = ValueSome 123 }, nonUnwrapOptionOptions)
        )
        Assert.Equal(
            """{"vo":{"Case":"ValueNone"}}""",
            JsonSerializer.Serialize({ vo = ValueNone }, nonUnwrapOptionOptions)
        )

    [<Fact>]
    let ``deserialize non-UnwrapOption`` () =
        Assert.Equal(
            { vo = ValueSome 123 },
            JsonSerializer.Deserialize("""{"vo":{"Case":"ValueSome","Fields":[123]}}""", nonUnwrapOptionOptions)
        )
        Assert.Equal(
            { vo = ValueNone },
            JsonSerializer.Deserialize("""{"vo":{"Case":"ValueNone"}}""", nonUnwrapOptionOptions)
        )

    let ignoreNullOptions = JsonSerializerOptions(IgnoreNullValues = true)

    ignoreNullOptions.Converters.Add(
        JsonFSharpConverter(JsonUnionEncoding.InternalTag ||| JsonUnionEncoding.NamedFields)
    )

    let newIgnoreNullOptions =
        JsonSerializerOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)

    newIgnoreNullOptions.Converters.Add(
        JsonFSharpConverter(JsonUnionEncoding.InternalTag ||| JsonUnionEncoding.NamedFields)
    )

    [<AllowNullLiteral>]
    type Cls() =
        class
        end

    [<Struct>]
    type UnionWithNullableArgument = Foo of x: int * y: Cls

    [<Fact>]
    let ``deserialize with IgnoreNullValues`` () =
        let actual =
            JsonSerializer.Deserialize("""{"Case":"Foo","x":1}""", ignoreNullOptions)
        Assert.Equal(Foo(1, null), actual)

    [<Fact>]
    let ``serialize with IgnoreNullValues`` () =
        let actual = JsonSerializer.Serialize(Foo(1, null), ignoreNullOptions)
        Assert.Equal("""{"Case":"Foo","x":1}""", actual)

    [<Fact>]
    let ``deserialize with JsonIgnoreCondition.WhenWritingNull`` () =
        let actual =
            JsonSerializer.Deserialize("""{"Case":"Foo","x":1}""", newIgnoreNullOptions)
        Assert.Equal(Foo(1, null), actual)

    [<Fact>]
    let ``serialize with JsonIgnoreCondition.WhenWritingNull`` () =
        let actual = JsonSerializer.Serialize(Foo(1, null), newIgnoreNullOptions)
        Assert.Equal("""{"Case":"Foo","x":1}""", actual)

    let propertyNamingPolicyOptions =
        JsonSerializerOptions(PropertyNamingPolicy = JsonNamingPolicy.CamelCase)

    propertyNamingPolicyOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.Untagged))

    [<Struct>]
    type CamelCase = CCA of CcFirst: int * CcSecond: string

    [<Fact>]
    let ``deserialize with property naming policy`` () =
        let actual =
            JsonSerializer.Deserialize("""{"ccFirst":1,"ccSecond":"a"}""", propertyNamingPolicyOptions)
        Assert.Equal(CCA(1, "a"), actual)

    [<Fact>]
    let ``serialize with property naming policy`` () =
        let actual = JsonSerializer.Serialize(CCA(1, "a"), propertyNamingPolicyOptions)
        Assert.Equal("""{"ccFirst":1,"ccSecond":"a"}""", actual)

    let propertyNameCaseInsensitiveOptions =
        JsonSerializerOptions(PropertyNameCaseInsensitive = true)

    propertyNameCaseInsensitiveOptions.Converters.Add(
        JsonFSharpConverter(JsonUnionEncoding.InternalTag ||| JsonUnionEncoding.NamedFields)
    )

    [<Fact>]
    let ``deserialize with property case insensitive`` () =
        let actual =
            JsonSerializer.Deserialize(
                """{"Case":"CCA","cCfIrSt":1,"cCsEcOnD":"a"}""",
                propertyNameCaseInsensitiveOptions
            )
        Assert.Equal(CCA(1, "a"), actual)

    [<Fact>]
    let ``serialize with property case insensitive`` () =
        let actual =
            JsonSerializer.Serialize(CCA(1, "a"), propertyNameCaseInsensitiveOptions)
        Assert.Equal("""{"Case":"CCA","CcFirst":1,"CcSecond":"a"}""", actual)

    let propertyNameCaseInsensitiveUntaggedOptions =
        JsonSerializerOptions(PropertyNameCaseInsensitive = true)

    propertyNameCaseInsensitiveUntaggedOptions.Converters.Add(JsonFSharpConverter(JsonUnionEncoding.Untagged))

    [<Fact>]
    let ``deserialize untagged with property case insensitive`` () =
        let actual =
            JsonSerializer.Deserialize("""{"cCfIrSt":1,"cCsEcOnD":"a"}""", propertyNameCaseInsensitiveUntaggedOptions)
        Assert.Equal(CCA(1, "a"), actual)

    [<Fact>]
    let ``serialize untagged with property case insensitive`` () =
        let actual =
            JsonSerializer.Serialize(CCA(1, "a"), propertyNameCaseInsensitiveUntaggedOptions)
        Assert.Equal("""{"CcFirst":1,"CcSecond":"a"}""", actual)

    [<Struct>]
    type Unwrapped = Unwrapped of string

    [<Fact>]
    let ``deserialize unwrapped single-case`` () =
        Assert.Equal(Unwrapped "foo", JsonSerializer.Deserialize("\"foo\"", options))

    [<Fact>]
    let ``serialize unwrapped single-case`` () =
        Assert.Equal("\"foo\"", JsonSerializer.Serialize(Unwrapped "foo", options))

    let noNewtypeOptions = JsonSerializerOptions()

    noNewtypeOptions.Converters.Add(
        JsonFSharpConverter(JsonUnionEncoding.Default &&& ~~~JsonUnionEncoding.UnwrapSingleCaseUnions)
    )

    [<Fact>]
    let ``deserialize non-unwrapped single-case`` () =
        Assert.Equal(
            Unwrapped "foo",
            JsonSerializer.Deserialize("""{"Case":"Unwrapped","Fields":["foo"]}""", noNewtypeOptions)
        )

    [<Fact>]
    let ``serialize non-unwrapped single-case`` () =
        Assert.Equal(
            """{"Case":"Unwrapped","Fields":["foo"]}""",
            JsonSerializer.Serialize(Unwrapped "foo", noNewtypeOptions)
        )

    module UnwrapRecord =

        [<Struct>]
        type U =
            | U of u: R
            | V of v: int

        and [<Struct>] R = { x: int; y: float }

        let adjacentTagOptions = JsonSerializerOptions()

        adjacentTagOptions.Converters.Add(
            JsonFSharpConverter(JsonUnionEncoding.AdjacentTag ||| JsonUnionEncoding.UnwrapRecordCases)
        )

        [<Fact>]
        let ``serialize with AdjacentTag`` () =
            let actual = JsonSerializer.Serialize(U { x = 1; y = 2. }, adjacentTagOptions)
            Assert.Equal("""{"Case":"U","Fields":{"x":1,"y":2}}""", actual)
            let actual = JsonSerializer.Serialize(V 42, adjacentTagOptions)
            Assert.Equal("""{"Case":"V","Fields":{"v":42}}""", actual)

        [<Fact>]
        let ``deserialize with AdjacentTag`` () =
            let actual =
                JsonSerializer.Deserialize("""{"Case":"U","Fields":{"x":1,"y":2}}""", adjacentTagOptions)
            Assert.Equal(U { x = 1; y = 2. }, actual)
            let actual =
                JsonSerializer.Deserialize("""{"Case":"V","Fields":{"v":42}}""", adjacentTagOptions)
            Assert.Equal(V 42, actual)

        let externalTagOptions = JsonSerializerOptions()

        externalTagOptions.Converters.Add(
            JsonFSharpConverter(JsonUnionEncoding.ExternalTag ||| JsonUnionEncoding.UnwrapRecordCases)
        )

        [<Fact>]
        let ``serialize with externalTag`` () =
            let actual = JsonSerializer.Serialize(U { x = 1; y = 2. }, externalTagOptions)
            Assert.Equal("""{"U":{"x":1,"y":2}}""", actual)
            let actual = JsonSerializer.Serialize(V 42, externalTagOptions)
            Assert.Equal("""{"V":{"v":42}}""", actual)

        [<Fact>]
        let ``deserialize with externalTag`` () =
            let actual =
                JsonSerializer.Deserialize("""{"U":{"x":1,"y":2}}""", externalTagOptions)
            Assert.Equal(U { x = 1; y = 2. }, actual)
            let actual = JsonSerializer.Deserialize("""{"V":{"v":42}}""", externalTagOptions)
            Assert.Equal(V 42, actual)

        let internalTagOptions = JsonSerializerOptions()

        internalTagOptions.Converters.Add(
            JsonFSharpConverter(JsonUnionEncoding.InternalTag ||| JsonUnionEncoding.UnwrapRecordCases)
        )

        [<Fact>]
        let ``serialize with internalTag`` () =
            let actual = JsonSerializer.Serialize(U { x = 1; y = 2. }, internalTagOptions)
            Assert.Equal("""{"Case":"U","x":1,"y":2}""", actual)
            let actual = JsonSerializer.Serialize(V 42, internalTagOptions)
            Assert.Equal("""{"Case":"V","v":42}""", actual)

        [<Fact>]
        let ``deserialize with internalTag`` () =
            let actual =
                JsonSerializer.Deserialize("""{"Case":"U","x":1,"y":2}""", internalTagOptions)
            Assert.Equal(U { x = 1; y = 2. }, actual)
            let actual =
                JsonSerializer.Deserialize("""{"Case":"V","v":42}""", internalTagOptions)
            Assert.Equal(V 42, actual)

        let untaggedOptions = JsonSerializerOptions()

        untaggedOptions.Converters.Add(
            JsonFSharpConverter(JsonUnionEncoding.Untagged ||| JsonUnionEncoding.UnwrapRecordCases)
        )

        [<Fact>]
        let ``serialize with untagged`` () =
            let actual = JsonSerializer.Serialize(U { x = 1; y = 2. }, untaggedOptions)
            Assert.Equal("""{"x":1,"y":2}""", actual)
            let actual = JsonSerializer.Serialize(V 42, untaggedOptions)
            Assert.Equal("""{"v":42}""", actual)

        [<Fact>]
        let ``deserialize with untagged`` () =
            let actual = JsonSerializer.Deserialize("""{"x":1,"y":2}""", untaggedOptions)
            Assert.Equal(U { x = 1; y = 2. }, actual)
            let actual = JsonSerializer.Deserialize("""{"v":42}""", untaggedOptions)
            Assert.Equal(V 42, actual)

    [<JsonFSharpConverter(unionTagName = "tag", unionFieldsName = "val")>]
    [<Struct>]
    type Override = A of x: int * y: string

    [<Fact>]
    let ``should not override tag name in attribute if AllowOverride = false`` () =
        let o = JsonSerializerOptions()
        o.Converters.Add(JsonFSharpConverter())
        Assert.Equal("""{"Case":"A","Fields":[123,"abc"]}""", JsonSerializer.Serialize(Override.A(123, "abc"), o))

    [<Fact>]
    let ``should override tag name in attribute if AllowOverride = true`` () =
        let o = JsonSerializerOptions()
        o.Converters.Add(JsonFSharpConverter(allowOverride = true))
        Assert.Equal("""{"tag":"A","val":[123,"abc"]}""", JsonSerializer.Serialize(Override.A(123, "abc"), o))

    [<Fact>]
    let ``should not override JsonEncoding if not specified`` () =
        let o = JsonSerializerOptions()
        o.Converters.Add(
            JsonFSharpConverter(JsonUnionEncoding.InternalTag ||| JsonUnionEncoding.NamedFields, allowOverride = true)
        )
        Assert.Equal("""{"tag":"A","x":123,"y":"abc"}""", JsonSerializer.Serialize(Override.A(123, "abc"), o))

    [<JsonFSharpConverter(JsonUnionEncoding.InternalTag)>]
    [<Struct>]
    type Override2 = A of int * string

    [<Fact>]
    let ``should override JsonEncoding if specified`` () =
        let o = JsonSerializerOptions()
        o.Converters.Add(JsonFSharpConverter(allowOverride = true))
        Assert.Equal("""["A",123,"abc"]""", JsonSerializer.Serialize(Override2.A(123, "abc"), o))

    [<Fact>]
    let ``should apply explicit overrides if allowOverride = false`` () =
        let o = JsonSerializerOptions()
        o.Converters.Add(
            JsonFSharpConverter(overrides = dict [ typeof<Override>, JsonFSharpOptions(JsonUnionEncoding.InternalTag) ])
        )
        Assert.Equal("""["A",123,"abc"]""", JsonSerializer.Serialize(Override.A(123, "abc"), o))

    [<Fact>]
    let ``should apply explicit overrides if allowOverride = true`` () =
        let o = JsonSerializerOptions()
        o.Converters.Add(
            JsonFSharpConverter(
                allowOverride = true,
                overrides = dict [ typeof<Override>, JsonFSharpOptions(JsonUnionEncoding.InternalTag) ]
            )
        )
        Assert.Equal("""["A",123,"abc"]""", JsonSerializer.Serialize(Override.A(123, "abc"), o))

    [<Fact>]
    let ``should apply explicit overrides inheriting JsonUnionEncoding`` () =
        let o = JsonSerializerOptions()
        o.Converters.Add(
            JsonFSharpConverter(
                JsonUnionEncoding.InternalTag ||| JsonUnionEncoding.NamedFields,
                overrides = dict [ typeof<Override>, JsonFSharpOptions(unionTagName = "tag") ]
            )
        )
        Assert.Equal("""{"tag":"A","x":123,"y":"abc"}""", JsonSerializer.Serialize(Override.A(123, "abc"), o))

    [<Struct>]
    type NamedAfterTypesA = NTA of int

    [<Struct>]
    type NamedAfterTypesB = NTB of int * string

    [<Struct>]
    type NamedAfterTypesC = NTC of X: int

    [<Struct>]
    type NamedAfterTypesD = NTD of X: int * Y: string

    [<Struct>]
    type NamedAfterTypesE = NTE of string * string

    let namedAfterTypesOptions = JsonSerializerOptions()

    namedAfterTypesOptions.Converters.Add(
        JsonFSharpConverter(
            JsonUnionEncoding.InternalTag
            ||| JsonUnionEncoding.NamedFields
            ||| JsonUnionEncoding.UnionFieldNamesFromTypes
        )
    )

    [<Fact>]
    let ``serialize UnionFieldNamesFromTypes`` () =
        Assert.Equal("""{"Case":"NTA","Int32":123}""", JsonSerializer.Serialize(NTA 123, namedAfterTypesOptions))
        Assert.Equal(
            """{"Case":"NTB","Int32":123,"String":"test"}""",
            JsonSerializer.Serialize(NTB(123, "test"), namedAfterTypesOptions)
        )
        Assert.Equal("""{"Case":"NTC","X":123}""", JsonSerializer.Serialize(NTC 123, namedAfterTypesOptions))
        Assert.Equal(
            """{"Case":"NTD","X":123,"Y":"test"}""",
            JsonSerializer.Serialize(NTD(123, "test"), namedAfterTypesOptions)
        )
        Assert.Equal(
            """{"Case":"NTE","String1":"123","String2":"test"}""",
            JsonSerializer.Serialize(NTE("123", "test"), namedAfterTypesOptions)
        )

    [<Fact>]
    let ``deserialize UnionFieldNamesFromTypes`` () =
        Assert.Equal(NTA 123, JsonSerializer.Deserialize("""{"Case":"NTA","Int32":123}""", namedAfterTypesOptions))
        Assert.Equal(
            NTB(123, "test"),
            JsonSerializer.Deserialize("""{"Case":"NTB","Int32":123,"String":"test"}""", namedAfterTypesOptions)
        )
        Assert.Equal(NTC 123, JsonSerializer.Deserialize("""{"Case":"NTC","X":123}""", namedAfterTypesOptions))
        Assert.Equal(
            NTD(123, "test"),
            JsonSerializer.Deserialize("""{"Case":"NTD","X":123,"Y":"test"}""", namedAfterTypesOptions)
        )
        Assert.Equal(
            NTE("123", "test"),
            JsonSerializer.Deserialize("""{"Case":"NTE","String1":"123","String2":"test"}""", namedAfterTypesOptions)
        )

    let namedAfterTypesOptionsWithNamingPolicy = JsonSerializerOptions()

    namedAfterTypesOptionsWithNamingPolicy.Converters.Add(
        JsonFSharpConverter(
            JsonUnionEncoding.InternalTag
            ||| JsonUnionEncoding.NamedFields
            ||| JsonUnionEncoding.UnionFieldNamesFromTypes,
            unionFieldNamingPolicy = JsonNamingPolicy.CamelCase
        )
    )

    [<Fact>]
    let ``serialize UnionFieldNamesFromTypes with unionFieldNamingPolicy`` () =
        Assert.Equal(
            """{"Case":"NTA","int32":123}""",
            JsonSerializer.Serialize(NTA 123, namedAfterTypesOptionsWithNamingPolicy)
        )
        Assert.Equal(
            """{"Case":"NTB","int32":123,"string":"test"}""",
            JsonSerializer.Serialize(NTB(123, "test"), namedAfterTypesOptionsWithNamingPolicy)
        )
        Assert.Equal(
            """{"Case":"NTC","x":123}""",
            JsonSerializer.Serialize(NTC 123, namedAfterTypesOptionsWithNamingPolicy)
        )
        Assert.Equal(
            """{"Case":"NTD","x":123,"y":"test"}""",
            JsonSerializer.Serialize(NTD(123, "test"), namedAfterTypesOptionsWithNamingPolicy)
        )
        Assert.Equal(
            """{"Case":"NTE","string1":"123","string2":"test"}""",
            JsonSerializer.Serialize(NTE("123", "test"), namedAfterTypesOptionsWithNamingPolicy)
        )

    [<Fact>]
    let ``deserialize UnionFieldNamesFromTypes with unionFieldNamingPolicy`` () =
        Assert.Equal(
            NTA 123,
            JsonSerializer.Deserialize("""{"Case":"NTA","int32":123}""", namedAfterTypesOptionsWithNamingPolicy)
        )
        Assert.Equal(
            NTB(123, "test"),
            JsonSerializer.Deserialize(
                """{"Case":"NTB","int32":123,"string":"test"}""",
                namedAfterTypesOptionsWithNamingPolicy
            )
        )
        Assert.Equal(
            NTC 123,
            JsonSerializer.Deserialize("""{"Case":"NTC","x":123}""", namedAfterTypesOptionsWithNamingPolicy)
        )
        Assert.Equal(
            NTD(123, "test"),
            JsonSerializer.Deserialize("""{"Case":"NTD","x":123,"y":"test"}""", namedAfterTypesOptionsWithNamingPolicy)
        )
        Assert.Equal(
            NTE("123", "test"),
            JsonSerializer.Deserialize(
                """{"Case":"NTE","string1":"123","string2":"test"}""",
                namedAfterTypesOptionsWithNamingPolicy
            )
        )
