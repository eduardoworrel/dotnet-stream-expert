using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors();
 
var app = builder.Build();

 app.UseCors(builder => builder
 .AllowAnyOrigin()
 .AllowAnyMethod()
 .AllowAnyHeader()
);

app.MapGet("/", async (HttpContext context
) => {

    // Gestão eficaz das operações de I/O & contrapressão (backpressure)
    var HTTP_WRITER = PipeWriter.Create(context.Response.Body);
    using var fileReader = new StreamReader("../private/job_descriptions.csv");

    // Buffer intermediário com operações de I/O otimizadas.
    var transformPipe = new Pipe();

    // Execução simultânea do processamento do CSV e envio via HTTP 
    await Task.WhenAll(
        WriteCSVOnDemand(fileReader, transformPipe.Writer),
        PipeThrough(
            transformPipe.Reader,
            HTTP_WRITER,
            Transform.StreamCsvToNDJson()
        ));
});

// Método assíncrono para leitura e escrita de um arquivo CSV sob demanda,
// otimizado para streams.
async Task WriteCSVOnDemand(StreamReader fileReader, PipeWriter writer)
{

    const int BatchSize = 8192 * 2;  // 16KB
    char[] buffer = new char[BatchSize];

    // StringBuilder para acumular os caracteres restantes que
    // não terminam com uma nova linha após cada leitura do buffer.
    StringBuilder leftover = new StringBuilder();

    int charsRead;

    while ((charsRead = await fileReader.ReadAsync(buffer, 0, BatchSize)) > 0)
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


// Canaliza a leitura e a escrita de dados entre Pipes, aplicando uma transformação
// assíncrona. Permite processamento em tempo real com controle eficaz do fluxo.
async Task PipeThrough(
    PipeReader reader,
    PipeWriter writer, 
    Func<ReadOnlyMemory<byte>, Task<ReadOnlyMemory<byte>>> processBuffer
    )
{
    ReadResult readResult;
    do
    {
        readResult = await reader.ReadAsync();
        var buffer = readResult.Buffer;

        foreach (var segment in buffer)
        {
            var processedSegment = await processBuffer(segment);
            await writer.WriteAsync(processedSegment);
        }

        reader.AdvanceTo(buffer.End);
    } while (!readResult.IsCompleted);

    writer.Complete();
}

app.Run();
