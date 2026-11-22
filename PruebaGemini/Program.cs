using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Sockets;
using DotNetEnv;


// CARGAR CONFIGURACION
Env.Load();

// API KEY GEMINI
string? apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");

if (string.IsNullOrWhiteSpace(apiKey))
{
    Console.WriteLine("No se encontro la API KEY");
    return;
}

// MODELO GEMINI
string modelo = "gemini-3.1-flash-lite";

string url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelo}:generateContent";

// CONFIGURACION ESP32
string? ipESP32 = Environment.GetEnvironmentVariable("ESP32_IP");
string? puertoTexto = Environment.GetEnvironmentVariable("UDP_PORT");

if (string.IsNullOrWhiteSpace(ipESP32) ||
    !int.TryParse(puertoTexto, out int puertoESP32))
{
    Console.WriteLine("Error en la configuracion del ESP32.");
    return;
}

using UdpClient udp = new();

using HttpClient http = new();

http.Timeout = TimeSpan.FromSeconds(60);

http.DefaultRequestHeaders.Add("x-goog-api-key", apiKey);

Console.WriteLine("=================================");
Console.WriteLine("       CONTROLADOR IA");
Console.WriteLine("=================================");

Console.WriteLine("ESP32: " + ipESP32);
Console.WriteLine("Puerto UDP: " + puertoESP32);
Console.WriteLine("Escribe 'salir' para terminar.\n");
// =====================================
// BUCLE PRINCIPAL
// =====================================

while (true)
{
    Console.Write("\nTu: ");

    string pregunta = Console.ReadLine() ?? "";

    if (pregunta.Trim().Equals("salir", StringComparison.OrdinalIgnoreCase))
    {
        Console.WriteLine("Controlador finalizado.");
        break;
    }

    if (string.IsNullOrWhiteSpace(pregunta))
        continue;

    try
    {
        // INSTRUCCIONES GEMINI

        string instrucciones = """
        Eres un controlador inteligente de servomotores.

        Convierte las instrucciones del usuario en acciones JSON.

        Responde exclusivamente con JSON válido.

        Reglas:

        1. Movimiento:
           tipo = "mover"
           angulo = grados entre 5 y 175
           velocidad = valor entre 1 y 600

        2. Espera:
           tipo = "esperar"
           tiempo_ms = milisegundos

        3. Si no se especifica velocidad, utiliza 60.

        4. No inventes acciones.

        Formato:

        {
          "acciones": [
            {
              "tipo": "mover",
              "angulo": 90,
              "velocidad": 60
            }
          ]
        }
        """;

        var solicitud = new
        {
            systemInstruction = new
            {
                parts = new[]
                {
                    new { text = instrucciones }
                }
            },

            contents = new[]
            {
                new
                {
                    role = "user",
                    parts = new[]
                    {
                        new { text = pregunta }
                    }
                }
            }
        };

        string json = JsonSerializer.Serialize(solicitud);

        var contenido = new StringContent(
            json,
            Encoding.UTF8,
            "application/json"
        );

        // CONSULTAR GEMINI

        Console.WriteLine("\nConsultando Gemini...");

        HttpResponseMessage respuesta =
            await http.PostAsync(url, contenido);

        string resultado =
            await respuesta.Content.ReadAsStringAsync();

        if (!respuesta.IsSuccessStatusCode)
        {
            Console.WriteLine("Error API:");
            Console.WriteLine(resultado);
            continue;
        }

        using JsonDocument documento =
            JsonDocument.Parse(resultado);

        string texto = documento.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString() ?? "";

        Console.WriteLine("\nACCIONES GENERADAS:");
        Console.WriteLine(texto);

        // INTERPRETAR JSON

        Programa? programa = JsonSerializer.Deserialize<Programa>(
            texto
        );

        if (programa == null || programa.Acciones.Count == 0)
        {
            Console.WriteLine("No se encontraron acciones.");
            continue;
        }

        // EJECUTAR ACCIONES

        Console.WriteLine("\nEJECUTANDO SECUENCIA:");

        bool errorAccion = false;

        foreach (Accion accion in programa.Acciones)
        {
            switch (accion.Tipo.ToLower())
            {
                case "mover":

                    if (accion.Angulo is < 5 or > 175 ||
                        accion.Velocidad is < 1 or > 600 ||
                        accion.Angulo == null ||
                        accion.Velocidad == null)
                    {
                        Console.WriteLine("Movimiento invalido.");
                        errorAccion = true;
                        break;
                    }

                    // DESCARTAR RESPUESTAS ANTIGUAS

                    while (udp.Available > 0)
                    {
                        await udp.ReceiveAsync();
                    }

                    // ENVIAR MOVIMIENTO

                    string comando =
                        $"MOVE:{accion.Angulo}:{accion.Velocidad}";

                    byte[] datos = Encoding.UTF8.GetBytes(comando);

                    await udp.SendAsync(
                        datos,
                        datos.Length,
                        ipESP32,
                        puertoESP32
                    );

                    Console.WriteLine(
                        $"[MOVER] Objetivo: {accion.Angulo} grados"
                    );

                    // ESPERAR FINALIZACION DEL MOVIMIENTO

                    bool movimientoTerminado = false;

                    int intentos = 0;

                    while (!movimientoTerminado && intentos < 150)
                    {
                        intentos++;

                        await Task.Delay(100);

                        // Limpiar respuestas anteriores

                        while (udp.Available > 0)
                        {
                            await udp.ReceiveAsync();
                        }

                        // Consultar estado

                        string consulta = "GET_STATUS";

                        byte[] consultaBytes =
                            Encoding.UTF8.GetBytes(consulta);

                        await udp.SendAsync(
                            consultaBytes,
                            consultaBytes.Length,
                            ipESP32,
                            puertoESP32
                        );

                        using var cancelacion =
                            new CancellationTokenSource(1000);

                        UdpReceiveResult recibido;

                        try
                        {
                            recibido = await udp.ReceiveAsync(
                                cancelacion.Token
                            );
                        }
                        catch (OperationCanceledException)
                        {
                            continue;
                        }

                        string estado =
                            Encoding.UTF8.GetString(recibido.Buffer);

                        Console.WriteLine("[ESP32] " + estado);

                        // COMPROBAR ESTADO

                        if (estado.Contains("MOVING:0") &&
                            estado.Contains(
                                $"POS:{accion.Angulo};TARGET:{accion.Angulo}"
                            ))
                        {
                            movimientoTerminado = true;
                        }
                    }

                    if (!movimientoTerminado)
                    {
                        Console.WriteLine(
                            "Error: tiempo agotado esperando el movimiento."
                        );

                        errorAccion = true;
                    }
                    else
                    {
                        Console.WriteLine(
                            "[OK] Movimiento completado."
                        );
                    }

                    break;

                case "esperar":

                    if (accion.TiempoMs is null ||
                        accion.TiempoMs < 0 ||
                        accion.TiempoMs > 60000)
                    {
                        Console.WriteLine("Tiempo invalido.");
                        errorAccion = true;
                        break;
                    }

                    Console.WriteLine(
                        $"[ESPERAR] {accion.TiempoMs} ms"
                    );

                    await Task.Delay(accion.TiempoMs.Value);

                    break;

                default:

                    Console.WriteLine(
                        $"Accion desconocida: {accion.Tipo}"
                    );

                    errorAccion = true;

                    break;
            }

            if (errorAccion)
                break;
        }

        Console.WriteLine("\nSecuencia finalizada.");
    }
    catch (Exception ex)
    {
        Console.WriteLine("Error: " + ex.Message);
    }
}

// =====================================
// CLASES
// =====================================

public class Accion
{
    [JsonPropertyName("tipo")]
    public string Tipo { get; set; } = "";

    [JsonPropertyName("angulo")]
    public int? Angulo { get; set; }

    [JsonPropertyName("velocidad")]
    public int? Velocidad { get; set; }

    [JsonPropertyName("tiempo_ms")]
    public int? TiempoMs { get; set; }
}

public class Programa
{
    [JsonPropertyName("acciones")]
    public List<Accion> Acciones { get; set; } = new();
}
