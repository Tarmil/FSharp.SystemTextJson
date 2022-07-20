namespace System.Text.Json.Serialization

/// Represents a value that can be represented as an absent field in JSON.
/// In particular, Skippable<'T option> allows distinguishing between absent field and null field.
[<Struct>]
type Skippable<'T> =
    | Skip
    | Include of 'T

    /// Returns true if the value is Skip.
    member this.isSkip =
        match this with
        | Skip -> true
        | Include _ -> false

    /// Returns true if the value is not Skip.
    member this.isInclude =
        match this with
        | Include _ -> true
        | Skip -> false

module Skippable =

    /// Transforms the input using the specified function.
    let map mapping skippable =
        match skippable with
        | Include x -> Include <| mapping x
        | Skip -> Skip

    /// Transforms the input using the specified function.
    let map2 mapping skippable1 skippable2 =
        match skippable1, skippable2 with
        | Include x, Include y -> Include <| mapping x y
        | _ -> Skip

    /// Transforms the input using the specified function.
    let map3 mapping skippable1 skippable2 skippable3 =
        match skippable1, skippable2, skippable3 with
        | Include x, Include y, Include z -> Include <| mapping x y z
        | _ -> Skip

    /// Applies the Skippable-returning function to the Skippable-wrapped value.
    let bind binder skippable =
        match skippable with
        | Skip -> Skip
        | Include x -> binder x

    /// Applies the Skippable-wrapped function to the Skippable-wrapped value,
    /// returning Skip if either is Skip.
    let apply mapping skippable =
        match mapping, skippable with
        | Include f, Include x -> Include(f x)
        | _ -> Skip

    /// Gets the value of the Skippable if the input is Include, otherwise returns
    /// the specified default value.
    let defaultValue value skippable =
        match skippable with
        | Skip -> value
        | Include x -> x

    /// Gets the value of the Skippable if the input is Include, otherwise evaluates
    /// valueThunk and returns the result.
    let defaultWith valueThunk skippable =
        match skippable with
        | Skip -> valueThunk ()
        | Include x -> x

    /// Returns the Skippable if it is Include, otherwise returns ifSkip.
    let orElse ifSkip skippable =
        match skippable with
        | Skip -> ifSkip
        | Include x -> Include x

    /// Converts the Skippable to an option.
    let toOption skippable =
        match skippable with
        | Skip -> None
        | Include x -> Some x

    /// Converts the option to a Skippable.
    let ofOption option =
        match option with
        | None -> Skip
        | Some x -> Include x

    /// Converts the Skippable to a ValueOption.
    let toValueOption skippable =
        match skippable with
        | Skip -> ValueNone
        | Include x -> ValueSome x

    /// Converts the ValueOption to a Skippable.
    let ofValueOption voption =
        match voption with
        | ValueNone -> Skip
        | ValueSome x -> Include x

    /// Calls the specified function on the Skippable's wrapped value.
    let iter action skippable =
        match skippable with
        | Include x -> action x
        | Skip -> ()

    /// Converts Include to Skip if the wrapped value does not match the predicate.
    let filter predicate skippable =
        match skippable with
        | Include x -> if predicate x then Include x else Skip
        | Skip -> Skip

    /// Returns true if the value is Skip.
    let isSkip (skippable: Skippable<_>) =
        skippable.isSkip

    /// Returns true if the value is not Skip.
    let isInclude (skippable: Skippable<_>) =
        skippable.isInclude


type Skippable<'T> with

    /// Infix operator for Skippable.apply.
    /// Applies the Skippable-wrapped function to the Skippable-wrapped value,
    /// returning Skip if either is Skip.
    static member (<*>)(fSkip, xSkip) =
        Skippable.apply fSkip xSkip

    /// Infix operator for Skippable.map.
    /// Transforms the input using the specified function.
    static member (<!>)(f, skippable) =
        Skippable.map f skippable
