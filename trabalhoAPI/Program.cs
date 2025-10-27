using System.Text;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

/// --- Funções utilitárias ---

// Função para cifrar (gera saída em binário)
string VernamCipherToBinary(string message, string key)
{
    if (key.Length != message.Length)
        throw new ArgumentException("A chave deve ter o mesmo tamanho que a mensagem.");

    byte[] msgBytes = Encoding.UTF8.GetBytes(message);
    byte[] keyBytes = Encoding.UTF8.GetBytes(key);
    byte[] result = new byte[msgBytes.Length];

    for (int i = 0; i < msgBytes.Length; i++)
        result[i] = (byte)(msgBytes[i] ^ keyBytes[i]);

    // Converte cada byte em uma sequência binária de 8 bits
    return string.Join("", result.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));
}

// Função para decifrar (recebe binário)
string VernamDecipherFromBinary(string binaryCipher, string key)
{
    if (binaryCipher.Length % 8 != 0)
        throw new ArgumentException("O texto cifrado deve ter múltiplos de 8 bits.");

    int numBytes = binaryCipher.Length / 8;
    byte[] cipherBytes = new byte[numBytes];

    for (int i = 0; i < numBytes; i++)
    {
        string byteString = binaryCipher.Substring(i * 8, 8);
        cipherBytes[i] = Convert.ToByte(byteString, 2);
    }

    byte[] keyBytes = Encoding.UTF8.GetBytes(key);
    if (keyBytes.Length != cipherBytes.Length)
        throw new ArgumentException("A chave deve ter o mesmo tamanho que a mensagem cifrada.");

    byte[] result = new byte[cipherBytes.Length];
    for (int i = 0; i < cipherBytes.Length; i++)
        result[i] = (byte)(cipherBytes[i] ^ keyBytes[i]);

    return Encoding.UTF8.GetString(result);
}

// Gera uma chave aleatória do mesmo tamanho da mensagem
string GerarChaveAleatoria(int tamanho)
{
    var random = new Random();
    var bytes = new byte[tamanho];
    random.NextBytes(bytes);
    // Converte bytes em caracteres imprimíveis (33–126 ASCII)
    return Encoding.UTF8.GetString(bytes.Select(b => (byte)((b % 94) + 33)).ToArray());
}

/// --- Endpoints ---

// Cifrar
app.MapPost("/cifrar", ([FromBody] CifrarRequest request) =>
{
    var chave = GerarChaveAleatoria(request.Mensagem.Length);
    var cifrada = VernamCipherToBinary(request.Mensagem, chave);

    return Results.Ok(new CifrarResponse(cifrada, chave));
})
.WithName("CifrarMensagem")
.WithOpenApi();

// Decifrar
app.MapPost("/decifrar", ([FromBody] DecifrarRequest request) =>
{
    var mensagemOriginal = VernamDecipherFromBinary(request.MensagemCifrada, request.Chave);
    return Results.Ok(new DecifrarResponse(mensagemOriginal));
})
.WithName("DecifrarMensagem")
.WithOpenApi();

app.Run();

/// --- DTOs ---
public record CifrarRequest(string Mensagem);
public record CifrarResponse(string MensagemCifrada, string Chave);
public record DecifrarRequest(string MensagemCifrada, string Chave);
public record DecifrarResponse(string MensagemOriginal);
