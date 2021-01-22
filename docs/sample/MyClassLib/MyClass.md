# MyClass

Namespace: MyClassLib

My class.

```csharp
public class MyClass : IMyInterface
```

Inheritance [object](https://docs.microsoft.com/en-us/dotnet/api/system.object) → [MyClass](MyClass.md)

Implements [IMyInterface](IMyInterface.md)

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

My enum [MyEnum](MyEnum.md)

```csharp
public MyEnum MyEnum { get; set; }
```

> #### Property Value
> 
> `MyEnum`<br>The enum value
> 

---

## Constructors

### MyClass()

Initializes a new instance of the [MyClass](MyClass.md) class.

```csharp
public MyClass()
```

> 

### MyClass(string, int)

Initializes a new instance of the [MyClass](MyClass.md) class.

```csharp
public MyClass(string firstParam, int secondParam)
```

> #### Parameters
> 
> firstParam : `string`<br>The first param.
> 
> secondParam : `int`<br>The second param.
> 

---

## Methods

### Do(string, int)

Do some thing.

```csharp
public void Do(string firstParam, int secondParam)
```

> #### Parameters
> 
> firstParam : `string`<br>The first param.
> 
> secondParam : `int`<br>The second param.
> 

#### Exceptions

Exception<br>Thrown when...

### Get(string)

Gets some thing.

```csharp
public string Get(string param)
```

> #### Parameters
> 
> param : `string`<br>The param.
> 
> #### Returns
> 
> string<br>An empty string.
> 

#### Exceptions

Exception<br>Thrown when...

### StaticMethod()

A static method.

```csharp
public static void StaticMethod()
```

> 

---

## Events

### MyEvent

My event.

```csharp
public event EventHandler<EventArgs> MyEvent;
```

---

[`< Index`](..\index.md)
