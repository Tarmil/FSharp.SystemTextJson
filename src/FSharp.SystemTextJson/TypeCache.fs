namespace System.Text.Json.Serialization

module TypeCache =
    open FSharp.Reflection

    // Have to use concurrentdictionary here because dictionaries thrown on non-locked access:
    (* Error Message:
        System.InvalidOperationException : Operations that change non-concurrent collections must have exclusive access. A concurrent update was performed on this collection and corrupted its state. The collection's state is no longer correct.
        Stack Trace:
            at System.Collections.Generic.Dictionary`2.TryInsert(TKey key, TValue value, InsertionBehavior behavior) *)
    type Dict<'a, 'b> = System.Collections.Concurrent.ConcurrentDictionary<'a, 'b>

    /// cached access to FSharpType.IsUnion to prevent repeated access to reflection members
    let isUnion =
        let cache = Dict<System.Type, bool>()

        fun (ty: System.Type) ->
            cache.GetOrAdd(ty, (fun ty -> FSharpType.IsUnion(ty, true)))

    /// cached access to FSharpType.IsRecord to prevent repeated access to reflection members
    let isRecord =
        let cache = Dict<System.Type, bool>()

        fun (ty: System.Type) ->
            cache.GetOrAdd(ty, (fun ty -> FSharpType.IsRecord(ty, true)))
