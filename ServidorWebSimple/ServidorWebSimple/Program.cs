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
        // PRUEBA DE LECTURA - BORRAR ANTES DE ENTREGAR
        //Console.WriteLine($"PRUEBA DE LECTURA - Directorio actual: {currentDirectory}");
        rootDirectory = File.ReadAllText(Path.Combine(currentDirectory, "configuracion", "archivos_config.txt")).Trim();
        port = int.Parse(File.ReadAllText(Path.Combine(currentDirectory, "configuracion", "puerto_config.txt")).Trim());

        // Bloque try-catch para manejar excepciones de conexión al iniciar el servidor
        // Evita que se cierre el programa si hay un error
        try
        {
            IniciarServidor();

            // Bucle infinito para esperar a que los clientesTCP se conecten
            // Este loop hace que el servidor pueda aceptar múltiples conexiones --> CONCURRENCIA
             while (true)
            {
                // Esperar a que un clienteTCP se conecte
                // El método AcceptTcpClientAsync() devuelve un objeto TcpClient que representa al clienteTCP conectado
                clienteTCP = await servidor.AcceptTcpClientAsync();

                
                // Obtener la dirección IP del clienteTCP conectado para loguearla
                string clientIP = ((IPEndPoint)clienteTCP.Client.RemoteEndPoint).Address.ToString();

                Console.WriteLine("¡Cliente conectado!");


                // Obtener el stream de la conexión con el cliente
                NetworkStream stream = clienteTCP.GetStream();
                byte[] buffer = new byte[1024];
                // Esperar a que el cliente envíe datos
                int bytes = await stream.ReadAsync(buffer, 0, buffer.Length);
                string httpRequest = Encoding.UTF8.GetString(buffer, 0, bytes);
                Console.WriteLine($"Mensaje recibido:\n{httpRequest}--- fin del mensaje recibido ---\n\n");

                // Llamar a la función que maneja la solicitud del clienteTCP
                await ManejarSolicitud(httpRequest,stream, clientIP);
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error: {e.Message}");
        }
    }


    // CONFIRMAR si es necesario que esta función sea async
    // Yo diría que no porque el servidor se inicia una sola vez y no depende de nada más
    //private static async Task IniciarServidor()
    private static void IniciarServidor()
    {
        servidor = new TcpListener(localIPAddress, port);
        servidor.Start();
        Console.WriteLine($"Escuchando en puerto {port}, sirviendo desde {rootDirectory}, esperando solicitudes...\n\n");
    }

    // Este método sí tiene sentido que sea async porque depende de la conexión con el clienteTCP
    private static async Task ManejarSolicitud(string httpRequest, NetworkStream stream, string clientIP)
    {
        Console.WriteLine("Manejando solicitud del clienteTCP...");

        // Separar la solicitud en líneas y partes para obtener datos de la solicitud
        string[] requestLines = httpRequest.Split('\n');
        string[] requestLineParts = requestLines[0].Split(' ');
        string method = requestLineParts[0];
        // NUEVO - Comento este path proque va a ser reemplazado por el que se obtiene de la url
        // Hay que declararlo como fullPath para poder separarlo en path y parametrosConsulta
        //string path = requestLineParts[1];
        string fullPath = requestLineParts[1];

        // NUEVO - extraer ruta y parámetros de la solicitud por url
        string path = fullPath.Split('?')[0];
        // NUEVO - Si la url contiene parámetros, se extraen
        // EXPLICAR LOS METODOS USADOS PARA OBTENER LOS PARAMETROS DE LA URL
        string parametrosConsulta = fullPath.Contains('?') ? fullPath.Split('?')[1] : "";


        /*
        Ejemplos para armar consultas y verificar que se loguean

        -En la URL desde el navegador
        http://localhost:7575/index.html?nombre=Pedro&edad=22
        (sustituir nombre y edad por los parámetros y valores deseados e index.html por el archivo deseado o dejar vacío para el archivo por defecto)

        -Desde Postman
        Llenar los campos key/value en params al hacer la solicitud GET
        */

        string fileName = path.TrimStart('/');
        // Combinar el directorio actual, el directorio raíz y la ruta para obtener la ruta completa del archivo
        string filePathCompleto = Path.Combine(currentDirectory, rootDirectory, fileName);

        // Llama a la función para loguear datos de la solicitud
        // NUEVO - Se agregan los parámetros de la consulta a los datos que se loguean
        LoguearSolicitud(clientIP, method, path, parametrosConsulta);

        
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

        // Cerrar la conexión con el clienteTCP después de haber manejado la solicitud
        clienteTCP.Close();
        Console.WriteLine("Conexión cerrada.\n\n");
    }


    // Función para enviar respuesta al cliente comprimida en con GZIP
    // TAREA - Buscar una manera de probar que la respuesta se envía comprimida
    private static async Task EnviarRespuesta(string pathArchivo, NetworkStream stream)
    {
        // Leer el contenido del archivo existente como bytes
        byte[] fileBytes = await File.ReadAllBytesAsync(pathArchivo);

        // Crear una memoria en buffer para almacenar los datos comprimidos
        // todo este bloque es la compresión
        using (var memoryStream = new MemoryStream())
        {
            // Usar GZipStream para comprimir los datos y escribirlos en la memoria en buffer
            // CONFIRMAR si es cierto que usando "using" se hace automaticamente el dispose o probar incluir codigo de lámina de clase 6
            using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
            {
                await gzipStream.WriteAsync(fileBytes, 0, fileBytes.Length);

            }

            // Convertir el contenido comprimido a un array de bytes
            byte[] compressedBytes = memoryStream.ToArray();

            // incluir la respuesta HTTP con los encabezados necesarios
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

    // NUEVO - Se agrega el parámetro parametrosConsulta a lo que se va a loguear
    private static void LoguearSolicitud(string clientIP, string method, string path, string parametrosConsulta)
    {
        string logDirectory = Path.Combine(currentDirectory, "logs");
        if (!Directory.Exists(logDirectory))
        {
            Directory.CreateDirectory(logDirectory);
        }

        string logFilePath = Path.Combine(logDirectory, $"{DateTime.Now:yyyy-MM-dd}.log");
        // EXPLICAR COMO FUNCIONA ENVIRONMENT.NEWLINE
        string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - IP: {clientIP} - Method: {method} - Path: {path} - Parametros de Consulta: {parametrosConsulta}{Environment.NewLine}";

        File.AppendAllText(logFilePath, logEntry);
    }




}
