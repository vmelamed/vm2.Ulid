// SPDX-License-Identifier: MIT
// Copyright (c) 2025 Val Melamed

BenchmarkSwitcher
    .FromAssembly(typeof(Program).Assembly)
    .Run(args, GetConfig(args))
    ;

static IConfig GetConfig(string[] args)
{
    var config =
#if DEBUG
                new DebugInProcessConfig()   // for debugging the benchmarks only
#else
                DefaultConfig.Instance
#endif
                ;

    var options = ConfigOptions.StopOnFirstError;
    var artifactsFolder = "./BenchmarkDotNet.Artifacts/results";

    for (var i = 0; i < args.Length; i++)
        switch (args[i])
        {
            case "--artifacts":
                if (i + 1 < args.Length)
                    artifactsFolder = args[i + 1];
                break;

            case "--disable-optimizations-validator":
                options |= ConfigOptions.DisableOptimizationsValidator;
                break;
        }

    return config
            .WithArtifactsPath(artifactsFolder)
            .WithOptions(options)
            ;
}
