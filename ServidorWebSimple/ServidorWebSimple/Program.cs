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
                // El método AcceptTcpClientAsync devuelve un objeto TcpClient que representa al clienteTCP conectado
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

        // Eliminar el caracter '/' del principio de la ruta para obtener el nombre del archivo
        string fileName = path.TrimStart('/');

        // Combinar el directorio actual, el directorio raíz y la ruta para obtener la ruta completa del archivo
        // Corregir para que ioncluya el directorio actual
        //string filePathCorto = Path.Combine(currentDirectory, rootDirectory, path);
        // Listo: el problema estaba en que no estaba sumando el current directory al path porque Path.Combine no lo hace si está presente un / en el path (en este caso lo tenía porque venía de la solicitud http)
        string filePathCompleto = Path.Combine(currentDirectory, rootDirectory, fileName);

        /*
      * Una solicitud se ve así (para tener referencia de lo que stá pasando arriba):4

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
                string archivoDefault = File.ReadAllText(Path.Combine(currentDirectory, rootDirectory, "index.html")).Trim();
                string pathArchivoDefault = Path.Combine(currentDirectory, rootDirectory, "index.html");

                //ServirArchivoComprimido(pathArchivoDefault, stream);

                // Ahora hay que tratar de envolver esto para que use compresion con gzip
                // Respuesta HTTP del servidor al cliente
                string httpResponse = $"HTTP/1.1 200 OK\nContent-Type: text/html; charset=UTF-8\n\n{archivoDefault}";
                byte[] response = Encoding.UTF8.GetBytes(httpResponse);
                await stream.WriteAsync(response, 0, response.Length);
                // Escribir respuesta en consola
                Console.WriteLine($"\n**Respuesta enviada al cliente**.\nMensaje enviado:\n{httpResponse}\n --- fin del mensaje enviado ---\n\n");
            }
            else
            {

                if (File.Exists(filePathCompleto))
                {
                   // Console.WriteLine("PRUEBA - Debería dar TRUE " + File.Exists(filePathCompleto));
                    string archivoExistente = File.ReadAllText(Path.Combine(currentDirectory, rootDirectory, fileName)).Trim();
                    
                    // Respuesta HTTP del servidor al cliente
                    string httpResponse = $"HTTP/1.1 200 OK\nContent-Type: text/html; charset=UTF-8\n\n{archivoExistente}";
                    byte[] response = Encoding.UTF8.GetBytes(httpResponse);
                    await stream.WriteAsync(response, 0, response.Length);
                    // Escribir respuesta en consola
                    Console.WriteLine($"\n**Respuesta enviada al cliente**.\nMensaje enviado:\n{httpResponse}\n --- fin del mensaje enviado ---\n\n");

                }
                else
                {
                    string archivoError = File.ReadAllText(Path.Combine(currentDirectory, rootDirectory, "error_404.html")).Trim();
                    
                    // Respuesta HTTP del servidor al cliente
                    string httpResponse = $"HTTP/1.1 404 Not Found\nContent-Type: text/html; charset=UTF-8\n\n{archivoError}";
                    byte[] response = Encoding.UTF8.GetBytes(httpResponse);
                    await stream.WriteAsync(response, 0, response.Length);
                    // Escribir respuesta en consola
                    Console.WriteLine($"\n**Respuesta enviada al cliente**.\nMensaje enviado:\n{httpResponse}\n --- fin del mensaje enviado ---\n\n");

                }
            }
        }      
        else if (method == "POST")
        {
            //Console.WriteLine("Enviar respuesta adecuada para POST");
            // Respuesta HTTP del servidor al cliente
            string httpResponse = $"HTTP/1.1 201 Created\nContent-Type: text/html; charset=UTF-8\n\nPOST recibido correctamente.";
            byte[] response = Encoding.UTF8.GetBytes(httpResponse);
            await stream.WriteAsync(response, 0, response.Length);
            // Escribir respuesta en consola
            Console.WriteLine($"\n**Respuesta enviada al cliente**.\nMensaje enviado:\n{httpResponse}\n --- fin del mensaje enviado ---\n\n");
        }

        // Cerrar la conexión con el clienteTCP después de haber manejado la solicitud
        clienteTCP.Close();
        Console.WriteLine("Conexión cerrada.\n\n");
    }



    // Escribe una funcion que se encargue de servir un archivo y los comprima en gzip
    // Debe devolver un código de estado 200 si el archivo se sirvió correctamente
    // Debe devolver un código de estado 404 si el archivo no se encontró
    // Debe devolver un código de estado 500 si hubo un error al servir el archivo
    // Escribe abajo la funcion
    

    // Función que se encarga de servir un archivo
    // El archivo debe estar comprimido con gzip
    // ver ejemplo en prueba.cs
    // Debe devolver un código de estado 200 si el archivo se sirvió correctamente
    /*
    private static async Task ServirArchivoComprimido(string filePath, NetworkStream stream, int statusCode = 200)
    {
        byte[] fileBytes = await File.ReadAllBytesAsync(filePath);

        using (MemoryStream memoryStream = new MemoryStream())
        {
            using (GZipStream gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
            {
                await gzipStream.WriteAsync(fileBytes, 0, fileBytes.Length);
            }

            byte[] compressedBytes = memoryStream.ToArray();

            // Agregar las cabeceras necesarias para que el archivo se pueda ver en el navegador
            string httpResponse = $"HTTP/1.1 {statusCode} OK\nContent-Type: text/html; charset=UTF-8\nContent-Encoding: gzip\nContent-Length: {compressedBytes.Length}\n\n";
            byte[] responseHeaders = Encoding.UTF8.GetBytes(httpResponse);

            await stream.WriteAsync(responseHeaders, 0, responseHeaders.Length);
            await stream.WriteAsync(compressedBytes, 0, compressedBytes.Length);
        }

        Console.WriteLine($"Archivo {filePath} comprimido y servido correctamente.");
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
