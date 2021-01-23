# MyDelegate

Namespace: MyClassLib

```csharp
public sealed class MyDelegate : System.MulticastDelegate
```

Inheritance [Object](https://docs.microsoft.com/en-us/dotnet/api/system.object) → [Delegate](https://docs.microsoft.com/en-us/dotnet/api/system.delegate) → [MulticastDelegate](https://docs.microsoft.com/en-us/dotnet/api/system.multicastdelegate) → [MyDelegate](MyDelegate.md)

Implements [ICloneable](https://docs.microsoft.com/en-us/dotnet/api/system.icloneable), [ISerializable](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.serialization.iserializable)

## Properties

### Target

```csharp
public Object Target { get; }
```

> #### Property Value
> 
> `Object`<br>
> 

### Method

```csharp
public MethodInfo Method { get; }
```

> #### Property Value
> 
> `MethodInfo`<br>
> 

---

## Constructors

### MyDelegate(Object, IntPtr)

```csharp
public MyDelegate(Object object, IntPtr method)
```

> #### Parameters
> 
> object : `Object`<br>
> 
> method : `IntPtr`<br>
> 

---

## Methods

### Invoke(String)

```csharp
public Void Invoke(String str)
```

> #### Parameters
> 
> str : `String`<br>
> 
> #### Returns
> 
> Void<br>
> 

### BeginInvoke(String, AsyncCallback, Object)

```csharp
public IAsyncResult BeginInvoke(String str, AsyncCallback callback, Object object)
```

> #### Parameters
> 
> str : `String`<br>
> 
> callback : `AsyncCallback`<br>
> 
> object : `Object`<br>
> 
> #### Returns
> 
> IAsyncResult<br>
> 

### EndInvoke(IAsyncResult)

```csharp
public Void EndInvoke(IAsyncResult result)
```

> #### Parameters
> 
> result : `IAsyncResult`<br>
> 
> #### Returns
> 
> Void<br>
> 

---

[`< Index`](..\index.md)
