using System.Net;
using System.Net.Sockets;
using System.Text; 
using System.IO.Compression;


class ServidorWebSimple
{
    private static string rootDirectory;
    private static int port;
    private static IPAddress localIPAddress = IPAddress.Parse("127.0.0.1");
    private static TcpListener servidor;
    private static TcpClient clienteTCP;
    private static string currentDirectory;

    
    static async Task Main(string[] args)
    {
        // Obtener el directorio actual y el directorio raíz de los archivos a servir
        currentDirectory = Directory.GetCurrentDirectory();
        rootDirectory = File.ReadAllText(Path.Combine(currentDirectory, "configuracion", "archivos_config.txt")).Trim();
        port = int.Parse(File.ReadAllText(Path.Combine(currentDirectory, "configuracion", "puerto_config.txt")).Trim());

        // Bloque try-catch para manejar excepciones de conexión al iniciar el servidor
        try
        {
            IniciarServidor();

            // Bucle infinito para esperar a que los clientesTCP se conecten
             while (true)
            {
                // Esperar a que un clienteTCP se conecte
                clienteTCP = await servidor.AcceptTcpClientAsync();

                
                // Obtener la dirección IP del clienteTCP conectado para loguearla
                string clientIP = ((IPEndPoint)clienteTCP.Client.RemoteEndPoint).Address.ToString();

                Console.WriteLine("¡Cliente conectado!");

                // Llamar a la función para manejar la solicitud del clienteTCP
                //await ManejarSolicitud(httpRequest,stream, clientIP);
                // MODIFICADO - para manejar las solicitudes en hilos separados, es necesario usar Task.Run
                // con la forma anterior (await) las solicitudes se manejaban en serie y por eso se rompía la conexión
                // cuando se llena el buffer de lectura
                _ = Task.Run(() => ManejarSolicitud(clienteTCP, clientIP));

            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error: {e.Message}");
        }
    }


    // CONFIRMAR si es necesario que esta función sea async o no
    // Yo diría que no porque el servidor se inicia una sola vez y no depende de nada más
    // private static async Task IniciarServidor()
    private static void IniciarServidor()
    {
        servidor = new TcpListener(localIPAddress, port);
        servidor.Start();
        Console.WriteLine($"Escuchando en puerto {port}, sirviendo desde {rootDirectory}, esperando solicitudes...\n\n");
    }

    // Este método sí tiene sentido que sea async porque se ejecuta cada vez que se recibe una solicitud
    private static async Task ManejarSolicitud(TcpClient clienteTCP, string clientIP)
    {
        Console.WriteLine("Manejando solicitud del clienteTCP...");

        // Obtener el stream de la conexión con el cliente
        NetworkStream stream = clienteTCP.GetStream();
        byte[] buffer = new byte[1024];

        try 
        {

            // Esperar a que el cliente envíe datos
            int bytes = await stream.ReadAsync(buffer, 0, buffer.Length);
            string httpRequest = Encoding.UTF8.GetString(buffer, 0, bytes);
            Console.WriteLine($"Mensaje recibido:\n{httpRequest}--- fin del mensaje recibido ---\n\n");    

            // Declarar variables para guardadr los datos de la solicitud
            // Separar la solicitud en líneas - cada linea es un string en un array de strings:
            string[] requestLines = httpRequest.Split('\n');

            // Separar la primera línea de la solicitud en sus partes separadas por ' ' (método, ruta, versión HTTP):
            string[] requestLineParts = requestLines[0].Split(' ');

            // Guardar el método, la ruta y la versión HTTP de la solicitud
            string method = requestLineParts[0];
            string fullPath = requestLineParts[1];
            string httpVersion = requestLineParts[2];

            // Ruta de recurso solicitado: lo que está antes del '?' en la URL
            string path = fullPath.Split('?')[0];

            // Si contiene parametros (se verifica si contiene el caracter '?') los extrae, si no, deja un string vacio
            string parametrosConsulta = fullPath.Contains('?') ? fullPath.Split('?')[1] : "";

            // Eliminar el caracter '/' del inicio de la ruta para obtener el nombre del archivo solicitado
            string fileName = path.TrimStart('/');

            // Combinar el directorio actual, el directorio raíz y la ruta para obtener la ruta completa del archivo
            string filePathCompleto = Path.Combine(currentDirectory, rootDirectory, fileName);

            // Llama a la función para loguear datos de la solicitud
            LoguearSolicitud(clientIP, method, path, httpVersion, parametrosConsulta);

        
            // Manejar la solicitud según el método (GET o POST)
            if (method == "GET")
            {
                if (path == "/" || path == null)
                {
                        // Si la ruta solicitada es el directorio raiz o no se especifica nada, enviar archivo index.html por defecto
                        string pathArchivoDefault = Path.Combine(currentDirectory, rootDirectory, "index.html");
                        await EnviarRespuesta(pathArchivoDefault,stream);

                }
                else
                {

                    if (File.Exists(filePathCompleto))
                    {
                        // Si el archivo solicitado existe, enviar el archivo
                        await EnviarRespuesta(filePathCompleto,stream);

                    }
                    else
                    {
                        // Si el archivo solicitado no existe, enviar archivo personalizado con error 404
                        string pathArchivoError = Path.Combine(currentDirectory, rootDirectory, "error_404.html");
                        // PENDIENTE - modificar funcion existente o crear una nueva para que el header sea 404 en caso de archivo no encontrado
                        await EnviarRespuesta(pathArchivoError,stream);
                    

                    }
                }
            }      
            else if (method == "POST")
            {
                // Si el método es POST, enviar respuesta con código 201 Created
                string httpResponse = $"HTTP/1.1 201 Created\nContent-Type: text/html; charset=UTF-8\n\n";
                byte[] response = Encoding.UTF8.GetBytes(httpResponse);
                await stream.WriteAsync(response, 0, response.Length);
                // Escribir respuesta en consola
                Console.WriteLine($"\n**Respuesta enviada al cliente**.\nMensaje enviado:\n{httpResponse}\n --- fin del mensaje enviado ---\n\n");
            }

        }
        catch (Exception e)
        {
            Console.WriteLine($"Error: {e.Message}");
        }

        finally
        {
            // Cerrar la conexión con el clienteTCP después de haber manejado la solicitud
            clienteTCP.Close();
            Console.WriteLine("Conexión cerrada.\n\n");
        }

    }


    // Función para enviar respuesta al cliente comprimida en con GZIP
    // PENDIENTE - HAcer pruebas para verificar que la compresión funciona correctamente
    // comparando el tamaño del archivo original con el comprimido
    // PENDIENTE - Modificar para que cuando el cliente solicite un archivo no existente, envíe un header código 404
    private static async Task EnviarRespuesta(string pathArchivo, NetworkStream stream)
    {
        // Leer el contenido del archivo existente como bytes
        byte[] fileBytes = await File.ReadAllBytesAsync(pathArchivo);

        // Crear una memoria en buffer para almacenar los datos comprimidos
        // todo este bloque es la compresión
        using (var memoryStream = new MemoryStream())
        {
            // Usar GZipStream para comprimir los datos y escribirlos en la memoria en buffer
            // PENDIENTE - CONFIRMAR si es cierto que con "using" se hace automaticamente el dispose o probar/incluir codigo de lámina de clase 6
            using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
            {
                await gzipStream.WriteAsync(fileBytes, 0, fileBytes.Length);

            }

            // Convertir el contenido comprimido a un array de bytes
            byte[] compressedBytes = memoryStream.ToArray();

            // incluir la respuesta HTTP con los encabezados necesarios
            // PENDIENTE - Modificar para que cuando el cliente solicite un archivo no existente, envíe un header código 404
            string httpResponseHeaders = "HTTP/1.1 200 OK\r\n" +
                                  "Content-Encoding: gzip\r\n" +
                                  "Content-Type: text/html; charset=UTF-8\r\n" +
                                  $"Content-Length: {compressedBytes.Length}\r\n" +
                                  "\r\n";

            // Convertir los encabezados HTTP a bytes
            byte[] responseHeaders = Encoding.UTF8.GetBytes(httpResponseHeaders);

            // Enviar los encabezados HTTP al cliente
            await stream.WriteAsync(responseHeaders, 0, responseHeaders.Length);

            // Enviar el contenido comprimido al cliente
            await stream.WriteAsync(compressedBytes, 0, compressedBytes.Length);

            // Escribir respuesta en consola
            Console.WriteLine($"\n**Respuesta enviada al cliente**.\nEncabezados enviados:\n{httpResponseHeaders}\n --- fin de los encabezados enviados ---\n\n");
        }
    }

    // Función para loguear los datos de la solicitud
    private static void LoguearSolicitud(string clientIP, string method, string path, string httpVersion, string parametrosConsulta)
    {
        string logDirectory = Path.Combine(currentDirectory, "logs");
        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }

        string logFilePath = Path.Combine(logDirectory, $"{DateTime.Now:yyyy-MM-dd}.log");
        // PENDIENTE - Explicar qué hace Environment.NewLine
        string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - IP: {clientIP} - Method: {method} - Path: {path} - Protocol: {httpVersion} -Parametros de Consulta: {parametrosConsulta}{Environment.NewLine}";

        File.AppendAllText(logFilePath, logEntry);
    }




}
