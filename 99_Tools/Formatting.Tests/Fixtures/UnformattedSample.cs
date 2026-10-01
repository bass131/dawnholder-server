// Formatting.Tests fixture: deliberately unformatted input for the formatter/proof contract.
// It is inspected data excluded from every Compile set; do not reformat or move it.
namespace Fixture.Unformatted;

public   static class UnformattedSample
{
      public static string Describe( int value )
    {
  var point = new Point { X = value, Y = 2 };
        var text = $"value={ value }  kept";
            var verbatim = @"two  spaces";
        // comment  with  two spaces
        return text+verbatim + point.X;
    }

    private sealed class Point { public int X { get; set; } public int Y { get; set; } }
}
