using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NAudio.Wave;
using OpenCvSharp;

class SimpleTcpClient
{
    public static void Main()
    {
        // Use the server computer's IP if running on two computers.
        string serverIp = "127.0.0.1";

        try
        {
            using (Socket server = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp))
            using (VideoCapture camera = new VideoCapture(0, VideoCaptureAPIs.DSHOW))
            using (Mat frame = new Mat())
            {
                if (!camera.IsOpened())
                {
                    Console.WriteLine(  "Cannot open webcam. Try camera index 1 " +"or check camera permissions.");
                    return;
                }

                camera.Set(VideoCaptureProperties.FrameWidth, 640);
                camera.Set(VideoCaptureProperties.FrameHeight, 480);

                Console.WriteLine("Connecting to server...");

                server.Connect(new IPEndPoint(IPAddress.Parse(serverIp), 9050));

                Console.WriteLine("Connected. Sending video.");
                Console.WriteLine( "Press ESC in the preview window to stop.");

                while (true)
                {
                    if (!camera.Read(frame) || frame.Empty())
                    {
                        Console.WriteLine("Cannot read camera frame.");
                        break;
                    }

                    // Convert the camera frame into JPEG bytes.
                    byte[] imageBytes;
                    Cv2.ImEncode(".jpg", frame, out imageBytes);

                    // Send a 4-byte image size before each image.
                    byte[] header = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(imageBytes.Length));

                    SendExactly(server, header);
                    SendExactly(server, imageBytes);

                    Cv2.ImShow("Client webcam preview", frame);

                    if (Cv2.WaitKey(33) == 27)
                    {
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Client error: " + ex.Message);
        }
        finally
        {
            Cv2.DestroyAllWindows();
        }

        Console.WriteLine("Client stopped.");
    }

    static void SendExactly(Socket socket, byte[] buffer)
    {
        int total = 0;

        while (total < buffer.Length)
        {
            int sent = socket.Send(
                buffer,
                total,
                buffer.Length - total,
                SocketFlags.None);

            if (sent == 0)
            {
                throw new IOException(
                    "Connection closed while sending.");
            }

            total += sent;
        }
        
    }
}