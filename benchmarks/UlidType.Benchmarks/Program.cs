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

    var artifactsFolder = "./BenchmarkDotNet.Artifacts/results";
    var options = ConfigOptions.StopOnFirstError;

    if (Environment.GetEnvironmentVariable("CI", EnvironmentVariableTarget.Process)?.ToLowerInvariant() is "true")
        options |= ConfigOptions.DisableOptimizationsValidator;
    ;

    for (var i = 0; i < args.Length; i++)
        if (args[i] == "--artifacts" && ++i < args.Length)
        {
            artifactsFolder = args[i];
            break;  // this is all we needed to know from the command line arguments, so we can stop processing them
        }

    return config
            .WithArtifactsPath(artifactsFolder)
            .WithOptions(options)
            ;
}
