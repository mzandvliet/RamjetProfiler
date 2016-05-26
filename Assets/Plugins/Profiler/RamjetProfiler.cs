using System.Diagnostics;
using Debug = UnityEngine.Debug;

public static class RamjetProfiler {
    private static Stopwatch _stopwatch;
    private static string _name;

    static RamjetProfiler() {
        _stopwatch = new Stopwatch();
    }

    public static void BeginSample(string name) {
        _name = name;
        _stopwatch.Reset();
        _stopwatch.Start();
    }

    public static void EndSample() {
        _stopwatch.Stop();
        Debug.LogFormat("Profiler || {0} took {1:0.00}", _name, _stopwatch.Elapsed.TotalSeconds * 1000d);
        _name = "";
    }
}
