# Traits

!!! info "In this project `Traits` are defined as interfaces where we use them in the following manner"

```csharp
public byte[] Method<T>(T item) where T : ITrait
{
    return default;
}
```

The idea is to use the `where` constraint for generic parameters. This will cause the method to be JIT'ted with the concrete
type, and therefore eliminate virtual call overhead.