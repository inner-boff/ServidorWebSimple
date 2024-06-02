using System;
using System.Net;
using System.Net.Sockets;
using System.Text;


namespace YourNamespace
{

    // creo que después pude separarse así en clases
    /*
    class Servidor
    {
    }

    class ManejadorDeSolicitudes
    {
    }

    class Logger
    {
    }
    */

   
    class Program
    {
        static void Main(string[] args)
        {
            // Declaramos un servidor de la clase TcpListener
            // Lo seteamos en null para que pase primero por el bloque try/catch
            TcpListener servidor = null;

            try
            {
                // Estas son las dos variables que necesita el servidor
                // una dirección IP (localhost en este caso) y un puerto
                // OJO: el puerto deberá ser configurable desde un archivo externo de configuración
                IPAddress localIPAddress = IPAddress.Parse("127.0.0.1");
                var port = 13000;

                // se crea el servidor con los argumentos 
                servidor = new TcpListener(localIPAddress,port);

                // Iniciamos el servidor
                servidor.Start();

                // Creamos una variable data para guardar lo que recibe el servidor
                // va a entrar como Stream pero queremos leerla como string
                string data = null;

                // loop while para que el servidor esté constanttemente escuchando
                // y cuadno alguien se conecte podamos leer su info
                while(true)
                {
                    Console.WriteLine($"Ecuchando en puerto {port}, esperando solicitudes...");
                    // Creamos un cliente Tcp Client: cuando el servidor (listener) acepta a un cliente,
                    // lo va a guardar aquí en este objeto "cliente":

                    TcpClient cliente = servidor.AcceptTcpClient();
                    // Cuando el servidor acepta al cliente, se establece la conexión entre ambos
                    Console.WriteLine("¡Conectado!");

                    // Recibe la info del cliente conectado como un stream
                    NetworkStream stream = cliente.GetStream();
                    // Guardamos en un búfer
                    byte[] buffer = new byte[1024];
                    int bytes = stream.Read(buffer, 0, buffer.Length);
                    // Para poder leer como HTTP
                    string httpRequest = Encoding.UTF8.GetString(buffer,0,bytes);
                    Console.WriteLine($"Mensaje recibido: {httpRequest}");


                }
                
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error {e}");
            }
            finally 
            {
                // Detiene el servidor
                servidor.Stop();

            }
        }
    }
}