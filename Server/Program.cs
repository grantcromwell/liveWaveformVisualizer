using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using OpenCvSharp;

class SimpleTcpSrvr
{
    public static void Main()
    {
        Socket listener = null;
        Socket client = null;

        try
        {
            listener = new Socket(
                AddressFamily.InterNetwork,
                SocketType.Stream,
                ProtocolType.Tcp);

            listener.Bind(
                new IPEndPoint(IPAddress.Any, 9050));

            listener.Listen(10);

            Console.WriteLine("Waiting for a video client...");

            client = listener.Accept();

            Console.WriteLine(
                "Connected to: " + client.RemoteEndPoint);

            Console.WriteLine(
                "Press ESC in the video window to stop.");

            byte[] header = new byte[4];

            while (ReceiveExactly(client, header))
            {
                int imageSize = IPAddress.NetworkToHostOrder(
                    BitConverter.ToInt32(header, 0));

                if (imageSize <= 0 || imageSize > 5000000)
                {
                    throw new InvalidDataException(
                        "Invalid image size.");
                }

                byte[] imageBytes = new byte[imageSize];

                if (!ReceiveExactly(client, imageBytes))
                {
                    throw new EndOfStreamException(
                        "Client disconnected before sending the image.");
                }

                Mat frame = Cv2.ImDecode(
                    imageBytes, ImreadModes.Color);

                try
                {
                    if (frame.Empty())
                    {
                        throw new InvalidDataException(
                            "Cannot decode image.");
                    }

                    Cv2.ImShow("Server - Live Video", frame);

                    if (Cv2.WaitKey(1) == 27)
                    {
                        break;
                    }
                }
                finally
                {
                    frame.Dispose();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Server error: " + ex.Message);
        }
        finally
        {
            if (client != null)
            {
                client.Close();
            }

            if (listener != null)
            {
                listener.Close();
            }
        }

        Console.WriteLine("Server stopped. Press ENTER to close.");
        Console.ReadLine();
    }

    static bool ReceiveExactly(Socket socket, byte[] buffer)
    {
        int total = 0;

        while (total < buffer.Length)
        {
            int received = socket.Receive(
                buffer,
                total,
                buffer.Length - total,
                SocketFlags.None);

            if (received == 0)
            {
                if (total == 0)
                {
                    return false;
                }

                throw new EndOfStreamException(
                    "Connection closed during a transfer.");
            }

            total += received;
        }

        return true;
    }
}