
using System.Net.Http.Headers;
using System.Text;
using System.IO.Pipelines;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors();
 
var app = builder.Build();

app.UseCors(builder => builder
 .AllowAnyOrigin()
 .AllowAnyMethod()
 .AllowAnyHeader()
);

const string apiKey = "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";



app.MapGet("/", async (HttpContext context) =>{

    using (var client = new HttpClient())
    {
        var requestMessage = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
        {
            Content = new StringContent(
                "{\"model\": \"gpt-3.5-turbo\", \"messages\": [{\"role\": \"system\", \"content\": \"Você é um esplecialista em Web Streams.\"}, {\"role\": \"user\", \"content\": \"Compare o uso de WebStreams ao de Websockets e SSE\"}],\"stream\": true}",
                Encoding.UTF8, "application/json")
        };

        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var response = await client.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead);

        var HTTP_WRITER = PipeWriter.Create(context.Response.Body);
        if (response.IsSuccessStatusCode)
        {
            context.Response.StatusCode = StatusCodes.Status200OK;

            await using var responseStream = await response.Content.ReadAsStreamAsync();
            using var streamReader = new StreamReader(responseStream);    
            await WriteLineByLineOnDemand(streamReader,HTTP_WRITER);
        }
    }
});

async Task WriteLineByLineOnDemand(StreamReader fileReader, PipeWriter writer)
{

    const int BatchSize = 8192 * 2;  // 16KB
    char[] buffer = new char[BatchSize];

    // StringBuilder para acumular os caracteres restantes que
    // não terminam com uma nova linha após cada leitura do buffer.
    StringBuilder leftover = new StringBuilder();

    int charsRead;

    while ((charsRead = await fileReader.ReadAsync(buffer)) > 0)
    { 
        // Acumula os caracteres lidos no StringBuilder.
        leftover.Append(buffer, 0, charsRead);
        int lastNewLine = leftover.ToString().LastIndexOf('\n');

        if (lastNewLine >= 0)
        {
            // Extrai a linha completa do buffer.
            string toWrite = leftover.ToString(0, lastNewLine);
            byte[] byteBuffer = Encoding.UTF8.GetBytes(toWrite);

            // Solicita memória do PipeWriter para escrever os bytes.
            var memory = writer.GetMemory(byteBuffer.Length);
            // Copia os bytes para o espaço de memória do PipeWriter.
            byteBuffer.CopyTo(memory.Span);
            // Indica ao PipeWriter quantos bytes foram escritos.
            writer.Advance(byteBuffer.Length);
            // Envia os bytes escritos para o consumidor do PipeWriter.
            await writer.FlushAsync();

            leftover.Remove(0, lastNewLine + 1);
        }
    }
    if (leftover.Length > 0)
    {
        // Converte os caracteres restantes para bytes e os escreve no PipeWriter.
        byte[] byteBuffer = Encoding.UTF8.GetBytes(leftover.ToString());
        var memory = writer.GetMemory(byteBuffer.Length);
        byteBuffer.CopyTo(memory.Span);
        writer.Advance(byteBuffer.Length);
        await writer.FlushAsync();
    }

    writer.Complete();
}

app.Run();

record ChatCompletionChunk(string Id, string Object, long Created, string Model, List<Choice> Choices);

record Choice(int Index, Delta Delta);

record Delta(string Role, string Content);