# EnumExt

Source generators for enums: extensions, JSON converters, EF Core value converters, ASP. NET Core model binders, and type wrappers (e.g. for Unity).

## Table of contents

* [Installation](#installation)
* [EnumExtensions](#enumextensions)
* [EnumValuesList](#enumvalueslist)
* [JsonConverter](#jsonconverter)
* [JsonArrayConverter](#jsonarrayconverter)
* [ModelBinder](#modelbinder)
* [ValueConverter](#valueconverter)
* [ValueArrayConverter](#valuearrayconverter)
* [EnumTypeFor](#enumtypefor)

## Installation

1. Import `EnumExt.dll` into your Unity project.
2. Disable all checkboxes in `EnumExt.dll` import settings panel. Press `Apply`.

   <img width="572" height="348" alt="EnumExt_Step_1" src="https://github.com/user-attachments/assets/a1ab1fe6-eaf1-4434-aaeb-650901c38072" />

3. Add `RoslynAnalyzer` tag to `EnumExt.dll`

   <img width="575" height="53" alt="EnumExt_Step_2" src="https://github.com/user-attachments/assets/c0f508cf-2ed9-4858-9f7d-ecb3d32fceb5" />

## EnumExtensions

### Description

Generates a partial static class `{EnumName}Ext` with: `Values` , `StringValues` , extension methods `Value()` , `Name()` , `SnakeCaseName()` , `FromString()` , `FromSnakeCaseString()` , `FromValue()` , `FromStringValue()` . For `[Flags]` enums: `SetFlag` , `GetFlag` , `ClearFlag` , `GetActiveFlags` . For non-flags: `Fold` , `FoldT` , `ValueFold` . Optional LanguageExt support adds `TryFromString` , `TryFromSnakeCaseString` , `TryFromValue` , `TryFromStringValue` .

### How to use

Apply `[EnumExtensions]` to an enum. Use the generated `{EnumName}Ext` class and extension methods.

```csharp
[EnumExtensions]
public enum Status
{
    Pending,
    Active,
    Done,
}

// Usage
var name = Status.Active.Name();           // "Active"
var value = Status.Active.Value();         // 1
var parsed = StatusExt.FromString("Done"); // Status.Done
var all = StatusExt.Values;                // Status[]
```

<details>
<summary>Generated code</summary>

```csharp
public static partial class StatusExt
{
    public const string EnumName = "Status";
    public const string SnakeCaseEnumName = "status";

    public static Status[] Values => new[]
    {
        Status.Pending,
        Status.Active,
        Status.Done,
    };

    public static string[] StringValues => new[]
    {
        "Pending",
        "Active",
        "Done",
    };

    public static int Value(this Status self) => (int) self;

    public static string Name(this Status self)
    {
        switch (self)
        {
            case Status.Pending: return "Pending";
            case Status.Active: return "Active";
            case Status.Done: return "Done";
            default: throw new System.ArgumentOutOfRangeException(nameof(self), self, null);
        }
    }

    public static string SnakeCaseName(this Status self)
    {
        switch (self)
        {
            case Status.Pending: return "pending";
            case Status.Active: return "active";
            case Status.Done: return "done";
            default: throw new System.ArgumentOutOfRangeException(nameof(self), self, null);
        }
    }

    public static Status FromString(string value)
    {
        switch (value)
        {
            case "Pending": return Status.Pending;
            case "Active": return Status.Active;
            case "Done": return Status.Done;
            default: throw new System.ArgumentOutOfRangeException(nameof(value), value, null);
        }
    }

    public static Status FromSnakeCaseString(string value)
    {
        switch (value)
        {
            case "pending": return Status.Pending;
            case "active": return Status.Active;
            case "done": return Status.Done;
            default: throw new System.ArgumentOutOfRangeException(nameof(value), value, null);
        }
    }

    public static Status FromValue(int value)
    {
        switch (value)
        {
            case 0: return Status.Pending;
            case 1: return Status.Active;
            case 2: return Status.Done;
            default: throw new System.ArgumentOutOfRangeException(nameof(value), value, null);
        }
    }

    public static Status FromStringValue(string value)
    {
        switch (value)
        {
            case "0": return Status.Pending;
            case "1": return Status.Active;
            case "2": return Status.Done;
            default: throw new System.ArgumentOutOfRangeException(nameof(value), value, null);
        }
    }

    public static void Fold(this Status self, System.Action onPending = null, System.Action onActive = null, System.Action onDone = null)
    {
        switch (self)
        {
            case Status.Pending: onPending?.Invoke(); return;
            case Status.Active: onActive?.Invoke(); return;
            case Status.Done: onDone?.Invoke(); return;
            default: throw new System.ArgumentOutOfRangeException(nameof(self), self, null);
        }
    }

    public static T Fold<T>(this Status self, System.Func<T> onPending, System.Func<T> onActive, System.Func<T> onDone)
    {
        switch (self)
        {
            case Status.Pending: return onPending.Invoke();
            case Status.Active: return onActive.Invoke();
            case Status.Done: return onDone.Invoke();
            default: throw new System.ArgumentOutOfRangeException(nameof(self), self, null);
        }
    }

    public static T Fold<T>(this Status self, T onPending, T onActive, T onDone)
    {
        switch (self)
        {
            case Status.Pending: return onPending;
            case Status.Active: return onActive;
            case Status.Done: return onDone;
            default: throw new System.ArgumentOutOfRangeException(nameof(self), self, null);
        }
    }
}
```

</details>

## EnumValuesList

### Description

Generates a partial static class with a single static field `Values` — an array of all enum members.

### How to use

Apply `[EnumValuesList]` to an enum. Use the generated `Values` array.

```csharp
[EnumValuesList]
public enum Priority
{
    Low,
    Medium,
    High,
}

// Usage
foreach (var priority in PriorityExt.Values)
{
    Process(priority);
}
```

<details>
<summary>Generated code</summary>

```csharp
public static partial class PriorityExt
{
    public static Priority[] Values =
    {
        Priority.Low,
        Priority.Medium,
        Priority.High,
    };
}
```

</details>

## JsonConverter

### Description

Generates a JSON converter for the enum (System. Text. Json or Newtonsoft. Json). Serialization can use enum name, snake_case name, or numeric value. **Requires `[EnumExtensions]` on the same enum.**

Parameters: `JsonConverterLibrary` — `SystemTextJson` or `NewtonsoftJson` ; `ConversionStrategy` — `Name` , `SnakeCase` , or `Value` .

### How to use

Apply `[JsonConverter(JsonConverterLibrary.SystemTextJson, ConversionStrategy.Name)]` (or `NewtonsoftJson` / `SnakeCase` / `Value` ). Use the generated converter on a property with `[JsonConverter(typeof(StatusJsonConverterName))]` .

```csharp
[EnumExtensions]
[JsonConverter(JsonConverterLibrary.SystemTextJson, ConversionStrategy.Name)]
public enum Status
{
    Pending,
    Active,
    Done,
}

public class MyModel
{
    [JsonPropertyName("status")]
    [JsonConverter(typeof(StatusJsonConverterName))]
    public Status Status { get; set; }
}
```

<details>
<summary>Generated code</summary>

```csharp
public class StatusJsonConverterName : System.Text.Json.JsonConverter<Status>
{
    public override Status Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) { /* ... */ }

    public override void Write(Utf8JsonWriter writer, Status value, JsonSerializerOptions options) { /* ... */ }
}

// Or

public class StatusJsonConverterName : Newtonsoft.Json.JsonConverter
{
    public override bool CanConvert(Type objectType) { /* ... */ }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) { /* ... */ }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) { /* ... */ }
}
```

</details>

## JsonArrayConverter

### Description

Generates a JSON converter for arrays of the enum ( `T[]` ). Same library and conversion strategy options as JsonConverter. **Requires `[EnumExtensions]` on the same enum.**

### How to use

Apply `[JsonArrayConverter(JsonConverterLibrary.SystemTextJson, ConversionStrategy.Name)]` . Use the generated converter on a property with `[JsonConverter(typeof(StatusArrayJsonConverterName))]` .

```csharp
[EnumExtensions]
[JsonArrayConverter(JsonConverterLibrary.SystemTextJson, ConversionStrategy.Name)]
public enum Status
{
    Pending,
    Active,
    Done,
}

public class MyModel
{
    [JsonPropertyName("statuses")]
    [JsonConverter(typeof(StatusArrayJsonConverterName))]
    public Status[] Statuses { get; set; }
}
```

<details>
<summary>Generated code</summary>

```csharp
public class StatusArrayJsonConverterName : System.Text.Json.Serialization.JsonConverter<Status[]>
{
    public override Status[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) { /* ... */ }

    public override void Write(Utf8JsonWriter writer, Status[] values, JsonSerializerOptions options) { /* ... */ }
}

// Or

public class StatusArrayJsonConverterName : Newtonsoft.Json.JsonConverter
{
    public override bool CanConvert(Type objectType) { /* ... */ }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) { /* ... */ }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) { /* ... */ }
}
```

</details>

## ModelBinder

### Description

Generates an ASP. NET Core `IModelBinder` and a `ModelBinderAttribute` for binding query/form strings to the enum. Also generates an array model binder and attribute for `enum[]` .

### How to use

Apply `[ModelBinder]` to an enum. Use the generated attribute on action parameters or model properties together with `[FromQuery]` (or other binding attributes).

```csharp
[ModelBinder]
public enum Status
{
    Pending,
    Active,
    Done,
}

public class MyQueryModel
{
    [StatusModelBinder]
    [FromQuery(Name = "status")]
    public Status Status { get; set; }
}
```

<details>
<summary>Generated code</summary>

```csharp
public class StatusModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext) { /* ... */ }
}

public class StatusModelBinderAttribute : ModelBinderAttribute
{
    public StatusModelBinderAttribute()
    {
        BinderType = typeof(StatusModelBinder);
    }
}

// Also: StatusArrayModelBinder and StatusArrayModelBinderAttribute for Status[]
```

</details>

## ValueConverter

### Description

Generates an EF Core `ValueConverter<TEnum, string>` for storing the enum in the database as a string (name, snake_case, or numeric string). **Requires `[EnumExtensions]` on the same enum.**

Parameters: `ConversionStrategy` — `Name` , `SnakeCase` , or `Value` .

### How to use

Apply `[ValueConverter(ConversionStrategy.Name)]` to an enum. Use the generated converter in your EF Core model configuration.

```csharp
[EnumExtensions]
[ValueConverter(ConversionStrategy.Name)]
public enum Status
{
    Pending,
    Active,
    Done,
}

// Usage in DbContext
modelBuilder.Entity<MyEntity>()
    .Property(e => e.Status)
    .HasConversion(new StatusValueConverterName());
```

<details>
<summary>Generated code</summary>

```csharp
public class StatusValueConverterName() : ValueConverter<Status, string>(
    v => v.Name(),
    v => StatusExt.FromString(v));
```

</details>

## ValueArrayConverter

### Description

Generates an EF Core `ValueConverter<TEnum[], string[]>` for storing arrays of the enum as string arrays in the database. **Requires `[EnumExtensions]` on the same enum.**

Parameters: `ConversionStrategy` — `Name` , `SnakeCase` , or `Value` .

### How to use

Apply `[ValueArrayConverter(ConversionStrategy.Name)]` to an enum. Use in EF Core for properties of type `Status[]` (or your enum array).

```csharp
[EnumExtensions]
[ValueArrayConverter(ConversionStrategy.Name)]
public enum Status
{
    Pending,
    Active,
    Done,
}

// Usage
modelBuilder.Entity<MyEntity>()
    .Property(e => e.Statuses)
    .HasConversion(new StatusValueArrayConverterName());
```

<details>
<summary>Generated code</summary>

```csharp
public class StatusValueArrayConverterName() : ValueConverter<Status[], string[]>(
    v => v.Select(e => e.Name()).ToArray(),
    v => v.Select(StatusExt.FromString).ToArray());
```

</details>

## EnumTypeFor

### Description

Generates a wrapper class that maps each enum member to a field of a given type (e.g. for Unity serialization). The class has a field per enum value, a constructor, `Get(enum key)` , `Set(enum key, value)` , `Apply(enum key, func)` , and a `Values` property. Optional parameters: `customName` (class name), `unitySerializable` , `generateEditor` .

### How to use

Apply `[EnumTypeFor(typeof(YourType))]` to an enum. Optionally pass `customName` , `unitySerializable` , `generateEditor` . Multiple `[EnumTypeFor(...)]` on the same enum generate multiple wrapper classes.

```csharp
public class ColorConfig
{
    public string Label;
    public int Order;
}

[EnumTypeFor(typeof(ColorConfig))]
[EnumTypeFor(typeof(string), customName: "ColorLabels")]
public enum Color
{
    Red,
    Green,
    Blue,
}

// Usage
var wrapper = new ColorForColorConfig();
var redConfig = wrapper.Get(Color.Red);
wrapper.Set(Color.Blue, new ColorConfig { Label = "Blue", Order = 3 });
```

<details>
<summary>Generated code</summary>

```csharp
[System.Serializable]
public class ColorForColorConfig
{
    [UnityEngine.SerializeField] private ColorConfig Red;
    [UnityEngine.SerializeField] private ColorConfig Green;
    [UnityEngine.SerializeField] private ColorConfig Blue;

    public ColorForColorConfig() { }

    public ColorForColorConfig(ColorConfig red, ColorConfig green, ColorConfig blue)
    {
        this.Red = red;
        this.Green = green;
        this.Blue = blue;
    }

    public ColorConfig Get(Color key)
    {
        return key switch
        {
            Color.Red => Red,
            Color.Green => Green,
            Color.Blue => Blue,
            _ => throw new System.ArgumentOutOfRangeException(nameof(key), key, null),
        };
    }

    public void Set(Color key, ColorConfig value) { /* ... */ }
    public void Apply(Color key, System.Func<ColorConfig, ColorConfig> func) { /* ... */ }
    public ColorConfig[] Values => new[] { Red, Green, Blue };
}
```

</details>
