# Serialization format

<!-- START doctoc generated TOC please keep comment here to allow auto update -->
<!-- DON'T EDIT THIS SECTION, INSTEAD RE-RUN doctoc TO UPDATE -->


- [Lists](#lists)
- [Sets](#sets)
- [Maps](#maps)
- [Tuples and struct tuples](#tuples-and-struct-tuples)
- [Records, struct records and anonymous records](#records-struct-records-and-anonymous-records)
- [Unions and struct unions](#unions-and-struct-unions)
- [Options](#options)

<!-- END doctoc generated TOC please keep comment here to allow auto update -->

## Lists

F# lists are serialized as JSON arrays.

```fsharp
JsonSerializer.Serialize([1; 2; 3], options)
// --> [1,2,3]
```

## Sets

F# sets are serialized as JSON arrays.

```fsharp
JsonSerializer.Serialize(Set [1; 2; 3], options)
// --> [1,2,3]
```

## Maps

F# string-keyed maps are serialized as JSON objects.

```fsharp
JsonSerializer.Serialize(Map [("a", 1); ("b", 2); ("c", 3)], options)
// --> {"a":1,"b":2,"c":3}
```

Maps with other types as keys are serialized as JSON arrays, where each item is a `[key,value]` array.

```fsharp
JsonSerializer.Serialize(Map [(1, "a"); (2, "b"); (3, "c")], options)
// --> [[1,"a"],[2,"b"],[3,"c"]]
```

## Tuples and struct tuples

Tuples and struct tuples are serialized as JSON arrays.

```fsharp
JsonSerializer.Serialize((1, "abc"), options)
// --> [1,"abc"]

JsonSerializer.Serialize(struct (1, "abc"), options)
// --> [1,"abc"]
```

## Records, struct records and anonymous records

Records and anonymous records are serialized as JSON objects.

```fsharp
type Example = { x: string; y: string }

JsonSerializer.Serialize({ x = "Hello"; y = "world!" }, options)
// --> {"x":"Hello","y":"world!"}

JsonSerializer.Serialize({| x = "Hello"; y = "world!" |}, options)
// --> {"x":"Hello","y":"world!"}
```

Named record fields are serialized in the order in which they were declared in the type declaration.

Anonymous record fields are serialized in alphabetical order.

## Unions and struct unions

Unions can be serialized in a number of formats.
See [Customizing](Customizing.md) for a more complete overview.

By default, unions are serialized similarly to the Json.NET library.
A union value is serialized into a JSON object with the following fields:

* A field `"Case"` whose value is a string representing the name of the union case;
* If the case has fields, a field `"Fields"` whose value is an array.

For example:

```fsharp
type Example =
    | WithArgs of anInt: int * aString: string |
    | NoArgs

JsonSerializer.Serialize(NoArgs, options)
// --> {"Case":"NoArgs"}

JsonSerializer.Serialize(WithArgs (123, "Hello, world!"), options)
// --> {"Case":"WithArgs","Fields":[123,"Hello, world!"]}
```

## Options

By default, the types `'T option` and `'T voption` (aka `ValueOption`) are treated specially.

* The value `None` or `ValueNone` is represented as `null`.

* The value `Some x` or `ValueSome x` is represented the same as `x`, without wrapping it in the union representation for `Some`.

This behavior can be [customized](Customizing.md).

<!-- TODO Skippable -->