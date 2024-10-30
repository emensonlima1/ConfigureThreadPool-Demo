using System.Collections.Concurrent;

ConfigureThreadPool();

var keysAndValues = new ConcurrentDictionary<string, string>();
for (var i2 = 0; i2 < 100000; i2++)
{
    keysAndValues[$"key_{i2}"] = $"value_{i2}";
}

var task = Task.Run(MonitorThreadPool);

while (true)
{
    Parallel.ForEach(keysAndValues, new ParallelOptions { MaxDegreeOfParallelism = 10000 }, kvp =>
    {
    });
}


static void ConfigureThreadPool()
{
    var cores = Environment.ProcessorCount;
    var maxWorkerThreads = Math.Max(cores * 2, 8);
    var maxIoThreads = maxWorkerThreads * 2;
    var minWorkerThreads = Math.Max(1, cores);
    var minIoThreads = Math.Max(1, cores);

    ThreadPool.SetMinThreads(minWorkerThreads, minIoThreads);
    ThreadPool.SetMaxThreads(maxWorkerThreads, maxIoThreads);

    Console.WriteLine($"Initial ThreadPool configured: MinWorkerThreads={minWorkerThreads}, MaxWorkerThreads={maxWorkerThreads}");
}

static void MonitorThreadPool()
{
    while (true)
    {
        ThreadPool.GetAvailableThreads(out var availableWorkerThreads, out var availableIoThreads);
        ThreadPool.GetMinThreads(out var minWorkerThreads, out var minIoThreads);
        ThreadPool.GetMaxThreads(out var maxWorkerThreads, out var maxIoThreads);

        var usedWorkerThreads = maxWorkerThreads - availableWorkerThreads;

        Console.WriteLine($"[Monitor] MaxWorkerThreads: {maxWorkerThreads}, AvailableWorkerThreads: {availableWorkerThreads}, Worker Threads in Use: {usedWorkerThreads}/{maxWorkerThreads}");

        AdjustThreadPool(usedWorkerThreads, maxWorkerThreads, minWorkerThreads, maxIoThreads, minIoThreads);
    }
}

static void AdjustThreadPool(int usedWorkerThreads, int maxWorkerThreads, int minWorkerThreads, int maxIoThreads, int minIoThreads)
{
    const int adjustmentThreshold = 10;

    // Increase max threads if usage is high
    if (usedWorkerThreads > maxWorkerThreads * 0.8)
    {
        var newMaxWorkerThreads = Math.Min(maxWorkerThreads + adjustmentThreshold, Environment.ProcessorCount * 4);
        var newMaxIoThreads = Math.Min(maxIoThreads + adjustmentThreshold / 2, newMaxWorkerThreads * 2);

        ThreadPool.SetMaxThreads(newMaxWorkerThreads, newMaxIoThreads);
        Console.WriteLine($"[Adjustment] Increasing Max Worker Threads to {newMaxWorkerThreads}, Max IO Threads to {newMaxIoThreads}");
    }
    // Decrease max threads if usage is low
    else if (usedWorkerThreads < maxWorkerThreads * 0.3)
    {
        var newMaxWorkerThreads = Math.Max(minWorkerThreads, maxWorkerThreads - adjustmentThreshold);
        var newMaxIoThreads = Math.Max(minIoThreads, maxIoThreads - adjustmentThreshold / 2);
        ThreadPool.SetMaxThreads(newMaxWorkerThreads, newMaxIoThreads);

        Console.WriteLine($"[Adjustment] Reducing Max Worker Threads to {newMaxWorkerThreads}, Max IO Threads to {newMaxIoThreads}");
    }

    // Adjust minimum threads
    if (usedWorkerThreads > minWorkerThreads && usedWorkerThreads > maxWorkerThreads * 0.8)
    {
        var newMinWorkerThreads = Math.Min(maxWorkerThreads, minWorkerThreads + 2);
        var newMinIoThreads = Math.Min(maxIoThreads, minIoThreads + 1);
        ThreadPool.SetMinThreads(newMinWorkerThreads, newMinIoThreads);

        Console.WriteLine($"[Adjustment] Increasing Min Worker Threads to {newMinWorkerThreads}, Min IO Threads to {newMinIoThreads}");
    }
    else if (usedWorkerThreads < minWorkerThreads * 0.5)
    {
        var newMinWorkerThreads = Math.Max(1, minWorkerThreads - 1);
        var newMinIoThreads = Math.Max(1, minIoThreads - 1);
        ThreadPool.SetMinThreads(newMinWorkerThreads, newMinIoThreads);

        Console.WriteLine($"[Adjustment] Reducing Min Worker Threads to {newMinWorkerThreads}, Min IO Threads to {newMinIoThreads}");
    }
}