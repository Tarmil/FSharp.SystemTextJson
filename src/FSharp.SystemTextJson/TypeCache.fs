namespace System.Text.Json.Serialization

module TypeCache = 
    open FSharp.Reflection
    
    // Have to use concurrentdictionary here because dictionaries thrown on non-locked access:
    (* Error Message:
        System.InvalidOperationException : Operations that change non-concurrent collections must have exclusive access. A concurrent update was performed on this collection and corrupted its state. The collection's state is no longer correct.
        Stack Trace:
            at System.Collections.Generic.Dictionary`2.TryInsert(TKey key, TValue value, InsertionBehavior behavior) *)
    type Dict<'a, 'b> = System.Collections.Concurrent.ConcurrentDictionary<'a, 'b>

    type TypeKind =
        | Record = 0
        | Union = 1
        | List = 2
        | Other = 100

    let getKind =
        let cache = Dict<System.Type, TypeKind>()
        let listTy = typedefof<_ list>

        fun (ty: System.Type) ->
            cache.GetOrAdd(ty, fun ty -> 
                if ty.IsGenericType && ty.GetGenericTypeDefinition() = listTy then TypeKind.List
                elif FSharpType.IsUnion(ty, true) then TypeKind.Union
                elif FSharpType.IsRecord(ty, true) then TypeKind.Record
                else TypeKind.Other)

    /// cached access to FSharpType.IsUnion to prevent repeated access to reflection members
    let isUnion ty =
        getKind ty = TypeKind.Union
            
    /// cached access to FSharpType.IsRecord to prevent repeated access to reflection members
    let isRecord ty =
        getKind ty = TypeKind.Record

    let isList ty =
        getKind ty = TypeKind.List
