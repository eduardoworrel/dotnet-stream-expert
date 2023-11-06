# 🔥📀 .NET Streams [CSV para JSON para o navegador]


## Como testar

- Qualquer CSV deve ser adicionado a pasta private

- Foi utilizado o seguinte (1,74 GB) [https://www.kaggle.com/datasets/ravindrasinghrana/job-description-dataset/](https://www.kaggle.com/datasets/ravindrasinghrana/job-description-dataset/) 

```csharp
//Observe no program.cs
using var fileReader = new StreamReader("../private/job_descriptions.csv");
```

- abra o arquivo `json.html` no navegador e o processo iniciará automáticamente

--- 

Inspirado em **[Read 30GB+ Data in the Browser Without Blocking the Screen || Webstreams 101 || Erick Wendel](https://www.youtube.com/watch?v=EexM7EL9Blk)**
