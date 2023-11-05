using System.IO.Pipelines;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCors();
 
var app = builder.Build();

app.UseCors(builder => builder
 .AllowAnyOrigin()
 .AllowAnyMethod()
 .AllowAnyHeader()
);
const string path = "../private/output.mp4";
app.MapGet("/1", async (HttpContext context)=>{
    var mimeType = "video/mp4";

    var fileContent = await File.ReadAllBytesAsync(path);
    return Results.File(fileContent, mimeType);
});

app.MapGet("/2", async (HttpContext context)=>{
    var mimeType = "video/mp4";

    var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
    return Results.Stream(stream, contentType: mimeType);
});

app.MapGet("/3", async (
        HttpContext context,
        CancellationToken token
        )=>{
    // tunado/envenenado
    using var stream = new FileStream(path, 
        FileMode.Open, FileAccess.Read);
    
    context.Response.ContentType = "video/mp4";
    context.Response.ContentLength = stream.Length;

    var buffer = new byte[1024 * 128]; // 128 KB
    int bytesRead;
    while ((bytesRead = await stream.ReadAsync(buffer, token)) > 0)
    {
        await context.Response.Body.WriteAsync(buffer, 0, bytesRead, token);
    }
});


app.MapGet("/4", async (HttpContext context, CancellationToken token) =>
{
    var imagePath = @"../private/fire.png";

    var process = new Process
    {
        StartInfo = new ProcessStartInfo
        {
            FileName = "ffmpeg",
            Arguments =
                $" -i {path} -i {imagePath}"
                + @" -filter_complex ""[0:v]lutrgb='r=negval:g=negval:b=negval'[inv];[1:v]scale=iw*0.3:-1[wm];[inv][wm]overlay=W-w-20:10""" 
                + " -f mp4 -movflags frag_keyframe+empty_moov"
                + " pipe:1",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        }
    };

    process.Start();
    
    var buffer = new byte[1024 * 128]; // 128 KB
    int bytesRead;

    var readableStream = process.StandardOutput.BaseStream;
    
    while ((bytesRead = await readableStream.ReadAsync(buffer,token)) > 0)
    {
        await context.Response.Body.WriteAsync(buffer, 0, bytesRead,token);
    }
    process.Kill();
});

app.Run();
