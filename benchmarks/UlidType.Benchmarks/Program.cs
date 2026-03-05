// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Val Melamed

BenchmarkSwitcher
    .FromAssembly(typeof(Program).Assembly)
    .Run(args, GetConfig(args))
    ;

static IConfig GetConfig(string[] args)
{
#if DEBUG
    var config = new DebugInProcessConfig();   // for debugging the benchmarks only
#else
    var config = DefaultConfig.Instance;
#endif
    var options = ConfigOptions.StopOnFirstError;
    var artifactsFolder = "./BenchmarkDotNet.Artifacts/results";

    for (var i = 0; i < args.Length; i++)
        switch (args[i])
        {
            case "--artifacts":
                if (i + 1 < args.Length)
                    artifactsFolder = args[i + 1];
                else
                    Console.WriteLine($"Warning: --artifacts option requires a path argument. Using the default path {artifactsFolder}.");
                break;

            case "--disable-optimizations-validator":
                options |= ConfigOptions.DisableOptimizationsValidator;
                break;
        }

    return config
#if RELEASE
            .WithArtifactsPath(artifactsFolder)
            .WithOptions(options)
#endif
            ;
}
