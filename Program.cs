using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using Microsoft.AspNetCore.Http.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<JsonOptions>(opts => opts.SerializerOptions.PropertyNamingPolicy = null);
var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPost("/api/match", async (HttpRequest request) =>
{
    if (!request.HasFormContentType) return Results.BadRequest("Expected form file upload");
    var form = await request.ReadFormAsync();
    var file = form.Files.GetFile("file");
    if (file == null) return Results.BadRequest("No file uploaded");

    using var ms = new MemoryStream();
    await file.CopyToAsync(ms);
    ms.Position = 0;

    using var image = Image.Load<Rgba32>(ms);
    // Sample pixels to limit work for large images
    int w = image.Width, h = image.Height;
    long r=0,g=0,b=0,count=0;
    int step = Math.Max(1, (int)Math.Sqrt((w*h)/20000.0));

    for (int y=0;y<h;y+=step)
    for (int x=0;x<w;x+=step)
    {
        var p = image[x,y];
        r += p.R;
        g += p.G;
        b += p.B;
        count++;
    }

    var avgR = (int)(r / count);
    var avgG = (int)(g / count);
    var avgB = (int)(b / count);
    string hex = $"#{avgR:X2}{avgG:X2}{avgB:X2}";

    var name = NearestNamedColor(avgR, avgG, avgB);

    return Results.Json(new { R = avgR, G = avgG, B = avgB, Hex = hex, Name = name });
});

app.Run();

string NearestNamedColor(int r, int g, int b)
{
    // Small palette of named colors
    var palette = new (string Name, int R, int G, int B)[] {
        ("Black",0,0,0), ("White",255,255,255), ("Red",255,0,0), ("Lime",0,255,0), ("Blue",0,0,255),
        ("Yellow",255,255,0), ("Cyan",0,255,255), ("Magenta",255,0,255), ("Gray",128,128,128), ("Orange",255,165,0)
    };
    double best = double.MaxValue; string bestName = "Unknown";
    foreach (var c in palette)
    {
        double d = Math.Pow(r - c.R,2) + Math.Pow(g - c.G,2) + Math.Pow(b - c.B,2);
        if (d < best) { best = d; bestName = c.Name; }
    }
    return bestName;
}
