# MyClass

Namespace: MyClassLib

My class.

```csharp
public class MyClass : System.Object, IMyInterface
```

Inheritance [Object](https://docs.microsoft.com/en-us/dotnet/api/system.object) → [MyClass](MyClass.md)

Implements [IMyInterface](IMyInterface.md)

## Properties

### MyProperty

My property ([string](https://docs.microsoft.com/en-us/dotnet/api/system.string)).

```csharp
public String MyProperty { get; protected set; }
```

> #### Property Value
> 
> `String`<br>The property value.
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

### MyClass(String, Int32)

Initializes a new instance of the [MyClass](MyClass.md) class.

```csharp
public MyClass(String firstParam, Int32 secondParam)
```

> #### Parameters
> 
> firstParam : `String`<br>The first param.
> 
> secondParam : `Int32`<br>The second param.
> 

---

## Methods

### Do(String, Int32)

Do some thing.

```csharp
public Void Do(String firstParam, Int32 secondParam)
```

> #### Parameters
> 
> firstParam : `String`<br>The first param.
> 
> secondParam : `Int32`<br>The second param.
> 
> #### Returns
> 
> Void<br>
> 

#### Exceptions

Exception<br>Thrown when...

### Get(String)

Gets some thing.

```csharp
public String Get(String param)
```

> #### Parameters
> 
> param : `String`<br>The param.
> 
> #### Returns
> 
> String<br>An empty string.
> 

#### Exceptions

Exception<br>Thrown when...

### StaticMethod()

A static method.

```csharp
public static Void StaticMethod()
```

> #### Returns
> 
> Void<br>
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
