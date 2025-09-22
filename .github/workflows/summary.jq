{
    HostEnvironmentInfo: .HostEnvironmentInfo,
    Totals: {
        Mean: [.Benchmarks.[].Statistics.Mean] | add | floor,
        Median: [.Benchmarks.[].Statistics.Median] | add | floor,
        Memory: {
            Gen0Collections: [.Benchmarks[].Memory.Gen0Collections] | add,
            Gen1Collections: [.Benchmarks[].Memory.Gen1Collections] | add,
            Gen2Collections: [.Benchmarks[].Memory.Gen2Collections] | add,
            AllocatedPerOp:  [.Benchmarks[].Memory.BytesAllocatedPerOperation] | add
        }
    },
    Benchmarks: [
        .Benchmarks[] | {
            DisplayInfo: .DisplayInfo,
            MethodTitle: .MethodTitle,
            Parameters:  .Parameters,
            Statistics: {
                Min:        .Statistics.Min | floor,
                Mean:       .Statistics.Mean | floor,
                Median:     .Statistics.Median | floor,
                Max:        .Statistics.Max | floor
            },
            Memory: .Memory
        }
    ]
}
