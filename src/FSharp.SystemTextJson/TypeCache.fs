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
        | Set = 3
        | Map = 4
        | Tuple = 5
        | Other = 100

    /// cached access to FSharpType.* and System.Type to prevent repeated access to reflection members
    let getKind =
        let cache = Dict<System.Type, TypeKind>()
        let listTy = typedefof<_ list>
        let setTy = typedefof<Set<_>>
        let mapTy = typedefof<Map<_,_>>
        let typeKindFactory = System.Func<System.Type, TypeKind>(fun ty ->
            if ty.IsGenericType && ty.GetGenericTypeDefinition() = listTy then TypeKind.List
            elif ty.IsGenericType && ty.GetGenericTypeDefinition() = setTy then TypeKind.Set
            elif ty.IsGenericType && ty.GetGenericTypeDefinition() = mapTy then TypeKind.Map
            elif FSharpType.IsTuple(ty) then TypeKind.Tuple
            elif FSharpType.IsUnion(ty, true) then TypeKind.Union
            else TypeKind.Other)

        fun (ty: System.Type) ->
            cache.GetOrAdd(ty, typeKindFactory)

    let isUnion ty =
        getKind ty = TypeKind.Union

    let isRecord ty =
        getKind ty = TypeKind.Record

    let isList ty =
        getKind ty = TypeKind.List

    let isSet ty =
        getKind ty = TypeKind.Set

    let isMap ty =
        getKind ty = TypeKind.Map

    let isTuple ty =
        getKind ty = TypeKind.Tuple
