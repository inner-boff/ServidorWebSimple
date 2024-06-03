using System;
using System.Net;
using System.Net.Sockets;
using System.IO; // Espacio de nombres para clases que permiten la manipulación de archivos y directorios.
using System.Text; // Espacio de nombres para clases que permiten la manipulación de texto.
using System.Threading.Tasks; // Espacio de nombres para clases relacionadas con tareas asincrónicas.
using System.IO.Compression;
using System.Runtime.CompilerServices;
using System.IO.Enumeration; // Espacio de nombres para clases que permiten la compresión de archivos.


class ServidorWebSimple
{
    private static string rootDirectory;
    private static int port;
    private static IPAddress localIPAddress = IPAddress.Parse("127.0.0.1");
    private static TcpListener servidor;
    private static TcpClient clienteTCP;
    //private static HttpClient clienteHttp;
    //private static NetworkStream stream = clienteTCP.GetStream();
    private static string currentDirectory;

    // Método Main asíncrono
    static async Task Main(string[] args)
    {
        currentDirectory = Directory.GetCurrentDirectory();
        rootDirectory = File.ReadAllText(Path.Combine(currentDirectory, "configuracion", "archivos_config.txt")).Trim();
        port = int.Parse(File.ReadAllText(Path.Combine(currentDirectory, "configuracion", "puerto_config.txt")).Trim());   


        // Bloque try-catch para manejar excepciones de conexión al iniciar el servidor
        // Evita que se cierre el programa si hay un error
        try
        {
            await IniciarServidor();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error: {e.Message}");
        }


        while (true)
        {
            // Esperar a que un clienteTCP se conecte
            // El método AcceptTcpClientAsync devuelve un objeto TcpClient que representa al clienteTCP conectado
            // Este loop hace que el servidor pueda aceptar múltiples conexiones
            clienteTCP = await servidor.AcceptTcpClientAsync();
            Console.WriteLine("¡Cliente conectado!");


            // Obtener el stream de la conexión con el clienteTCP
            // OJO: esto permite leer y escribir datos en la conexión
            // Así que puedo usarlo paar enviar respuesta
            NetworkStream stream = clienteTCP.GetStream();
            byte[] buffer = new byte[1024];
            // Esperar a que el clienteTCP envíe datos
            // OJO, AQUI ABAJO ESTÄ SALTANDO UNA EXECPCION NO MANEJADA
            // ystem.Net.Sockets.SocketException (995): Se ha forzado la interrupción de una conexión existente por el host remoto
            // INTENTAR METER EN UN TRY CATCH Y VER QUE PASA
            int bytes = await stream.ReadAsync(buffer, 0, buffer.Length);
            string httpRequest = Encoding.UTF8.GetString(buffer, 0, bytes);
            Console.WriteLine($"Mensaje recibido:\n{httpRequest}--- fin del mensaje recibido ---\n\n");

            // HTTP RESPONSE TEST
            // TRATAR DE ARMAR ESTAS RESPUESTAS DIRECTAMENTE EN MANEJAR SOLICITUDES; PORQUE DEPENDE DEL TIPO DE SOLICITUD
            /*
            string httpResponse = "HTTP/1.1 200 OK\nContent-Type: text/html; charset=UTF-8\n\n<html>Respuesta OK - HTML<h1><h1></html>";
            byte[] response = Encoding.UTF8.GetBytes(httpResponse);
            await stream.WriteAsync(response, 0, response.Length);
            Console.WriteLine("Respuesta enviada al clienteTCP.\n\n");
            Console.WriteLine($"Mensaje enviado:\n{httpResponse}\n --- fin del mensaje enviado ---\n\n");
            */
            _ = Task.Run(() => ManejarSolicitud(httpRequest,stream));
        }
    }

    // Funcion iniciar servidor que se encarga de inicializar el servidor
    private static async Task IniciarServidor()
    {
        servidor = new TcpListener(localIPAddress, port);
        servidor.Start();
        Console.WriteLine($"Escuchando en puerto {port}, sirviendo desde {rootDirectory}, esperando solicitudes...\n\n");
    }

    private static async Task ManejarSolicitud(string httpRequest, NetworkStream stream)
    {
        Console.WriteLine("Manejando solicitud del clienteTCP...");

        // Separar la solicitud en líneas y partes
        string[] requestLines = httpRequest.Split('\n');
        string[] requestLineParts = requestLines[0].Split(' ');
        string method = requestLineParts[0];
        string path = requestLineParts[1];

        /*
      * Una solicitud se ve así (para tener referencia de lo que stá pasando arriba):
         GET / HTTP/1.1
         User-Agent: PostmanRuntime/7.39.0
         Accept: *{/}* //
         Postman - Token: 9bd2d3b2 - f428 - 43e1 - bf62 - 3af50edffedc
         Host: localhost: 7575
         Accept - Encoding: gzip, deflate, br
         Connection: keep - alive
        */

        // Probando que pueden leerse los valores de las variables
        // OK, esto funciona, se puede leer el método y la ruta
        Console.WriteLine($"PRUEBA DE LECTURA - Método: {method}, Ruta: {path}, Directorio raiz: {rootDirectory}");

        // Crear la respuesta HTTP
        // ESTA RESPUESTA HTTP FUNCIONA; TRATAR DE LLAMARLA DESDE EL IF Y ADAPTAR SEGUN  EL CASO
        /*
        string HttpStatusCode = "200";
        string HttpResponseMessage = "OK";
        string body = "body test desde manejo de solicitudes";
        string httpResponse = $"HTTP/1.1 {HttpStatusCode} {HttpResponseMessage}\nContent-Type: text/html; charset=UTF-8\n\n{body}";
        byte[] response = Encoding.UTF8.GetBytes(httpResponse);
        await stream.WriteAsync(response, 0, response.Length);
        Console.WriteLine("\nRespuesta enviada al cliente.\n");
        Console.WriteLine($"Mensaje enviado:\n{httpResponse}\n --- fin del mensaje enviado ---\n\n");
        */

        // TRATAR DE METER LOS ARCHIVOS CORRESPONDIENETS SEGUN EL CASO ACA ABAJO
        if (method == "GET")
        {
            if (path == "/")
            {
                Console.WriteLine("Metodo GET y path / - Solicitud de la página de inicio.");
            }
            else
            {
                string filePath = Path.Combine(currentDirectory, rootDirectory, path);
                //string filePath = Path.Combine(rootDirectory, path.TrimStart('/'));
                if (File.Exists(filePath))
                {
                    //await ServirArchivo(filePath, stream);
                    Console.WriteLine("Enviar el archivo solicitado existente");
                }
                else
                {
                    //await ServirArchivo("error_404.html", stream, 404);
                    Console.WriteLine("Metodo GET y path inexistenete en el directorio - Enviar el archivo personalizado 404");
                }
            }
        }
        else if (method == "POST")
        {
            Console.WriteLine("Enviar respuesta adecuada para POST");
        }
            
        


      




        // Cerrar la conexión con el clienteTCP después de haber manejado la solicitud
        clienteTCP.Close();
        Console.WriteLine("Conexión cerrada.\n\n");
    }


    // Función que se encarga de servir un archivo
    // El archivo debe estar comprimido con gzip
    // ver ejemplo en prueba.cs
    /*
    private static async Task ServirArchivo(string filePath, NetworkStream stream, int statusCode = 200)
    {
        byte[] fileBytes = await File.ReadAllBytesAsync(filePath);

        using (MemoryStream memoryStream = new MemoryStream())
        {
            using (GZipStream gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
            {
                await gzipStream.WriteAsync(fileBytes, 0, fileBytes.Length);
            }

            byte[] compressedBytes = memoryStream.ToArray();

            //cliente.GetStream().Write(compressedBytes, 0, compressedBytes.Length);
            stream.Write(compressedBytes, 0, compressedBytes.Length);
        }
    }
    */



    // Fución que lee la respuesta del servidor
    // ME PARECE QUE NO HACE FALTA ESTA FUNCIÓN
    // Debe leer la respuesta del servidor y devolverla como un string
    // Debe devolver un string vacío si no hay respuesta o es nula
    // Debe usar NetworkStream para leer la respuesta del servido

    // Función que loguea datos de las solicitudes y respuestas segun el tipo de solicitud GET o POST
    // Debe loguear la fecha y hora de la solicitud, el método, la ruta, el código de respuesta y el tamaño de la respuesta
    // Debe loguear en un archivo de texto
    // Debe crear un archivo por día para los logs de ese día





}
