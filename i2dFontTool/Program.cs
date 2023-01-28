// See https://aka.ms/new-console-template for more information

using System.CommandLine;
using System.Drawing;
using System.Linq;
using System.Text;

var spritesheetOption = new Option<FileInfo>("--file", "Filename of the font spritesheet, needs to have each character surrounded by a rectangle filled with solid color.");
var charsetOption = new Option<FileInfo>("--charset", "Filename of the charset, a txt file containing all the font characters. Line breaks are ignored.");
var lineHeightOption = new Option<int>("--lineHeight", "Height of a single line in the spritesheet, must be uniform for all rows.");
var outputOption = new Option<FileInfo>("--out", "Output file path.");
var generateLowerUpperPairsOption = new Option<bool>("--addLowerUpperPairs", () => false, "Will additionally generate missing definitions for lower/upper character pairs.");

var rootCommand = new RootCommand("Tool for conveniently building a json font description for use with Instant2D.\n" +
    "Requires a spritesheet with all the character cells filled with solid (non-transpared) color and a character list, in order from left to right.\n" +
    "You'll have to adjust some of the resulting values yourself.");

rootCommand.AddOption(spritesheetOption);
rootCommand.AddOption(charsetOption);
rootCommand.AddOption(lineHeightOption);
rootCommand.AddOption(outputOption);
rootCommand.AddOption(generateLowerUpperPairsOption);

rootCommand.SetHandler((spritesheetPath, charsetPath, lineHeight, shouldGenerateLowerUpperPairs, outputPath) => {
    using var charStream = charsetPath.OpenText();
    using var bmpStream = spritesheetPath.OpenRead();

    // setup charset & bitmaps
    using var bitmap = (Bitmap) Image.FromStream(bmpStream);
    var charset = charStream.ReadToEnd()
        .ReplaceLineEndings("");

    var rectangles = new List<Rectangle>();
    var claimedPixels = new HashSet<Point>();

    bool CheckPixel(Point pos) => !claimedPixels.Contains(pos) && bitmap.GetPixel(pos.X, pos.Y).A != 0;

    Console.WriteLine("Analyzing spritesheet... ");

    for (var x = 0; x < bitmap.Width; x++) {
        for (var y = 0; y < bitmap.Height; y++) {
            var topLeft = new Point(x, y);
            if (!CheckPixel(topLeft))
                continue;

            var pos = topLeft;

            // grow bounds horizontally
            while (CheckPixel(pos with { X = pos.X + 1 })) {
                pos.X++;
            }

            // grow bounds vertically
            while (CheckPixel(pos with { Y = pos.Y + 1 })) {
                pos.Y++;
            }

            // create the character rectangle, then claim all the pixels inside it
            var rect = new Rectangle(topLeft, new(pos.X - topLeft.X + 1, pos.Y - topLeft.Y + 1));
            for (var i = rect.X; i < rect.Right; i++) {
                for (var j = rect.Y; j < rect.Bottom; j++) {
                    claimedPixels.Add(new(i, j));
                }
            }

            rectangles.Add(rect);
        }
    }

    // sort rectangles from left to right, top to bottom
    rectangles.Sort((a, b) => {
        var rowComparison = (a.Y / lineHeight).CompareTo(b.Y / lineHeight);
        if (rowComparison != 0) {
            return rowComparison;
        }

        return a.X.CompareTo(b.X);
    });

    var jsonBuilder = new StringBuilder();
    jsonBuilder.AppendLine("""
        {
            "lineSpacing": "SPECIFY_LINE_SPACING",
            "defaultChar": " ",
            "characters": [
                /* CHAR | ID   | X     | Y     | W     | H     | OffX  | OffY  | AdvX   */
        """);

    if (rectangles.Count != charset.Length) {
        Console.WriteLine("Warning: recognized characters don't match the charset by length.");
    }

    static string GenerateLine(Rectangle rect, char ch) => $"""
                    /* {ch} */ [{(int)ch + ",",-8}{rect.X + ",",-8}{rect.Y + ",",-8}{rect.Width + ",",-8}{rect.Height + ",",-8}{"0,",-8}{"0,",-8}{rect.Width + 1,-8}],
            """;

    for (var i = 0; i < rectangles.Count; i++) {
        var rect = rectangles[i];
        var ch = charset[i];

        // append generated line for this character
        jsonBuilder.AppendLine(GenerateLine(rect, ch));

        // optionally add missing pairs
        if (shouldGenerateLowerUpperPairs && char.IsLetterOrDigit(ch)) {
            var pair = char.IsUpper(ch) ? char.ToLower(ch) : char.ToUpper(ch);
            if (!charset.Contains(pair)) {
                jsonBuilder.AppendLine(GenerateLine(rect, pair));
            }
        }
    }

    jsonBuilder.AppendLine("""
            ]
        }
        """);

    File.WriteAllText(outputPath.FullName, jsonBuilder.ToString());

    Console.WriteLine("Done!");

}, spritesheetOption, charsetOption, lineHeightOption, generateLowerUpperPairsOption, outputOption);

return await rootCommand.InvokeAsync(args);
