using System.Drawing;
using System.Net.Sockets;
using System.Text;

int port;

do
{
    Console.Write("Enter the port: ");

    _ = int.TryParse(Console.ReadLine(), out port);
} while (port <= 0);

TcpListener tcpListener = new TcpListener(System.Net.IPAddress.Any, port);

tcpListener.Start();

Console.WriteLine();

while (true)
{
    Console.WriteLine("Waiting for connection...");

    TcpClient client = tcpListener.AcceptTcpClient();

    _ = Task.Run(() =>
    {
        try
        {
            Recieve(client);
        }
        catch (Exception ex)
        {
            WriteLog(client, ex.ToString());
        }
    });
}

void Recieve(TcpClient client)
{
    WriteLog(client, "Connected");

    NetworkStream ns = client.GetStream();

    byte[] buffer = new byte[4];

    WriteLog(client, "Waiting for data...");

    _ = ns.Read(buffer);

    int size = BitConverter.ToInt32(buffer);

    WriteLog(client, "Image size: " + size);

    buffer = new byte[size];

    WriteLog(client, "Reading image...");

    int bytesRead = ns.Read(buffer);

    WriteLog(client, "Bytes read: " + bytesRead);

    WriteLog(client, "Processing image...");

    MemoryStream ms = new MemoryStream(buffer);

    using Bitmap image = new Bitmap(ms);

    image.Save("img.bmp");

    WriteLog(client, "Converting image...");

    StringBuilder stringBuilder = new StringBuilder();

    for (int y = 0; y < image.Height; y++)
    {
        for (int x = 0; x < image.Width; x++)
        {
            double brightness = 1d / 255d * GetBrightness(image.GetPixel(x, y));
            char c = brightness switch
            {
                double i when i is >= 0 and < 0.2 => '-',
                double i when i is > 0.2 and < 0.4 => '+',
                double i when i is > 0.4 and < 0.6 => '#',
                double i when i is > 0.6 and < 0.8 => '%',
                double i when i is > 0.8 and <= 1 => '@',
                _ => '!'
            };

            _ = stringBuilder.Append(c);
        }
        _ = stringBuilder.AppendLine();
    }

    File.WriteAllText("output.txt", stringBuilder.ToString());

    buffer = Encoding.ASCII.GetBytes(stringBuilder.ToString());

    WriteLog(client, "Result size: " + buffer.Length);

    WriteLog(client, "Sending result to client...");

    ns.Write(BitConverter.GetBytes(buffer.Length));
    ns.Write(buffer);

    WriteLog(client, "Completed!");
}

double GetBrightness(Color color)
{
    return (0.2126 * color.R) + (0.7152 * color.G) + (0.0722 * color.B);
}

void WriteLog(TcpClient tcpClient, string message)
{
    Console.WriteLine($"[{tcpClient.Client.RemoteEndPoint}] -> {message}");
}