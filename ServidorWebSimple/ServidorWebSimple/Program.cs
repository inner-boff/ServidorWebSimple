using System;
using System.Net;
using System.Net.Sockets;
using System.IO; // Espacio de nombres para clases que permiten la manipulación de archivos y directorios.
using System.Text; // Espacio de nombres para clases que permiten la manipulación de texto.
using System.Threading.Tasks; // Espacio de nombres para clases relacionadas con tareas asincrónicas.
using System.IO.Compression; // Espacio de nombres para clases que permiten la compresión de archivos.


class ServidorWebSimple
{
    private static string rootDirectory;
    private static int port;
    private static IPAddress localIPAddress = IPAddress.Parse("127.0.0.1");
    private static TcpListener servidor;
    private static TcpClient cliente;

    // Método Main asíncrono
    static async Task Main(string[] args)
    {
        // Chequear por qué no funcionan las rutas relativas
        rootDirectory = File.ReadAllText("C:\\Users\\pamel\\OneDrive\\Documentos\\pame\\IFTS11\\2024_parte1\\ProgSobreRedes\\ProyectoFinal\\ServidorWeb\\ServidorWebSimple\\ServidorWebSimple\\ServidorWebSimple\\configuracion\\archivos_config.txt").Trim();
        port = int.Parse(File.ReadAllText("C:\\Users\\pamel\\OneDrive\\Documentos\\pame\\IFTS11\\2024_parte1\\ProgSobreRedes\\ProyectoFinal\\ServidorWeb\\ServidorWebSimple\\ServidorWebSimple\\ServidorWebSimple\\configuracion\\puerto_config.txt").Trim());


        
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
            // Esperar a que un cliente se conecte
            // El método AcceptTcpClientAsync devuelve un objeto TcpClient que representa al cliente conectado
            // Este loop hace que el servidor pueda aceptar múltiples conexiones
            cliente = await servidor.AcceptTcpClientAsync();
            Console.WriteLine("¡Cliente conectado!");

            NetworkStream stream = cliente.GetStream();
            byte[] buffer = new byte[1024];
            // Esperar a que el cliente envíe datos
            int bytes = await stream.ReadAsync(buffer, 0, buffer.Length);
            string httpRequest = Encoding.UTF8.GetString(buffer, 0, bytes);
            Console.WriteLine($"Mensaje recibido:\n{httpRequest}\n --- fin del mensaje ---\n\n");
            // Manejar la solicitud en un hilo aparte
            // Pregunta: ¿Es necesario hacer esto en un hilo aparte?
            // Respuesta: Sí, porque si no se hace, el servidor no puede seguir aceptando conexiones
            _ = Task.Run(() => ManejarSolicitudes(httpRequest));
        }
    }

    // Funcion iniciar servidor que se encarga de inicializar el servidor
    private static async Task IniciarServidor()
    {
        servidor = new TcpListener(localIPAddress, port);
        servidor.Start();
        Console.WriteLine($"Escuchando en puerto {port}, sirviendo desde {rootDirectory}, esperando solicitudes...\n\n");
    }

    private static async Task ManejarSolicitudes(string httpRequest)
    {
        Console.WriteLine("MANEJANDO SOLICITUDES");

        string[] requestLines = httpRequest.Split('\n');
        string[] requestLineParts = requestLines[0].Split(' ');
        string method = requestLineParts[0];
        string path = requestLineParts[1];

        /*
         * Una respuesta se ve así:
            GET / HTTP/1.1
            User-Agent: PostmanRuntime/7.39.0
            Accept: *{/}* //
            Postman - Token: 9bd2d3b2 - f428 - 43e1 - bf62 - 3af50edffedc
            Host: localhost: 7575
            Accept - Encoding: gzip, deflate, br
            Connection: keep - alive
         * 

         */

        // Probando que pueden leerse los valores de las variables
        Console.WriteLine($"Método: {method}, Ruta: {path}");

        // Construir la ruta completa del archivo solicitado
        string filePath = Path.Combine(rootDirectory, path);
        Console.WriteLine($"Ruta al archivo: {filePath}");

        // Servir el archivo default y el de error cuando corresponda

        if (path == "/")
        {
            
        }

        // Cerrar la conexión con el cliente después de haber manejado la solicitud
        cliente.Close();
        Console.WriteLine("Conexión cerrada.\n\n");
    }


   

}
