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
        // Toma el nombre del directorio raíz de un archivo de configuración
        currentDirectory = Directory.GetCurrentDirectory();
        rootDirectory = File.ReadAllText(Path.Combine(currentDirectory, "configuracion", "archivos_config.txt")).Trim();

        // Obtener el puerto de escucha del servidor
        // Toma el puerto de un archivo de configuración
        port = int.Parse(File.ReadAllText(Path.Combine(currentDirectory, "configuracion", "puerto_config.txt")).Trim());

        // Bloque try-catch para manejar excepciones de conexión al iniciar el servidor
        try
        {
            IniciarServidor();

            // Bucle infinito para esperar a que los clientesTCP se conecten y derivarlos a un hilo separado
             while (true)
            {
                // Esperar a que un clienteTCP se conecte
                clienteTCP = await servidor.AcceptTcpClientAsync();
                
                // Obtener la dirección IP del clienteTCP conectado para loguearla luego
                string clientIP = ((IPEndPoint)clienteTCP.Client.RemoteEndPoint).Address.ToString();

                Console.WriteLine("¡Cliente conectado!");

                // Llamar a la función para manejar la solicitud del clienteTCP en un hilo separado
                _ = Task.Run(() => ManejarSolicitud(clienteTCP, clientIP));
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error: {e.Message}");
        }
    }

    // Método para iniciar el servidor en la dirección IP y puerto especificado
    private static void IniciarServidor()
    {
        servidor = new TcpListener(localIPAddress, port);
        servidor.Start();
        Console.WriteLine($"Escuchando en puerto {port}, sirviendo desde {rootDirectory}, esperando solicitudes...\n\n");
    }

    // Método para manejar la solicitud del clienteTCP:
    // - Obtiene el stream de la conexión con el clienteTCP
    // - Lee los datos enviados por el clienteTCP y arma la solicitud HTTP
    // - Obtiene el método, la ruta y la versión del protocolo de la solicitud HTTP
    // - Envía una respuesta al clienteTCP según el método (GET o POST)
    // - Cierra la conexión con el clienteTCP
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
            Console.WriteLine($"**Mensaje recibido**\n{httpRequest}--- fin del mensaje recibido ---\n\n");    

            // Declarar variables para guardadr los datos de la solicitud
            // Separar la solicitud en líneas - cada linea es un string en un array de strings:
            string[] requestLines = httpRequest.Split('\n');

            // Separar la primera línea de la solicitud en sus partes separadas por ' ' (método, ruta, versión HTTP):
            string[] requestLineParts = requestLines[0].Split(' ');

            // Guardar el método, la ruta y la versión HTTP de la solicitud
            string method = requestLineParts[0];
            string fullPath = requestLineParts[1];
            string httpVersion = requestLineParts[2];

            // Si contiene parametros (se verifica si contiene el caracter '?') los extrae, si no, deja un string vacio
            string parametrosConsulta = fullPath.Contains('?') ? fullPath.Split('?')[1] : "";

            // Ruta de recurso solicitado: lo que está antes del '?' en la URL (si hay parametros de consulta)
            string path = fullPath.Split('?')[0];

            // Eliminar el caracter '/' del inicio de la ruta para obtener el nombre del archivo solicitado
            string nombreArchivo = path.TrimStart('/');

            // Combinar el directorio actual, el directorio raíz y el nombre del archivo para obtener la ruta completa del archivo
            string filePathCompleto = Path.Combine(currentDirectory, rootDirectory, nombreArchivo);

            // Llama a la función para loguear datos de la solicitud
            LoguearSolicitud(clientIP, method, path, httpVersion, parametrosConsulta);

        
            // Manejar la solicitud según el método (GET o POST)
            if (method == "GET")
            {
                if (path == "/" || path == null)
                {
                        // Si la ruta solicitada es el directorio raiz o no se especifica nada, 
                        // enviar archivo index.html por defecto y código 200
                        string pathArchivoDefault = Path.Combine(currentDirectory, rootDirectory, "index.html");
                        await EnviarRespuestaComprimida(pathArchivoDefault, stream, "200 OK"); 

                }
                else
                {

                    if (File.Exists(filePathCompleto))
                    {
                        // Si el archivo solicitado existe, enviar el archivo y código 200
                        await EnviarRespuestaComprimida(filePathCompleto, stream, "200 OK");

                    }
                    else
                    {
                        // Si el archivo solicitado no existe, enviar archivo personalizado de error y código 404
                        string pathArchivoError = Path.Combine(currentDirectory, rootDirectory, "error_404.html");
                        await EnviarRespuestaComprimida(pathArchivoError, stream, "404 Not Found");
                    

                    }
                }
            }      
            else if (method == "POST")
            {
                // Si el método es POST, enviar respuesta con código 201 Created
                // No envía contenido, solo los encabezados de respuesta
                await EnviarRespuestaHTTP(stream, "201 Created");
            }

        }
        catch (Exception e)
        {
            // Si ocurre un error, capturar la excepción y mostrar un mensaje en consola
            Console.WriteLine($"Error: {e.Message}");
        }

        finally
        {
            // Cerrar la conexión con el clienteTCP después de haber manejado la solicitud
            clienteTCP.Close();
            Console.WriteLine("Conexión cerrada.\n\n");
        }
    }

    // Función para enviar respuesta al cliente
    // - Toma el stream del cliente para enviar los encabezados
    // - Toma el código de estado HTTP para incluir en los encabezados de respuesta
    private static async Task EnviarRespuestaHTTP(NetworkStream stream, string statusCode)
    {
        string httpResponseHeaders = $"HTTP/1.1 {statusCode}\r\n" +
                                    "Content-Encoding: gzip\r\n" +
                                    "Content-Type: text/html; charset=UTF-8\r\n" +
                                    //(content != null ? $"Content-Length: {content.Length}\r\n" : "") +
                                    "\r\n";

        // Convertir los encabezados HTTP a bytes
        byte[] responseHeaders = Encoding.UTF8.GetBytes(httpResponseHeaders);

        // Enviar los encabezados HTTP al cliente
        await stream.WriteAsync(responseHeaders, 0, responseHeaders.Length);

        Console.WriteLine($"\n**Respuesta enviada al cliente**.\nEncabezados enviados:\n{httpResponseHeaders}\n --- fin de los encabezados enviados ---\n\n");
    }

    // Función para enviar archivo comprimido junto a los encabezados
    private static async Task EnviarRespuestaComprimida(string pathArchivo, NetworkStream stream, string statusCode)
    {


       // Abrir FileStream para leer el archivo
        using (FileStream fileStream = File.OpenRead(pathArchivo))
        {
            // Crear un buffer para leer el archivo en partes
            byte[] buffer = new byte[8192]; // Tamaño de buffer estándar
            int bytesRead;

            // Crear un GZipStream para comprimir y enviar directamente al NetworkStream del cliente
            using (var gzipStream = new GZipStream(stream, CompressionMode.Compress, true))
            {

                // Enviar los encabezados HTTP con el código de estado 
                await EnviarRespuestaHTTP(stream, statusCode);

                // Leer del archivo y escribir en el GZipStream en partes
                while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    await gzipStream.WriteAsync(buffer, 0, bytesRead);
                }
            }
        }

        Console.WriteLine($"Respuesta comprimida enviada al cliente con estado: {statusCode}");  
    }

   
    // Función para loguear los datos de la solicitud
    private static void LoguearSolicitud(string clientIP, string method, string path, string httpVersion, string parametrosConsulta)
    {
        // Crear un directorio para almacenar los logs
        string logDirectory = Path.Combine(currentDirectory, "logs");

        // Si el directorio no existe, crearlo
        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }

        // Crear un archivo de log con el nombre del día actual
        string logFilePath = Path.Combine(logDirectory, $"{DateTime.Now:yyyy-MM-dd}.log");

        // Crear una entrada de log con los datos especificados de la solicitud
        string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - IP: {clientIP} - Method: {method} - Path: {path} - Protocol: {httpVersion} -Parametros de Consulta: {parametrosConsulta}{Environment.NewLine}";

        // Abrir el archivo de log, agregar la entrada de log y cerrar el archivo
        // Si el archivo no existe, lo crea y escribe la entrada de log y lo cierra
        File.AppendAllText(logFilePath, logEntry);
    }

}
