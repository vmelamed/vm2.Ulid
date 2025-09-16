namespace vm2.Benchmarks;

public static class Program
{
    public static void Main(string[] args)
    {
        var artifactsFolder = ".\\BenchmarkDotNet.Artifacts\\results";

        for (var i = 0; i < args.Length; i++)
            if ((args[i] == "--artifacts" || args[i] == "i") && i+1 < args.Length)
                artifactsFolder = args.Length >= 1 ? args[0] : ".\\BenchmarkDotNet.Artifacts\\results";

        BenchmarkSwitcher
            .FromAssembly(typeof(Program).Assembly)
            .Run(
                args,
#if DEBUG
                        // for debugging the benchmarks only
                        new DebugInProcessConfig()
#else
                        DefaultConfig
                            .Instance
                            .WithArtifactsPath(artifactsFolder)
                            .WithOptions(ConfigOptions.StopOnFirstError)
#endif
            );
    }
}