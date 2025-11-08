using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpClient();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Alfabeto permitido: apenas letras minúsculas
const string Alfabeto = "abcdefghijklmnopqrstuvwxyz";
int AlfabetoLen = Alfabeto.Length;

// Funções utilitárias
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

        // Apenas letras minúsculas
        int idx = Alfabeto.IndexOf(ch);
        if (idx == -1)
        {
            sb.Append(ch);
            continue;
        }

        int novaPos = (idx + deslocamento) % AlfabetoLen;
        sb.Append(Alfabeto[novaPos]);
    }

    return sb.ToString();
}

string Decifrar(string textoCifrado, int deslocamento)
{
    return Cifrar(textoCifrado, -deslocamento);
}

bool TextoValido(string texto)
{
    // Apenas letras minúsculas e espaços
    return Regex.IsMatch(texto, @"^[a-z\s]+$");
}

bool DeslocamentoValido(int deslocamento)
{
    return deslocamento > 0 && deslocamento <= 25;
}

// -------------------------
// ENDPOINTS
// -------------------------

// Cifrar
app.MapPost("/cifrar", ([FromBody] CifrarRequest req) =>
{
    if (req == null || string.IsNullOrWhiteSpace(req.TextoClaro))
        return Results.BadRequest(new { Erro = "Envie textoClaro e deslocamento." });

    if (!TextoValido(req.TextoClaro))
        return Results.BadRequest(new { Erro = "O texto deve conter apenas letras minúsculas de a-z e espaços." });

    if (!DeslocamentoValido(req.Deslocamento))
        return Results.BadRequest(new { Erro = "O deslocamento deve ser maior que 0 e menor ou igual a 25." });

    var cifrado = Cifrar(req.TextoClaro, req.Deslocamento);
    return Results.Ok(new { textoCifrado = cifrado });
})
.WithOpenApi();

// Decifrar
app.MapPost("/decifrar", ([FromBody] DecifrarRequest req) =>
{
    if (req == null || string.IsNullOrWhiteSpace(req.TextoCifrado))
        return Results.BadRequest(new { Erro = "Envie textoCifrado e deslocamento." });

    if (!TextoValido(req.TextoCifrado))
        return Results.BadRequest(new { Erro = "O texto deve conter apenas letras minúsculas de a-z e espaços." });

    if (!DeslocamentoValido(req.Deslocamento))
        return Results.BadRequest(new { Erro = "O deslocamento deve ser maior que 0 e menor ou igual a 25." });

    var original = Decifrar(req.TextoCifrado, req.Deslocamento);
    return Results.Ok(new { textoClaro = original });
})
.WithOpenApi();

app.MapPost("/decifrarForcaBruta", async ([FromServices] IHttpClientFactory httpClientFactory, [FromBody] DecifrarForcaBrutaRequest req) =>
{
    if (req == null || string.IsNullOrWhiteSpace(req.TextoCifrado))
        return Results.BadRequest(new { Erro = "Envie textoCifrado." });

    if (!Regex.IsMatch(req.TextoCifrado, @"^[a-z\s]+$"))
        return Results.BadRequest(new { Erro = "O texto deve conter apenas letras minúsculas de a-z e espaços." });

    var client = httpClientFactory.CreateClient();
    var texto = req.TextoCifrado.Trim();

    string RemoverAcentos(string input)
    {
        var normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();
        foreach (var c in normalized)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                != System.Globalization.UnicodeCategory.NonSpacingMark)
                sb.Append(c);
        }
        return sb.ToString().Normalize(NormalizationForm.FormC);
    }

    for (int deslocamento = 1; deslocamento <= 25; deslocamento++)
    {
        var tentativa = Decifrar(texto, deslocamento);
        var palavras = tentativa.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        int validas = 0;

        foreach (var palavra in palavras)
        {
            var url = $"https://www.dicio.com.br/{palavra}/";
            try
            {
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var html = await response.Content.ReadAsStringAsync();
                    var htmlSemAcento = RemoverAcentos(html);
                    var palavraSemAcento = RemoverAcentos(palavra);

                    if (Regex.IsMatch(htmlSemAcento, $"<h1[^>]*>[^<]*\\b{Regex.Escape(palavraSemAcento)}\\b", RegexOptions.IgnoreCase))
                    {
                        validas++;
                    }
                }
            }
            catch
            {
                // ignora falhas de rede
            }
        }

        // 🔹 Critério: mais da metade das palavras precisam existir
        // 🔹 Critério: mais da metade das palavras precisam existir
        if (validas > 0 && validas >= Math.Max(2, palavras.Length / 2))
        {
            Console.WriteLine($"✅ Frase válida encontrada: {tentativa} (deslocamento {deslocamento})");
            return Results.Ok(new
            {
                textoClaro = tentativa,
                deslocamentoEncontrado = deslocamento
            });
        }
        else
        {
            Console.WriteLine($"❌ Tentativa descartada: {tentativa} (válidas: {validas}/{palavras.Length})");
        }

    }

    return Results.NotFound(new { Erro = "Nenhuma correspondência válida encontrada." });
})
.WithOpenApi();

app.Run();

// -------------------------
// CLASSES DE REQUISIÇÃO
// -------------------------

public class CifrarRequest
{
    public string? TextoClaro { get; set; }
    public int Deslocamento { get; set; }
}

public class DecifrarRequest
{
    public string? TextoCifrado { get; set; }
    public int Deslocamento { get; set; }
}

public class DecifrarForcaBrutaRequest
{
    public string? TextoCifrado { get; set; }
}
