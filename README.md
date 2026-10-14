# NTDLS.Semaphore

**NTDLS.Semaphore** is a .NET library (net8.0 / net9.0 / net10.0) that makes it easy to guarantee sequential access to variables and sections of code in multi-threaded applications. Instead of sprinkling `lock` statements throughout your code, you wrap the data or the critical section once and then execute work against it through a small set of well-named methods: blocking (`Use`, `Read`, `Write`), non-blocking (`TryUse`, `TryRead`, `TryWrite`), timed variants with millisecond timeouts, and multi-lock variants for when several locks must be held at once.

📦 Be sure to check out the NuGet package: https://www.nuget.org/packages/NTDLS.Semaphore

## When to use this package

Reach for NTDLS.Semaphore when you need to protect shared state from parallel or non-sequential thread access and want more control than a plain `lock` statement gives you:

- **You want to guard a variable, not just a block of code.** `PessimisticCriticalResource<T>` and `OptimisticCriticalResource<T>` encapsulate a value together with its lock, so every access goes through the same protected entry points.
- **You want reads to not block other readers.** `OptimisticCriticalResource<T>` acquires read-only and exclusive locks separately, so concurrent `Read` operations do not queue behind each other.
- **You want to avoid blocking the caller.** The `Try*` overloads attempt the lock once (or for a bounded number of milliseconds) and report whether they succeeded instead of waiting indefinitely — useful for fire-and-forget updates, UI code, or back-pressure logic.
- **You need to hold several locks at once.** The `...All` overloads acquire the resource's own lock plus an array of additional `ICriticalSection` locks atomically, releasing everything cleanly if any one of them cannot be obtained.
- **You want deadlock-aware retries.** `DeadlockAvoidanceTryRead` / `DeadlockAvoidanceTryWrite` retry a timed lock attempt while a cancellation token stays uncancelled.
- **You are debugging deadlocks or race conditions.** Optional process-wide thread ownership tracking records which thread holds which lock.

## Usage

### Pessimistic critical resource

`PessimisticCriticalResource<T>` protects a reference-type variable by always taking an exclusive lock before executing a delegate against it. There are nullable and non-nullable return variants of each method so you can return any type from the delegate.

```csharp
using NTDLS.Semaphore;

public class Car
{
    public string? Name { get; set; }
    public int NumberOfWheels { get; set; }
}

// Wrap the list together with its lock.
public PessimisticCriticalResource<List<Car>> Cars { get; } = new();

// Blocks until the lock is acquired, then adds the car.
public void Add(Car car)
{
    Cars.Use((obj) => obj.Add(car));
}

// Returns the first matching car; UseNullable<T> supports nullable results.
public Car? GetByName(string name)
{
    return Cars.UseNullable((obj) => obj.Where(o => o.Name == name).FirstOrDefault());
}

// TryUse does not block: it returns false if the lock could not be obtained.
public bool TryAdd(Car car)
{
    return Cars.TryUse((obj) => obj.Add(car));
}

// Timed variant: wait up to 500 ms for the lock.
public bool TryAdd(Car car, int timeoutMilliseconds)
{
    return Cars.TryUse(timeoutMilliseconds, (obj) => obj.Add(car));
}

// Peek executes without taking the lock at all (unsafe unless you know no
// other thread can touch the value concurrently).
public void PrintCount()
{
    Cars.Peek((obj) => Console.WriteLine(obj.Count));
}
```

#### Locking multiple resources at once

`TryUseAll` acquires the resource's own lock plus every lock in the supplied array; if any lock cannot be obtained, the ones already held are released and `false` is returned.

```csharp
using NTDLS.Semaphore;

public PessimisticCriticalResource<List<Car>> Cars { get; } = new();
public PessimisticSemaphore OtherLock1 { get; } = new();
public PessimisticSemaphore OtherLock2 { get; } = new();

// Only runs if "Cars" AND both other locks can be obtained within the timeout.
public Car? TryGet(string name, int timeoutMilliseconds)
{
    // Nullable-result overload: returns null if any lock could not be obtained.
    return Cars.TryUseAllNullable(
        new ICriticalSection[] { OtherLock1, OtherLock2 },
        timeoutMilliseconds,
        out bool wasLockObtained,
        (obj) => obj.Where(o => o.Name == name).FirstOrDefault());
}
```

### Optimistic critical resource

`OptimisticCriticalResource<T>` separates read-only and exclusive access so that read operations do not block other read operations. It is up to you to pick the right lock kind per operation. Note: read-only locks only express intention — nothing stops a reader from mutating the value, but doing so will cause race conditions.

```csharp
using NTDLS.Semaphore;

public class Car
{
    public string? Name { get; set; }
    public int NumberOfWheels { get; set; }
}

public OptimisticCriticalResource<List<Car>> Cars { get; } = new();

// Exclusive access: blocks until no other writer holds the lock.
public void Add(Car car)
{
    Cars.Write((obj) => obj.Add(car));
}

// Read-only access: does not block other readers.
// The delegate SHOULD NOT modify the passed value.
public Car? GetByName(string name)
{
    return Cars.ReadNullable((obj) => obj.Where(o => o.Name == name).FirstOrDefault());
}

// Non-blocking write: returns false instead of waiting if the lock is busy.
public bool TryAdd(Car car)
{
    return Cars.TryWrite((obj) => obj.Add(car));
}

// Timed non-blocking write: wait up to 500 ms for the exclusive lock.
public bool TryAdd(Car car, int timeoutMilliseconds)
{
    return Cars.TryWrite(timeoutMilliseconds, (obj) => obj.Add(car));
}
```

#### Reading across multiple resources

`ReadAll` / `TryReadAll` take the resource's own read-only lock plus read-only locks on an array of other sections, so several resources can be snapshotted consistently without blocking writers on unrelated resources.

```csharp
using NTDLS.Semaphore;

public OptimisticCriticalResource<List<Car>> Cars { get; } = new();
public OptimisticCriticalResource<List<string>> GarageNames { get; } = new();

// Acquires read locks on Cars plus every section in the supplied array.
public bool Snapshot(out List<Car>? cars, out List<string>? names)
{
    cars = null;
    names = null;

    // Waits up to 1000 ms; returns false if any read lock could not be obtained.
    bool wasLockObtained = Cars.TryReadAll(
        new ICriticalSection[] { GarageNames },
        1000,
        (obj) =>
        {
            // The delegate only runs once all read locks are held.
            cars = new List<Car>(obj);
            names = new List<string>();
        });

    if (wasLockObtained)
    {
        // Read the second resource under its own read-only lock.
        names = GarageNames.ReadNullable((g) => g.ToList());
    }

    return wasLockObtained;
}
```

### Critical section (semaphore)

When you only need to protect a region of code rather than a specific variable, use `PessimisticSemaphore` directly.

```csharp
using NTDLS.Semaphore;

private readonly PessimisticSemaphore _pessimisticSemaphore = new();
private int _value;

public int Value
{
    get
    {
        // Use<T> blocks until the section is free, then returns the delegate result.
        return _pessimisticSemaphore.Use(() => _value);
    }
    set
    {
        _pessimisticSemaphore.Use(() => _value = value);
    }
}

// TryUse does not block: returns true only if the section was free.
public bool TrySetValue(int newValue)
{
    return _pessimisticSemaphore.TryUse(() => _value = newValue);
}
```

### Thread ownership tracking

If you need to see which thread owns each semaphore or critical section, enable process-wide ownership tracking. Once enabled it stays enabled for the life of the process, so treat it as a debugging aid for hunting deadlocks and race conditions.

```csharp
using NTDLS.Semaphore;

// Enables tracking for the whole process (debugging only).
ThreadLockOwnershipTracking.Enable();

// Afterwards, inspect PessimisticSemaphore.Ownership / OptimisticSemaphore.Ownership
// (Dictionary<int, HeldLock>, keyed by thread id) to see who holds what.
```

## License

[MIT](https://choosealicense.com/licenses/mit/)
