# MyDelegate

Namespace: MyClassLib

```csharp
public sealed class MyDelegate : System.MulticastDelegate
```

Inheritance [object](https://docs.microsoft.com/en-us/dotnet/api/system.object) → [Delegate](https://docs.microsoft.com/en-us/dotnet/api/system.delegate) → [MulticastDelegate](https://docs.microsoft.com/en-us/dotnet/api/system.multicastdelegate) → [MyDelegate](MyDelegate.md)

Implements [ICloneable](https://docs.microsoft.com/en-us/dotnet/api/system.icloneable), [ISerializable](https://docs.microsoft.com/en-us/dotnet/api/system.runtime.serialization.iserializable)

## Properties

### Target

```csharp
public object Target { get; }
```

> #### Property Value
> 
> `object`<br>
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

### MyDelegate(object, IntPtr)

```csharp
public MyDelegate(object object, IntPtr method)
```

> #### Parameters
> 
> object : `object`<br>
> 
> method : `IntPtr`<br>
> 

---

## Methods

### Invoke(string)

```csharp
public void Invoke(string str)
```

> #### Parameters
> 
> str : `string`<br>
> 

### BeginInvoke(string, AsyncCallback, object)

```csharp
public IAsyncResult BeginInvoke(string str, AsyncCallback callback, object object)
```

> #### Parameters
> 
> str : `string`<br>
> 
> callback : `AsyncCallback`<br>
> 
> object : `object`<br>
> 
> #### Returns
> 
> IAsyncResult<br>
> 

### EndInvoke(IAsyncResult)

```csharp
public void EndInvoke(IAsyncResult result)
```

> #### Parameters
> 
> result : `IAsyncResult`<br>
> 

---

[`< Index`](..\index.md)
