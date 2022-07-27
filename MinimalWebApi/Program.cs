var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello World!");
app.MapPost("/upload", async (HttpRequest request) =>
    {
        //Do something with the file
        var files = request.Form.Files;
        var buffer = new Memory<byte>(new byte[1024 * 16]);
        var res = 0;
        foreach (var file in files)
        {
            await using var stream = file.OpenReadStream();
            var c = await stream.ReadAsync(buffer);
            res += c;
            Console.WriteLine(c);
        }
        Console.WriteLine(res);
        return Results.Ok();
    });

app.Run();