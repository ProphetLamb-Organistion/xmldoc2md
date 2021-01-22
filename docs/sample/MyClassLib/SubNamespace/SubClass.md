# SubClass

Namespace: MyClassLib.SubNamespace

Sub class from [MyClass](..\MyClass.md)

```csharp
public class SubClass : MyClassLib.MyClass
```

Inheritance [object](https://docs.microsoft.com/en-us/dotnet/api/system.object) → [MyClass](..\MyClass.md) → [SubClass](SubClass.md)

Implements [IMyInterface](..\IMyInterface.md)

## Properties

### MyProperty

My property ([string](https://docs.microsoft.com/en-us/dotnet/api/system.string)).

```csharp
public string MyProperty { get; protected set; }
```

> #### Property Value
> 
> `string`<br>The property value.
> 

### MyEnum

My enum [MyEnum](..\MyEnum.md)

```csharp
public MyEnum MyEnum { get; set; }
```

> #### Property Value
> 
> `MyEnum`<br>The enum value
> 

---

## Constructors

### SubClass()

```csharp
public SubClass()
```

> 

---

## Methods

### ToString()

Convert instance to string.

```csharp
public string ToString()
```

> #### Returns
> 
> string<br>A string.
> 

---

## Events

### MyEvent

My event.

```csharp
public event EventHandler<EventArgs> MyEvent;
```

---

[`< Index`](..\..\index.md)
