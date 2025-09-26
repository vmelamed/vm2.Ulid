using System.Text;

using vm2;

using static System.Console;

WriteLine($"New Ulid:");
WriteLine($"--------------------------");

var ulidFactory = new UlidFactory();
var ulid = Ulid.NewUlid();

Display(ulid);

WriteLine($"Ulid from UTF-8 string: \"{ulid}\" (round-trip)");
WriteLine($"-----------------------------------------");
var ulid2 = new Ulid(Encoding.UTF8.GetBytes(ulid.ToString()), true);

Display(ulid2);

WriteLine(Ulid.NewUlid().ToString());
WriteLine(Ulid.NewUlid().ToString());
WriteLine(Ulid.NewUlid().ToString());
WriteLine($"--------------------------");
Task.Delay(1).Wait();   // wait 1ms to ensure different timestamp
WriteLine(Ulid.NewUlid().ToString());
WriteLine(Ulid.NewUlid().ToString());
WriteLine(Ulid.NewUlid().ToString());
WriteLine($"--------------------------");
Task.Delay(1).Wait();
WriteLine(Ulid.NewUlid().ToString());
WriteLine(Ulid.NewUlid().ToString());
WriteLine(Ulid.NewUlid().ToString());
WriteLine();

static void Display(Ulid ulid)
{
    byte[] bytes = ulid.Bytes.ToArray();
    DateTimeOffset timestamp = ulid.Timestamp;
    byte[] randomBytes = ulid.RandomBytes.ToArray();

    WriteLine($"As ULID string:  \"{ulid}\"");
    WriteLine($"  u.Timestamp:   {timestamp:o} ({timestamp.ToUnixTimeMilliseconds()})");
    WriteLine($"  u.RandomBytes: [ 0x{string.Join(", 0x", randomBytes.Select(b => b.ToString("X2")))} ]");
    WriteLine($"As byte array:   [ 0x{string.Join(", 0x", bytes.Select(b => b.ToString("X2")))} ]");
    WriteLine($"As Guid:         {ulid.ToGuid()}");
    WriteLine();
}