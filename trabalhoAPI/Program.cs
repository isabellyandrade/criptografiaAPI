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

string GerarChaveAleatoria(int tamanho)
{
    Random num = new Random();
    string chave = "";

    for (int i = 0; i < tamanho; i++)
    {
        int numero = num.Next(33, 127);
        char caractere = (char)numero;
        chave += caractere;
    }

    return chave;
}

string Cifrar(string mensagem, string chave)
{
    if (mensagem.Length != chave.Length)
        throw new ArgumentException("A chave deve ter o mesmo tamanho que a mensagem.");

    var bytes = Encoding.UTF8.GetBytes(mensagem)
        .Zip(Encoding.UTF8.GetBytes(chave), (m, c) => (byte)(m ^ c))
        .Select(b => Convert.ToString(b, 2).PadLeft(8, '0'));

    return string.Concat(bytes);
}

string Decifrar(string binario, string chave)
{
    if (binario.Length % 8 != 0)
        throw new ArgumentException("O texto cifrado deve ter múltiplos de 8 bits.");

    int numBytes = binario.Length / 8;
    byte[] bytes = new byte[numBytes];

    for (int i = 0; i < numBytes; i++)
    {
        string byteString = binario.Substring(i * 8, 8);
        bytes[i] = Convert.ToByte(byteString, 2);
    }

    var chaveBytes = Encoding.UTF8.GetBytes(chave);
    if (chaveBytes.Length != bytes.Length)
        throw new ArgumentException("A chave deve ter o mesmo tamanho que a mensagem cifrada.");

    var decifrado = bytes.Zip(chaveBytes, (c, k) => (byte)(c ^ k));
    return Encoding.UTF8.GetString(decifrado.ToArray());
}

//Endpoints
app.MapPost("/cifrar", ([FromBody] MensagemRequest req) =>
{
    var chave = GerarChaveAleatoria(req.Mensagem.Length);
    var cifrada = Cifrar(req.Mensagem, chave);
    return Results.Ok(new { MensagemCifrada = cifrada, Chave = chave });
})
.WithOpenApi();

app.MapPost("/decifrar", ([FromBody] DecifrarRequest req) =>
{
    var mensagem = Decifrar(req.MensagemCifrada, req.Chave);
    return Results.Ok(new { MensagemOriginal = mensagem });
})
.WithOpenApi();

app.Run();

//DTOs
public class MensagemRequest
{
    public string Mensagem { get; set; } 
    
    public MensagemRequest(string mensagem)
    {
        Mensagem = mensagem;
    }
    public MensagemRequest() { }
}

public class DecifrarRequest
{
    public string MensagemCifrada { get; set; }
    public string Chave { get; set; }

    public DecifrarRequest(string mensagemCifrada, string chave)
    {
        MensagemCifrada = mensagemCifrada;
        Chave = chave;
    }
    public DecifrarRequest() { }
}

