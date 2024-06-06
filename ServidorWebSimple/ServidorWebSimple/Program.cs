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
    private static string currentDirectory;
    //Agregado para el manejo de la conexión, pero no estoy segura si hace falta
    private static NetworkStream stream;

    // añadidos para la compresión
    private static byte[] fileBytes;
    private static MemoryStream compressedStream;
    private static GZipStream gzipStream;
    private static byte[] compressedBytes;
    

    // Método Main asíncrono
    static async Task Main(string[] args)
    {
        currentDirectory = Directory.GetCurrentDirectory();
        Console.WriteLine($"Directorio actual: {currentDirectory}");
        // CHEQUEAR POR QUË NO ANDAN LOS RELATIVE PATHS EN VISUAL STUDIO
        rootDirectory = File.ReadAllText(Path.Combine(currentDirectory, "configuracion", "archivos_config.txt")).Trim();
        port = int.Parse(File.ReadAllText(Path.Combine(currentDirectory, "configuracion", "puerto_config.txt")).Trim());
        // En Visual Studio usar las rutas de abajo
        //rootDirectory = File.ReadAllText("C:\\Users\\pamel\\OneDrive\\Documentos\\pame\\IFTS11\\2024_parte1\\ProgSobreRedes\\ProyectoFinal\\ServidorWeb\\ServidorWebSimple\\ServidorWebSimple\\ServidorWebSimple\\configuracion\\archivos_config.txt").Trim();
        //port = int.Parse(File.ReadAllText("C:\\Users\\pamel\\OneDrive\\Documentos\\pame\\IFTS11\\2024_parte1\\ProgSobreRedes\\ProyectoFinal\\ServidorWeb\\ServidorWebSimple\\ServidorWebSimple\\ServidorWebSimple\\configuracion\\puerto_config.txt").Trim());


        // Bloque try-catch para manejar excepciones de conexión al iniciar el servidor
        // Evita que se cierre el programa si hay un error
        try
        {
            await IniciarServidor();

             while (true)
            {
                // Esperar a que un clienteTCP se conecte
                // El método AcceptTcpClientAsync() devuelve un objeto TcpClient que representa al clienteTCP conectado
                // Este loop hace que el servidor pueda aceptar múltiples conexiones
                clienteTCP = await servidor.AcceptTcpClientAsync();
                Console.WriteLine("¡Cliente conectado!");


                // Obtener el stream de la conexión con el clienteTCP
                // Esto permite leer y escribir datos en la conexión
                // Lo usamos luego en ManejarSolicitud() para enviar respuesta 
                NetworkStream stream = clienteTCP.GetStream();
                byte[] buffer = new byte[1024];
                // Esperar a que el clienteTCP envíe datos
                int bytes = await stream.ReadAsync(buffer, 0, buffer.Length);
                string httpRequest = Encoding.UTF8.GetString(buffer, 0, bytes);
                Console.WriteLine($"Mensaje recibido:\n{httpRequest}--- fin del mensaje recibido ---\n\n");
                // probar si se puede llamar a la función ManejarSolicitud() sin el task.run
                ManejarSolicitud(httpRequest,stream);
                //_ = Task.Run(() => ManejarSolicitud(httpRequest,stream));
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error: {e.Message}");
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
        // para loguear junto a la fecha y hora de la solicitud
        string Host = requestLines[1].Split(' ')[1];

        // Eliminar el caracter '/' del principio de la ruta para obtener el nombre del archivo
        string fileName = path.TrimStart('/');

        // Combinar el directorio actual, el directorio raíz y la ruta para obtener la ruta completa del archivo
        // Corregir para que incluya el directorio actual
        // Corregido: el problema estaba en que no estaba sumando el current directory al path porque Path.Combine no lo hace si está presente un / en el path (en este caso lo tenía porque venía de la solicitud http)
        string filePathCompleto = Path.Combine(currentDirectory, rootDirectory, fileName);

        /*
      * Una solicitud se ve así (para tener referencia de lo que stá pasando arriba):

        Desde Postman:
         GET / HTTP/1.1
         User-Agent: PostmanRuntime/7.39.0
         Accept:  //
         Postman - Token: 9bd2d3b2 - f428 - 43e1 - bf62 - 3af50edffedc
         Host: localhost: 7575
         Accept - Encoding: gzip, deflate, br
         Connection: keep - alive

        Desde el navegador:
        GET / HTTP/1.1
        Host: localhost:7575
        Connection: keep-alive
        sec-ch-ua: "Google Chrome";v="125", "Chromium";v="125", "Not.A/Brand";v="24"
        sec-ch-ua-mobile: ?0
            sec-ch-ua-platform: "Windows"
        Upgrade-Insecure-Requests: 1
        User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/125.0.0.0 Safari/537.36
        Accept: text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,image/apng,*{/}*;q=0.8,application/signed-exchange;v=b3;q=0.7
        Sec-Fetch-Site: none
        Sec-Fetch-Mode: navigate
        Sec-Fetch-User: ?1
        Sec-Fetch-Dest: document
        Accept-Encoding: gzip, deflate, br, zstd
        Accept-Language: en-US,en;q=0.9,es-AR;q=0.8,es-VE;q=0.7,es;q=0.6
        */

        // Pruebas de lectura de solicitud HTTP y variables - BORRAR ANTES DE ENTREGAR
        //Console.WriteLine($"PRUEBA DE LECTURA - SOLICITUD HTTP- Método: {method}, Ruta (path): {path}, Directorio raiz: {rootDirectory}");
        //Console.WriteLine($"PRUEBA DE LECTURA - VARIABLES VARIAS - \ncurrendDirectory: {currentDirectory} \nrootDirectory: {rootDirectory} \npath: {path}\nfilePathCompleto (debe incluir el current directory): {filePathCompleto} \nfileName (no debe incluir /): {fileName}\n FIN PRUEBA DE LECTURA\n\n");
        //Console.WriteLine($"PRUEBA DE LECTURA -filePathCorto (debe incluir /): {filePathCorto}");
        


        // Manejar la solicitud según el método (GET o POST)
        if (method == "GET")
        {
            if (path == "/" || path == null)
            {
                // Si la ruta solicitada es la raíz, enviar archivo index.html por defecto
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
            //Console.WriteLine("Enviar respuesta adecuada para POST");
            // Respuesta HTTP del servidor al cliente
            //string httpResponse = $"HTTP/1.1 201 Created\nContent-Type: text/html; charset=UTF-8\n\n<!DOCTYPE html>\n<html>\n<head>\n<title>Respuesta POST</title>\n</head>\n<body>\n<h1>Respuesta POST</h1>\n<p>¡Solicitud POST recibida!</p>\n</body>\n</html>\n";
            //string httpResponse = $"HTTP/1.1 201 Created\nContent-Type: text/html; charset=UTF-8\n\nPOST recibido correctamente.";
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
    private static async Task EnviarRespuesta(string pathArchivo, NetworkStream stream)
    {
        // Leer el contenido del archivo existente como bytes
        byte[] fileBytes = await File.ReadAllBytesAsync(pathArchivo);

        // Crear una memoria en buffer para almacenar los datos comprimidos
        using (var memoryStream = new MemoryStream())
        {
            // Usar GZipStream para comprimir los datos y escribirlos en la memoria en buffer
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



    // Función que loguea datos de las solicitudes y respuestas segun el tipo de solicitud GET o POST
    // Debe loguear la fecha y hora de la solicitud, el método, la ruta, el código de respuesta y el tamaño de la respuesta
    // Debe loguear en un archivo de texto
    // Debe crear un archivo por día para los logs de ese día





}
