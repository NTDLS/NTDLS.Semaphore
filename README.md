# NTDLS.Semaphore
Provides various classes for to ensure sequential mult-threaded access to variables or sections of code.

>**Auto-release CriticalResource using inline execution example:**
>
>An example using a CriticalResource to envelope a variable andprotect it from parallel execution,
> Note that there are nullable and nonnullable counterpars and also template/generics of each method to
> allow you to return various types from the delegate execution.
```csharp
public class Car
{
	public string? Name { get; set; }
	public int NumerOhWheels { get; set; }
}

public CriticalResource<List<Car>> Cars { get; set; } = new();

public void Add(Car car)
{
	Cars.Use((obj) => obj.Add(car));
}

public Car? GetByName(string name)
{
	return Cars.Use((obj) => obj.Where(o=>o.Name == name).FirstOrDefault());
}

public bool TryAdd(Car car)
{
	//Since TryUse<T> can return values, we have to pass the result of the try out though a variable.
	Cars.TryUse(out bool wasLockObtained, (obj) => obj.Add(car));
	return wasLockObtained;
}

public bool TryAdd(Car car, int timeout)
{
	//Since TryUse<T> can return values, we have to pass the result of the try out though a variable.
	Cars.TryUse(out bool wasLockObtained, timeout, (obj) => obj.Add(car));
	return wasLockObtained;
}
```


>**Auto-release CriticalSection example:**
>
>An example using a CriticalSection to protect a portion of code from parallel execution,
```csharp
public CriticalSection SyncObjectLock { get; } = new();

private int _value;

public int Value
{
	get
	{
		using (SyncObjectLock.Lock())
		{
			return _value;
		}
	}
	set
	{
		using (SyncObjectLock.Lock())
		{
			_value = value;
		}
	}
}
```

>**Auto-release CriticalSection using inline execution example:**
>
>An example using a CriticalSection to protect a portion of code from parallel execution,
```csharp
public CriticalSection SyncObjectLock { get; } = new();

private int _value;

public int Value
{
	get
	{
		return SyncObjectLock.Use(() => _value);
	}
	set
	{
		SyncObjectLock.Use(() => _value = value);
	}
}
```
