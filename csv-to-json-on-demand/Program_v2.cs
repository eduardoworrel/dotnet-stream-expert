// using System;
// using System.Buffers;
// using System.Collections.Generic;
// using System.IO;
// using System.IO.Pipelines;
// using System.Text;
// using System.Text.Json;
// using System.Threading.Tasks;
// using Microsoft.AspNetCore.Builder;
// using Microsoft.AspNetCore.Http;

// var builder = WebApplication.CreateBuilder(args);

// builder.Services.AddCors();
 
// var app = builder.Build();

//  app.UseCors(builder => builder
//  .AllowAnyOrigin()
//  .AllowAnyMethod()
//  .AllowAnyHeader()
// );

// app.MapGet("/", async (HttpContext context) => {
//     context.Response.ContentType = "application/json";

//     var readPipe = new Pipe();
//     var writePipe = new Pipe();

//     Task readingTask = FillReadPipeAsync(readPipe.Writer);
//     Task transformingTask = TransformToJSON(readPipe.Reader, writePipe.Writer);
//     Task writingTask = WriteToResponseAsync(writePipe.Reader, context.Response.Body);

//     await Task.WhenAll(readingTask, transformingTask, writingTask);
// });

// async Task FillReadPipeAsync(PipeWriter writer)
// {
//     const int BatchSize = 8192;  // 8KB
//     char[] buffer = new char[BatchSize];
//     StringBuilder leftover = new StringBuilder();
    
//     using var fileReader = new StreamReader("../private/job_descriptions.csv");
//     int charsRead;

//     while ((charsRead = await fileReader.ReadAsync(buffer, 0, BatchSize)) > 0)
//     {
//         leftover.Append(buffer, 0, charsRead);
//         int lastNewLine = leftover.ToString().LastIndexOf('\n');

//         if (lastNewLine >= 0)
//         {
//             string toWrite = leftover.ToString(0, lastNewLine);
//             byte[] byteBuffer = Encoding.UTF8.GetBytes(toWrite);
//             var memory = writer.GetMemory(byteBuffer.Length);
//             byteBuffer.CopyTo(memory.Span);
//             writer.Advance(byteBuffer.Length);
//             await writer.FlushAsync();

//             leftover.Remove(0, lastNewLine + 1);
//         }
//     }

//     if (leftover.Length > 0)
//     {
//         byte[] byteBuffer = Encoding.UTF8.GetBytes(leftover.ToString());
//         var memory = writer.GetMemory(byteBuffer.Length);
//         byteBuffer.CopyTo(memory.Span);
//         writer.Advance(byteBuffer.Length);
//         await writer.FlushAsync();
//     }

//     writer.Complete();
// }

// async Task TransformToJSON(PipeReader reader, PipeWriter writer)
// {
//     string?[] headers = null;
//     var accumulated = new StringBuilder();

//     await ReadUntilCompletedAsync(reader, async (ReadOnlySequence<byte> buffer) =>
//     {
//         var position = buffer.Start;
//         while (buffer.TryGet(ref position, out var memory))
//         {
//             var line = Encoding.UTF8.GetString(memory.Span);

//             if (headers == null)
//             {
//                 headers = line.Split(',');
//             }
//             else
//             {
//                 var values = ParseCsvLine(line).ToArray();
//                 var obj = new Dictionary<string, string>();
//                 for (int j = 0; j < headers.Length; j++)
//                 {
//                     obj[headers[j]] = values.Length > j ? values[j] : null;
//                 }

//                 var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(obj);
//                 await writer.WriteAsync(jsonBytes);

//                 var newline = Encoding.UTF8.GetBytes("\n");
//                 await writer.WriteAsync(newline);

//             }
//         }
//         await writer.FlushAsync();
//     });
//     writer.Complete();
// }

// async Task WriteToResponseAsync(PipeReader reader, Stream outputStream)
// {

//     await ReadUntilCompletedAsync(reader, async (buffer) =>
//     {
//         foreach (var segment in buffer)
//         {
//             await outputStream.WriteAsync(segment);
//         }
//     });

//     reader.Complete();
// }

// async Task ReadUntilCompletedAsync(PipeReader reader, Func<ReadOnlySequence<byte>, Task> processBuffer)
// {
//     ReadResult readResult;
//     do
//     {
//         readResult = await reader.ReadAsync();
//         await processBuffer(readResult.Buffer);
//         reader.AdvanceTo(readResult.Buffer.End);
//     } while (!readResult.IsCompleted);
// }

// static IEnumerable<string> ParseCsvLine(string line)
// {
//     var field = new StringBuilder();
//     var fields = new List<string>();
//     bool inQuotes = false;

//     for (int i = 0; i < line.Length; i++)
//     {
//         char c = line[i];

//         if (c == '"')
//         {
//             if (inQuotes && i < line.Length - 1 && line[i + 1] == '"')
//             {
//                 // Tratar aspas duplas escapadas ("") como uma aspa dupla literal
//                 field.Append('"');
//                 i++; // Pular a próxima aspa dupla
//             }
//             else
//             {
//                 inQuotes = !inQuotes;
//             }
//         }
//         else if (c == ',' && !inQuotes)
//         {
//             fields.Add(field.ToString());
//             field.Clear();
//         }
//         else
//         {
//             field.Append(c);
//         }
//     }

//     fields.Add(field.ToString());

//     return fields;
// }

// app.Run();
