using System.Text;
using System.Text.RegularExpressions;
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

const string Alfabeto = "abcdefghijklmnopqrstuvwxyz";
int AlfabetoLen = Alfabeto.Length;

static int Shift(int shift, int modulo)
{
    var s = shift % modulo;
    if (s < 0) s += modulo;
    return s;
}

string Cifrar(string texto, int deslocamento)
{
    var sb = new StringBuilder(texto.Length);
    deslocamento = Shift(deslocamento, AlfabetoLen);

    foreach (char ch in texto)
    {
        if (ch == ' ')
        {
            sb.Append(' ');
            continue;
        }

        bool isUpper = char.IsUpper(ch);
        char lower = char.ToLowerInvariant(ch);

        int idx = Alfabeto.IndexOf(lower);
        if (idx == -1)
        {
            sb.Append(ch);
            continue;
        }

        int novaPos = (idx + deslocamento) % AlfabetoLen;
        char cifrado = Alfabeto[novaPos];
        if (isUpper) cifrado = char.ToUpperInvariant(cifrado);
        sb.Append(cifrado);
    }

    return sb.ToString();
}

string Decifrar(string textoCifrado, int deslocamento)
{
    return Cifrar(textoCifrado, -deslocamento);
}

bool TextoValido(string texto)
{
    return Regex.IsMatch(texto, @"^[a-zA-Z\s]+$");
}

bool DeslocamentoValido(int deslocamento)
{
    return deslocamento > 0 && deslocamento <= 25;
}

// Endpoints
app.MapPost("/cifrar", ([FromBody] CifrarRequest req) =>
{
    if (req == null || req.TextoClaro == null)
        return Results.BadRequest(new { Erro = "Envie textoClaro e deslocamento." });

    if (!TextoValido(req.TextoClaro))
        return Results.BadRequest(new { Erro = "O texto não pode conter caracteres especiais." });

    if (!DeslocamentoValido(req.Deslocamento))
        return Results.BadRequest(new { Erro = "O deslocamento deve ser maior que 0 e menor ou igual a 25." });

    var cifrado = Cifrar(req.TextoClaro, req.Deslocamento);
    return Results.Ok(new { textoCifrado = cifrado });
})
.WithOpenApi();

app.MapPost("/decifrar", ([FromBody] DecifrarRequest req) =>
{
    if (req == null || req.TextoCifrado == null)
        return Results.BadRequest(new { Erro = "Envie textoCifrado e deslocamento." });

    if (!TextoValido(req.TextoCifrado))
        return Results.BadRequest(new { Erro = "O texto não pode conter caracteres especiais." });

    if (!DeslocamentoValido(req.Deslocamento))
        return Results.BadRequest(new { Erro = "O deslocamento deve ser maior que 0 e menor ou igual a 25." });

    var original = Decifrar(req.TextoCifrado, req.Deslocamento);
    return Results.Ok(new { textoClaro = original });
})
.WithOpenApi();

app.Run();

// Classes
public class CifrarRequest
{
    public string? TextoClaro { get; set; }
    public int Deslocamento { get; set; }

    public CifrarRequest() { }
    public CifrarRequest(string textoClaro, int deslocamento)
    {
        TextoClaro = textoClaro;
        Deslocamento = deslocamento;
    }
}

public class DecifrarRequest
{
    public string? TextoCifrado { get; set; }
    public int Deslocamento { get; set; }

    public DecifrarRequest() { }
    public DecifrarRequest(string textoCifrado, int deslocamento)
    {
        TextoCifrado = textoCifrado;
        Deslocamento = deslocamento;
    }
}
