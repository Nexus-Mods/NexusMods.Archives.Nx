# Traits

In this project 'Traits' are defined as interfaces where we use them in the following manner:

```csharp
public byte[] Method<T>(T item) where T : ITrait
{
    return default;
}
```

The idea is to use the `where` constraint to eliminate virtual call overhead; and be able to inline
elements directly. That's all.