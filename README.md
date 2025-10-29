# NTDLS.Semaphore

📦 Be sure to check out the NuGet package: https://www.nuget.org/packages/NTDLS.Semaphore

## Pessimistic Critical Resource
Provides various classes to protect a variable from parallel / non-sequential thread access by always acquiring an exclusive lock on the resource.
Also allows for shared access, pessimistic locking, optimistic locking and dead-lock prevention lock patterns with lightweight cancellation.

**PessimisticCriticalResource using inline execution example:**
>An example using a PessimisticCriticalResource to envelope a variable and protect it from parallel execution,
> Note that there are nullable and non-nullable counterparts and also template/generics of each method to
> allow you to return various types from the delegate execution.
```csharp
public class Car
{
    public string? Name { get; set; }
    public int NumerOhWheels { get; set; }
}

public PessimisticCriticalResource<List<Car>> Cars { get; set; } = new();

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


**Multi PessimisticCriticalResource using inline execution example:**
>An example using a PessimisticCriticalResource to envelope a variable and protect it and others from parallel execution.
```
public class Car
{
    public string? Name { get; set; }
    public int NumerOhWheels { get; set; }
}

public PessimisticCriticalResource<List<Car>> Cars { get; set; } = new();
public PessimisticSemaphore OtherLock1 { get; set; } = new();
public PessimisticSemaphore OtherLock2 { get; set; } = new();
public PessimisticSemaphore OtherLock3 { get; set; } = new();

public void Add(Car car)
{
    Cars.Use((obj) => obj.Add(car));
}

public Car? GetByName(string name)
{
    return Cars.Use((obj) => obj.Where(o => o.Name == name).FirstOrDefault());
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

public Car? TryGet(string name, int timeout)
{
    return Cars.TryUseAll(new[] { OtherLock1, OtherLock2, OtherLock3 }, timeout, out bool wasLockObtained, (obj) =>
    {
        //We only get here if we are able to lock "Cars" and OtherLock1, OtherLock2 and OtherLock3
        return obj.Where(o => o.Name == name).FirstOrDefault();
    });
}
```


## Optimistic Critical Resource
Protects a variable from parallel / non-sequential thread access but controls read-only and exclusive
access separately to prevent read operations from blocking other read operations.it is up to the developer
to determine when each lock type is appropriate. Note: read-only locks only indicate intention, the resource
will not disallow modification of the resource, but this will lead to race conditions.

**OptimisticCriticalResource using inline execution example:**
>
>An example using a OptimisticCriticalResource to protect a portion of code from parallel execution while not allowing reads to block reads.
```csharp
public class Car
{
    public string? Name { get; set; }
    public int NumerOhWheels { get; set; }
}

public OptimisticCriticalResource<List<Car>> Cars { get; set; } = new();

public void Add(Car car)
{
    Cars.Write((obj) => obj.Add(car));
}

public Car? GetByName(string name)
{
    return Cars.Read((obj) => obj.Where(o=>o.Name == name).FirstOrDefault());
}

public bool TryAdd(Car car)
{
    //Since TryUse<T> can return values, we have to pass the result of the try out though a variable.
    Cars.TryWrite(out bool wasLockObtained, (obj) => obj.Add(car));
    return wasLockObtained;
}

public bool TryAdd(Car car, int timeout)
{
    //Since TryUse<T> can return values, we have to pass the result of the try out though a variable.
    Cars.TryWrite(out bool wasLockObtained, timeout, (obj) => obj.Add(car));
    return wasLockObtained;
}
```


## Critical Section
Protects an area of code from parallel / non-sequential thread access.

**PessimisticSemaphore using inline execution example:**
>
>An example using a PessimisticSemaphore to protect a portion of code from parallel execution.
```csharp
private PessimisticSemaphore _pessimisticSemaphore = new();

private int _value;

public int Value
{
    get
    {
        return _pessimisticSemaphore.Use(() => _value);
    }
    set
    {
        _pessimisticSemaphore.Use(() => _value = value);
    }
}
```

## Thread ownership tracking
If you need to keep track of which thread owns each semaphore and/or critical sections then
  you can enable "ThreadOwnershipTracking" by calling ThreadOwnershipTracking.Enable(). Once this
  is enabled, it is enabled for the life of the application so this is only for debugging
  deadlock/race-condition tracking.
You can evaluate the ownership by evaluating
  the dictonary "ThreadOwnershipTracking.LockRegistration" or and instance of
  "PessimisticSemaphore" or "PessimisticCriticalResource" CurrentOwnerThread.

**Enabling Thread Ownership Tracking**
>
>An example of enabling the thread ownerhsip mechanism.
```csharp
ThreadOwnershipTracking.Enable();
```

## License
[Apache-2.0](https://choosealicense.com/licenses/apache-2.0/)

