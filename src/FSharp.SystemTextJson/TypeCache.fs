namespace System.Text.Json.Serialization

module private TypeCache = 
    open FSharp.Reflection
    type Dict<'a, 'b> = System.Collections.Generic.Dictionary<'a, 'b>

    /// cached access to FSharpType.IsUnion to prevent repeated access to reflection members
    let isUnion =
        let cache = Dict<string, bool>()

        fun (ty: System.Type) -> 
            match cache.TryGetValue ty.FullName with
            | true, isUnion -> isUnion
            | false, _ -> 
                let isUnion = FSharpType.IsUnion(ty, true)
                cache.[ty.FullName] <- isUnion
                isUnion
            
    /// cached access to FSharpType.IsRecord to prevent repeated access to reflection members
    let isRecord =
        let cache = Dict<string, bool>()

        fun (ty: System.Type) -> 
            match cache.TryGetValue ty.FullName with
            | true, isRecord -> isRecord
            | false, _ -> 
                let isRecord = FSharpType.IsRecord(ty, true)
                cache.[ty.FullName] <- isRecord
                isRecord
            