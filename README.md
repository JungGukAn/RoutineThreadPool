# RoutineThreadPool

RoutineThreadPool is a simple fixed count thread pool that support followings:

- You can select one or more threads to run your routine on.
- It provides a way to wait for specific amount of time, but does not block thread.
- You can forcely cancel your routine using cancellation token.

## Examples
- [Setting thread pool count](#setting-thread-pool-count)
- [Logging uncaught exception](#logging-uncaught-exception)
- [Execute after specific time](#execute-after-specific-time)
- [Execute repeatedly interval specific time](#execute-repeatedly-interval-specific-time)
- [Cancel forcely using CancellationToken](#cancel-forcely-using-cancellationtoken)
- [Shared thread pool](#shared-thread-pool)
- [Select threads](#select-threads)

## Setting thread pool count

You can set thread pool count by calling constructors like below.
```C#
public RoutineThreadPool(int threadPoolCount) 
```

It also supports constructors that do not set thread pool count. <br /> 
In this case, threads with the number of processors are automatically created.

``` C#
public RoutineThreadPool() 
```

## Logging uncaught exception
When you want to know and log uncaught exception raised in your code,<br />
Just set ILoggerFactory object in following constructors:

``` C#
public RoutineThreadPool(ILoggerFactory loggerFactory) 
// or
public RoutineThreadPool(ILoggerFactory loggerFactory, int threadPoolCount)
```

## Execute after specific time
Using `yield return TimeSpan` causes your code to run after specific time.
```C#
class Program
{
    static void Main(string[] args)
    {
        var threadPool = new RoutineThreadPool();
        threadPool.Start(OneTimeExecuteAfter5Seconds());

        PreventExit();
    }

    static IEnumerable<TimeSpan> OneTimeExecuteAfter5Seconds()
    {
        yield return TimeSpan.FromSeconds(5);
        Execute();
    }

    static void Execute()
    {
        // Do something
    }

    static void PreventExit()
    {
        ...
    }
}
```
## Execute repeatedly interval specific time
```C#
class Program
{
    static void Main(string[] args)
    {
        var threadPool = new RoutineThreadPool();
        threadPool.Start(ExecuteRepeatedly());

        PreventExit();
    }

    static IEnumerable<TimeSpan> ExecuteRepeatedly()
    {
        while(true)
        {
            Execute();
            yield return TimeSpan.FromMilliseconds(16);
        }
    }

    static void Execute()
    {
        // Do something
    }

    static void PreventExit()
    {
        ...
    }
}
```

## Cancel forcely using CancellationToken
RoutineThreadPool provides another way to quit your routine.
```C#
class Program
{
    static void Main(string[] args)
    {
        var threadPool = new RoutineThreadPool();
        
        var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(5));

        threadPool.Start(ExecuteRepeatedly(), cts.Token);

        PreventExit();
    }

    static IEnumerable<TimeSpan> ExecuteRepeatedly()
    {
        while(true)
        {
            Execute();
            yield return TimeSpan.FromMilliseconds(16);
        }
    }

    static void Execute()
    {
        // Do something
    }

    static void PreventExit()
    {
        ...
    }
}

```

## Shared thread pool
Shared thread pool count is equivalent of processor count. <br />
A way to access to shared thread pool is below:

```C#
RoutineThreadPool.Shared
```

## Select threads
You can select a thread. <br />
This help to remove locking when communication between objects in a same thread.

```C#
class Person
{
    private Room _room;

    public Person(Room room)
    {
        _room = room;
        RoutineThreadPool.Shared.Start(_room.ThreadIndex, RequestRoom());
    }

    IEnumerable<TimeSpan> RequestRoom()
    {
        yield return TimeSpan.FromSeconds(1);

        int count = _room.IncreaseCount();
        Console.WriteLine(count);
    }
}

class Room
{
    private int _count;

    public readonly int ThreadIndex;

    public Room()
    {
        ThreadIndex = 0;
    }

    // Don't need lock
    public int IncreaseCount()
    {
        // lock(this)
        // {
            _count += 1;
            return _count;
        // }
    }
}

class Program
{
    static void Main(string[] args)
    {
        var room = new Room();
        var people = new List<Person>();

        for (int i = 0; i < 10000; i++)
        {
            people.Add(new Person(room));
        }

        PreventExit();
    }

    static void PreventExit()
    {
        ...
    }
}

```

You can also select multiple threads or no specific threads. <br />
If you don't select threads, your routine can run on all threads.

But, your routine passed to `Start` method doesn't run in parallel. <br />
Another words, your routine runs on only one thread in same time.

This help to reduce situations where routines are delayed by busy threads. <br />
If any of these threads are not busy, The routine will run on time.

```C#
int minThreadIndex = 0;
int maxThreadIndex = 1;

// Select range from minThreadIndex to maxThreadIndex(include) index threads.
RoutineThreadPool.Shared.Start(minThreadIndex, maxThreadIndex, Routine());

// Select all threads
RoutineThreadPool.Shared.Start(0, RoutineThreadPool.Shared.ThreadCount - 1, Routine());
// Above is same as below 
RoutineThreadPool.Shared.Start(Routine());

```
