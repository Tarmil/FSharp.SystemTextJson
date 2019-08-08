``` ini

BenchmarkDotNet=v0.11.5, OS=macOS Mojave 10.14.5 (18F132) [Darwin 18.6.0]
Intel Core i7-7820HQ CPU 2.90GHz (Kaby Lake), 1 CPU, 8 logical and 4 physical cores
.NET Core SDK=3.0.100-preview7-012821
  [Host]   : .NET Core 3.0.0-preview7-27912-14 (CoreCLR 4.700.19.32702, CoreFX 4.700.19.36209), 64bit RyuJIT DEBUG
  ShortRun : .NET Core 3.0.0-preview7-27912-14 (CoreCLR 4.700.19.32702, CoreFX 4.700.19.36209), 64bit RyuJIT

Job=ShortRun  IterationCount=3  LaunchCount=1  
WarmupCount=3  

```
|      Method | Runtime | sleepTime |               Mean |             Error |          StdDev |  Gen 0 | Gen 1 | Gen 2 | Allocated |
|------------ |-------- |---------- |-------------------:|------------------:|----------------:|-------:|------:|------:|----------:|
|      **Thread** |    **Core** |         **0** |         **458.675 ns** |        **93.3957 ns** |       **5.1193 ns** |      **-** |     **-** |     **-** |         **-** |
|        Task |    Core |         0 |           2.638 ns |         0.1104 ns |       0.0061 ns |      - |     - |     - |         - |
| AsyncToTask |    Core |         0 |      12,241.998 ns |     8,935.4754 ns |     489.7836 ns | 0.2899 |     - |     - |     611 B |
| AsyncToSync |    Core |         0 |      15,524.103 ns |     2,711.1604 ns |     148.6078 ns | 0.3052 |     - |     - |    1208 B |
|      Thread |    Mono |         0 |                 NA |                NA |              NA |      - |     - |     - |         - |
|        Task |    Mono |         0 |                 NA |                NA |              NA |      - |     - |     - |         - |
| AsyncToTask |    Mono |         0 |                 NA |                NA |              NA |      - |     - |     - |         - |
| AsyncToSync |    Mono |         0 |                 NA |                NA |              NA |      - |     - |     - |         - |
|      **Thread** |    **Core** |         **1** |   **1,245,680.900 ns** |   **152,878.9237 ns** |   **8,379.8097 ns** |      **-** |     **-** |     **-** |         **-** |
|        Task |    Core |         1 |   1,262,077.624 ns |   290,683.9133 ns |  15,933.3662 ns |      - |     - |     - |     232 B |
| AsyncToTask |    Core |         1 |   1,299,835.912 ns |   150,832.8319 ns |   8,267.6565 ns |      - |     - |     - |     672 B |
| AsyncToSync |    Core |         1 |   1,283,742.656 ns |   174,601.9418 ns |   9,570.5216 ns |      - |     - |     - |    1208 B |
|      Thread |    Mono |         1 |                 NA |                NA |              NA |      - |     - |     - |         - |
|        Task |    Mono |         1 |                 NA |                NA |              NA |      - |     - |     - |         - |
| AsyncToTask |    Mono |         1 |                 NA |                NA |              NA |      - |     - |     - |         - |
| AsyncToSync |    Mono |         1 |                 NA |                NA |              NA |      - |     - |     - |         - |
|      **Thread** |    **Core** |        **15** |  **17,996,619.635 ns** | **2,237,816.9045 ns** | **122,662.2957 ns** |      **-** |     **-** |     **-** |         **-** |
|        Task |    Core |        15 |  18,141,758.609 ns | 3,634,650.6904 ns | 199,227.4689 ns |      - |     - |     - |     232 B |
| AsyncToTask |    Core |        15 |  18,161,297.927 ns |   515,590.3864 ns |  28,261.2489 ns |      - |     - |     - |     672 B |
| AsyncToSync |    Core |        15 |  18,131,542.302 ns | 3,462,740.2942 ns | 189,804.4800 ns |      - |     - |     - |    1208 B |
|      Thread |    Mono |        15 |                 NA |                NA |              NA |      - |     - |     - |         - |
|        Task |    Mono |        15 |                 NA |                NA |              NA |      - |     - |     - |         - |
| AsyncToTask |    Mono |        15 |                 NA |                NA |              NA |      - |     - |     - |         - |
| AsyncToSync |    Mono |        15 |                 NA |                NA |              NA |      - |     - |     - |         - |
|      **Thread** |    **Core** |       **100** | **104,472,800.200 ns** | **4,812,170.1650 ns** | **263,771.2847 ns** |      **-** |     **-** |     **-** |         **-** |
|        Task |    Core |       100 | 104,661,806.867 ns | 4,295,487.0718 ns | 235,450.1409 ns |      - |     - |     - |     232 B |
| AsyncToTask |    Core |       100 | 104,638,460.267 ns | 6,275,012.7078 ns | 343,954.6206 ns |      - |     - |     - |     672 B |
| AsyncToSync |    Core |       100 | 104,720,712.667 ns | 5,892,833.0704 ns | 323,006.0651 ns |      - |     - |     - |    1208 B |
|      Thread |    Mono |       100 |                 NA |                NA |              NA |      - |     - |     - |         - |
|        Task |    Mono |       100 |                 NA |                NA |              NA |      - |     - |     - |         - |
| AsyncToTask |    Mono |       100 |                 NA |                NA |              NA |      - |     - |     - |         - |
| AsyncToSync |    Mono |       100 |                 NA |                NA |              NA |      - |     - |     - |         - |

Benchmarks with issues:
  SleepMarks.Thread: ShortRun(Runtime=Mono, IterationCount=3, LaunchCount=1, WarmupCount=3) [sleepTime=0]
  SleepMarks.Task: ShortRun(Runtime=Mono, IterationCount=3, LaunchCount=1, WarmupCount=3) [sleepTime=0]
  SleepMarks.AsyncToTask: ShortRun(Runtime=Mono, IterationCount=3, LaunchCount=1, WarmupCount=3) [sleepTime=0]
  SleepMarks.AsyncToSync: ShortRun(Runtime=Mono, IterationCount=3, LaunchCount=1, WarmupCount=3) [sleepTime=0]
  SleepMarks.Thread: ShortRun(Runtime=Mono, IterationCount=3, LaunchCount=1, WarmupCount=3) [sleepTime=1]
  SleepMarks.Task: ShortRun(Runtime=Mono, IterationCount=3, LaunchCount=1, WarmupCount=3) [sleepTime=1]
  SleepMarks.AsyncToTask: ShortRun(Runtime=Mono, IterationCount=3, LaunchCount=1, WarmupCount=3) [sleepTime=1]
  SleepMarks.AsyncToSync: ShortRun(Runtime=Mono, IterationCount=3, LaunchCount=1, WarmupCount=3) [sleepTime=1]
  SleepMarks.Thread: ShortRun(Runtime=Mono, IterationCount=3, LaunchCount=1, WarmupCount=3) [sleepTime=15]
  SleepMarks.Task: ShortRun(Runtime=Mono, IterationCount=3, LaunchCount=1, WarmupCount=3) [sleepTime=15]
  SleepMarks.AsyncToTask: ShortRun(Runtime=Mono, IterationCount=3, LaunchCount=1, WarmupCount=3) [sleepTime=15]
  SleepMarks.AsyncToSync: ShortRun(Runtime=Mono, IterationCount=3, LaunchCount=1, WarmupCount=3) [sleepTime=15]
  SleepMarks.Thread: ShortRun(Runtime=Mono, IterationCount=3, LaunchCount=1, WarmupCount=3) [sleepTime=100]
  SleepMarks.Task: ShortRun(Runtime=Mono, IterationCount=3, LaunchCount=1, WarmupCount=3) [sleepTime=100]
  SleepMarks.AsyncToTask: ShortRun(Runtime=Mono, IterationCount=3, LaunchCount=1, WarmupCount=3) [sleepTime=100]
  SleepMarks.AsyncToSync: ShortRun(Runtime=Mono, IterationCount=3, LaunchCount=1, WarmupCount=3) [sleepTime=100]
