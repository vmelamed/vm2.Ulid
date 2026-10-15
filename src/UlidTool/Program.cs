// SPDX-License-Identifier: MIT
// Copyright (c) 2025-2026 Val Melamed

return UlidToolApp.Run(args);

internal static class UlidToolApp
{
    internal const int MinUlidCount = 1;
    internal const int MaxUlidCount = 10000;

    internal static int Run(string[] args, TextWriter? output = null)
    {
        RootCommand rootCommand = CreateRootCommand();
        var parseResult = rootCommand.Parse(args);

        TextWriter? originalOut = null;
        if (output is not null)
        {
            originalOut = Console.Out;
            Console.SetOut(output);
        }

        try
        {
            if (parseResult.Errors.Count > 0)
            {
                Console.WriteLine("Error parsing command line arguments:");
                foreach (var error in parseResult.Errors)
                    Console.WriteLine($"  {error.Message}");
                return 1;
            }

            return parseResult.Invoke();
        }
        finally
        {
            if (output is not null && originalOut is not null)
                Console.SetOut(originalOut);
        }
    }

    internal static RootCommand CreateRootCommand()
    {
        Option<int> numberOption = new("--number", "-n")
        {
            HelpName = "number",
            Description = $"""
            The number of ULIDs to generate. Must be from {MinUlidCount} to {MaxUlidCount}.

            """,
            Required = false,
            Arity = ArgumentArity.ExactlyOne,
            DefaultValueFactory = _ => 1,
            Validators = {
                result =>
                {
                    if (result.GetValueOrDefault<int>() is < MinUlidCount or > MaxUlidCount)
                        result.AddError($"The number of ULIDs to generate must be from {MinUlidCount} to {MaxUlidCount}.");
                }
            }
        };

        Option<string> formatOption = new("--format", "-f")
        {
            HelpName = "format",
            Description = """
            The format of the string representation can be one of the following
            (case-insensitive, can be abbreviated to any unique prefix of):
              - ULID: 26-character, Base32 (the default)
              - GUID: 36-character, hex 8-4-4-4-12
              - detailed: multi-line display of each ULID as ULID, GUID
                and its components (timestamp, random bytes, etc.)

            """,
            Required = false,
            Arity = ArgumentArity.ExactlyOne,
            DefaultValueFactory = _ => "ulid",
            Validators = {
                result =>
                {
                    var value = result.GetValueOrDefault<string>()?.ToLowerInvariant();
                    if (string.IsNullOrWhiteSpace(value) ||
                        !"ulid".StartsWith(value) &&
                        !"guid".StartsWith(value) &&
                        !"detailed".StartsWith(value))
                        result.AddError($"The specified format is not valid: `{value}`. Valid formats are: `ulid`, `guid`, or `detailed` (or any unique prefix of these).");
                }
            }
        };

        RootCommand rootCommand = new("Generate one or more ULIDs (Universally Unique Lexicographically Sortable Identifiers)")
        {
            numberOption,
            formatOption
        };

        rootCommand.SetAction(
            parseResult =>
            {
                // the validator accepts any letter case, so the action must normalize the same way
                string format = parseResult.GetValue(formatOption)!.ToLowerInvariant();

                for (var i = 0; i < parseResult.GetValue(numberOption); i++)
                {
                    var ulid = Ulid.NewUlid();

                    switch (format)
                    {
                        case string f when "ulid".StartsWith(f, StringComparison.Ordinal):
                            Console.WriteLine(ulid.ToString());
                            break;

                        case string f when "guid".StartsWith(f, StringComparison.Ordinal):
                            Console.WriteLine(ulid.ToGuid());
                            break;

                        case string f when "detailed".StartsWith(f, StringComparison.Ordinal):
                            DateTimeOffset timestamp = ulid.Timestamp;
                            byte[] randomBytes = ulid.RandomBytes.ToArray();
                            string ulidString = ulid.ToString();

                            // 10 Crockford digits carry exactly the 48 timestamp bits (3 + 9 x 5); the other 16 carry the 80 random bits
                            Console.WriteLine($"ULID: {ulidString}");
                            Console.WriteLine($"GUID: {ulid.ToGuid()}");
                            Console.WriteLine($"  Timestamp:    {ulidString[..10]}       {timestamp:o} ({timestamp.ToUnixTimeMilliseconds()})");
                            Console.WriteLine($"  Random Bytes: {ulidString[10..]} [ 0x{string.Join(", 0x", randomBytes.Select(b => b.ToString("X2")))} ]");
                            Console.WriteLine();
                            break;
                    }
                }
            });

        return rootCommand;
    }
}
