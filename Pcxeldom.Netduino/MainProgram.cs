using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using ElzeKool.io;
using ElzeKool.io.sht11_io;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;

namespace Pcxeldom.Netduino
{
    public class MainProgram
    {
        #region variables & constants

        // TODO: read values from microSD.

        const string host = "api.pachube.com";
        const string apiKey = "45oCGyxjCOrKFS8IaRjiTHS4-6JGB4PBY0l_PqRk7k4";
        const string feedID = "46053";
        const int samplingPeriod = 60000; // 60 seconds.
        const string CRLF = "\r\n";

        static OutputPort errLED = new OutputPort(Pins.ONBOARD_LED, false);
        static OutputPort sigLED = new OutputPort(Pins.GPIO_PIN_D11, false);
        static Socket socket = null;
        static double temperature = -274;
        static double humidity = -1;
        #endregion

        public static void Main()
        {
            #region Setting up SHT1x sensor.
            // Create IO provider (data pin - D13, clock pin - D12) and sensor instance.
            SensirionSHT11 sht1x = new SensirionSHT11(new SHT11_GPIO_IOProvider(Pins.GPIO_PIN_D13, Pins.GPIO_PIN_D12));

            // Sensor instance software reset.
            if (sht1x.SoftReset())
            {
                errLED.Write(true);
                throw new Exception("Error reseting SHT1x sensor.");
            }
            #endregion

            #region SHT1x sensor diagnostics.
            // Try setting sensor to GREATER sensitivity.
            if (sht1x.WriteStatusRegister((SensirionSHT11.SHT11Settings.NullFlag)))
            {
                errLED.Write(true);
                throw new Exception("Error setting sensor to greater sensitivity (NullFlag).");
            }
            Debug.Print("Temperature (RAW) 14-bit: " + sht1x.ReadTemperatureRaw());
            Debug.Print("Humidity (RAW) 12-bit: " + sht1x.ReadHumidityRaw());

            // Try setting sensor to (battery saving!) LESSER sensitivity.
            if (sht1x.WriteStatusRegister(SensirionSHT11.SHT11Settings.LessAcurate))
            {
                errLED.Write(true);
                throw new Exception("Error setting sensor to lesser sensitivity.");
            }
            Debug.Print("Temperature (RAW) 12-bit: " + sht1x.ReadTemperatureRaw());
            Debug.Print("Humidity (RAW) 8-bit: " + sht1x.ReadHumidityRaw());
            #endregion

            while (true)
            {
                // Reset error LED.
                errLED.Write(false);

                #region Creating connection to the Internet of Things server (Cosm/Pachube).
                if (socket == null)
                {
                    try
                    {
                        Debug.Print("Connecting to the Internet of Things server...");
                        // Get IP data by domain name.
                        IPHostEntry hostEntry = Dns.GetHostEntry(host);
                        IPAddress hostAddress = hostEntry.AddressList[0];
                        IPEndPoint remoteEndPoint = new IPEndPoint(hostAddress, 80);

                        // Connect!
                        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        socket.Connect(remoteEndPoint);
                        socket.SetSocketOption(SocketOptionLevel.Tcp, SocketOptionName.NoDelay, true);
                        socket.SendTimeout = samplingPeriod / 3;
                        Debug.Print("Connected!");
                    }
                    catch (Exception ex)
                    {
                        errLED.Write(true);
                        if (socket != null)
                        {
                            socket.Close();
                        }
                        socket = null;
                        Debug.Print("Error when establishing connection: " + ex.Message);
                    }
                }
                #endregion

                #region Sending sensor reading to the Internet of Things server (Pachube/Cosm).
                if (socket != null)
                {
                    try
                    {
                        //Reading sensors and displaying measured data for debugging purposes.
                        temperature = sht1x.ReadTemperature(SensirionSHT11.SHT11VDD_Voltages.VDD_3_5V, SensirionSHT11.SHT11TemperatureUnits.Celcius);
                        humidity = sht1x.ReadRelativeHumidity(SensirionSHT11.SHT11VDD_Voltages.VDD_3_5V);
                        Debug.Print("Temperature [C]: " + temperature);
                        Debug.Print("Humidity [%]: " + humidity);

                        Debug.Print("Sending sensor readings to the Internet of Things server...");
                        byte[] contentBuffer = Encoding.UTF8.GetBytes("Temperatura," + temperature.ToString("f") + CRLF + "Vlaznost," + humidity.ToString("f"));

                        string requestLine = "PUT /v2/feeds/" + feedID + ".csv HTTP/1.1" + CRLF;
                        byte[] requestLineBuffer = Encoding.UTF8.GetBytes(requestLine);

                        string headers =
                            "Host: " + host + CRLF +
                            "X-PachubeApiKey: " + apiKey + CRLF +
                            "Content-Type: text/csv" + CRLF +
                            "Content-Length: " + contentBuffer.Length + CRLF +
                            CRLF;
                        byte[] headersBuffer = Encoding.UTF8.GetBytes(headers);

                        socket.Send(requestLineBuffer);
                        socket.Send(headersBuffer);
                        socket.Send(contentBuffer);

                        Debug.Print("Sensor readings sent!");
                    }
                    catch (SocketException ex)
                    {
                        errLED.Write(true);
                        socket.Close();
                        socket = null;
                        Debug.Print("Socket error: " + ex.Message);
                    }
                    catch (Exception ex)
                    {
                        errLED.Write(true);
                        Debug.Print("Error: " + ex.Message);
                    }
                }
                #endregion

                #region Turning on signal LED if conditions are met.
                try
                {
                    if (temperature >= 27 || humidity >= 70)
                    {
                        sigLED.Write(true);
                    }
                    else
                    {
                        sigLED.Write(false);
                    }
                }
                catch (Exception ex)
                {
                    errLED.Write(true);
                    throw new Exception("Error setting signal LED: " + ex.Message);
                }
                #endregion

                #region Waiting for next sampling period.
                int sleep = samplingPeriod - (int)((DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) % samplingPeriod);
                Debug.Print("Sleeping for " + (sleep / 1000).ToString() + " seconds.\r\n");
                Thread.Sleep(sleep);
                #endregion
            }
        }
    }
}
