# IHasSolidType

!!! info "The `IHasSolidType` trait is used for items that can specify a preference on whether they'd prefer to be SOLIDly packed or not."

## Properties

### SolidType

```csharp
SolidPreference SolidType { get; }
```

This property gets the preference in terms of whether the item should be SOLID (packed in a solid block) or not. The `SolidPreference` enum specifies the available preferences.

The `SolidPreference` enum defines the following values (at time of writing):

- `Default`: Pack into a solid block if possible.  
- `NoSolid`: This file must not be packed in a solid block.  

## Usage

```csharp
public class MyPackedItem : IHasSolidType
{
    public SolidPreference SolidType { get; set; }
}

// Set the preference.
MyPackedItem.SolidType = SolidPreference.Default;
```

In this example, `MyPackedItem` implements the `IHasSolidType` interface.  
`MyPackedItem` can then be used in methods constrained with `where T : IHasSolidType`.