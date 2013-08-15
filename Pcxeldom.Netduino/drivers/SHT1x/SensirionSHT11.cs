/**
 * Sensirion SHT11 Class for .NET Micro Framework 3.0
 *
 * (C)opyright 2008 Elze Kool, http://www.microframework.nl
 * 
 * This sourcecode is provided AS-IS. I take no responsibility for direct or indirect
 * damage coused by this program/class. 
 * 
 * You are free to use this class Non-Commercialy and Commercialy as long as you add the above
 * copyright as reference in your sourcecode.
 * 
 **/

using System;
using System.Threading;
using ElzeKool.io.sht11_io;

namespace ElzeKool.io
{
    public class SensirionSHT11
    {
        // IO Provider used to communicate with SHT11
        private ISHT11_IOProvider IOProvider;

        /// <summary>
        /// Commands to send to SHT11
        /// </summary>
        private enum SHT11Commands
        {
            MeasureTemperature = 0x03,
            MeasureRelativeHumidity = 0x05,
            ReadStatusRegister = 0x07,
            WriteStatusRegister = 0x06,
            SoftReset = 0x1E,
        }

        /// <summary>
        /// Select VDD voltage where SHT11 is powered from
        /// </summary>
        public enum SHT11VDD_Voltages
        {
            VDD_5V,
            VDD_4V,
            VDD_3_5V,
            VDD_3V,
            VDD_2_5V
        }

        /// <summary>
        /// Select temperature unit 
        /// </summary>
        public enum SHT11TemperatureUnits
        {
            Farenheid,
            Celcius
        }


        /// <summary>
        /// Settings that can be written to the Status register
        /// </summary>
        [Flags]
        public enum SHT11Settings
        {
            EndOfBattery = 0x40,
            Heater = 0x04,
            NoReloadFromOTP = 0x02,
            LessAcurate = 0x01,
            NullFlag = 0x00
        }


        /// <summary>
        /// Initiate SHT11 Communication
        /// </summary>
        private void InitiateTransmision()
        {
            // To initiate a transmission, a Transmission Start sequence
            // has to be issued. It consists of a lowering of the DATA line
            // while SCK is high, followed by a low pulse on SCK and
            // raising DATA again while SCK is still high

            // Raise Clock
            IOProvider.Clock = true;

            // Lower Data
            IOProvider.Data = false;

            // Lower and Raise Clock
            IOProvider.Clock = false;
            IOProvider.Clock = true;

            // Raise Data
            IOProvider.Data = true;

            // Lower Clock
            IOProvider.Clock = false;
        }

        /// <summary>
        /// Reset Communication with SHT11
        /// </summary>
        private void ResetCommunication()
        {
            // If communication with the device is lost the following signal
            // sequence will reset the serial interface: While leaving
            // DATA high, toggle SCK nine or more times

            // Lower Data
            IOProvider.Data = false;

            // Cycle Clock
            for (byte t = 0; t <= 10; t++)
            {
                IOProvider.Clock = true;
                IOProvider.Clock = false;
            }

            // Raise Data
            IOProvider.Data = true;
        }

        /// <summary>
        /// Send Command to SHT11
        /// </summary>
        /// <param name="Command">Command to send</param>
        /// <returns>true on error, false on succes</returns>
        private bool SendCommand(SHT11Commands Command)
        {
            // First Initiate command
            InitiateTransmision();

            // Send Command, MSB First
            return WriteByte((byte)Command);
        }

        /// <summary>
        /// Send Byte to SHT11
        /// </summary>
        /// <param name="Data">Byte to Send</param>
        /// <returns>ACK</returns>
        private bool WriteByte(byte Data)
        {
            // Send Byte, MSB First
            for (byte bit = 0; bit < 8; bit++)
            {
                // Raise or lower data
                IOProvider.Data = (((Data << bit) & 0x80) == 0x80);

                // Raise Clock
                IOProvider.Clock = true;

                // Raise Data to prevent IO conflict with SHT11
                IOProvider.Data = true;

                // Lower Clock
                IOProvider.Clock = false;
            }


            // SHT1x indicates the proper reception of a command by
            // pulling the DATA pin low (ACK bit) after the falling edge of
            // the 8th SCK clock. The DATA line is released (and goes
            // high) after the falling edge of the 9th SCK clock.
            IOProvider.Clock = true;
            bool AckBit = IOProvider.Data;
            IOProvider.Clock = false;
            return AckBit;
        }

        /// <summary>
        /// Read Byte from SHT11
        /// </summary>
        /// <param name="ACK">Do Ack</param>
        /// <returns>Read byte</returns>
        private byte ReadByte(bool ACK)
        {
            // Storage for read byte
            byte readTemp = 0;

            for (byte bit = 0; bit < 8; bit++)
            {
                // Shift bits in buffer to right
                readTemp = (byte) (readTemp << 1);

                // Raise Clock
                IOProvider.Clock = true;

                // Check Data for bit
                if (IOProvider.Data == true)
                {
                    readTemp |= 0x01;
                }

                // Lower Clock
                IOProvider.Clock = false;

            }

            // Ack
            IOProvider.Data = !ACK;
            IOProvider.Clock = true;
            IOProvider.Clock = false;
            IOProvider.Data = true;

            // Return Read byte
            return readTemp;
        }
        
        /// <summary>
        /// Soft Reset the SHT11, Reset interface, Clear status register 
        /// </summary>
        /// <returns>True on error, False on succes</returns>
        public bool SoftReset()
        {
            // SoftReset the SHT11
            bool error = SendCommand(SHT11Commands.SoftReset);

            // Wait at least 11ms
            Thread.Sleep(20);

            return error;
        }

        /// <summary>
        /// Write Status Register
        /// </summary>
        /// <param name="Settings">Settings to write</param>
        /// <returns>True on error, False on succes</returns>
        public bool WriteStatusRegister(SHT11Settings Settings)
        {
            // Send Command and Settings
            SendCommand(SHT11Commands.WriteStatusRegister);
            return WriteByte((byte) Settings);
        }

        /// <summary>
        /// Read Status Register
        /// </summary>
        /// <returns></returns>
        public SHT11Settings ReadStatusRegister()
        {
            // Send command and return result
            SendCommand(SHT11Commands.ReadStatusRegister);
            return (SHT11Settings) ReadByte(false);
        }

        /// <summary>
        /// Read temperature compensated relative humidity
        /// </summary>
        /// <param name="VDD">VDD for SHT11</param>
        /// <returns>Relative Humidity in percent</returns>
        public double ReadRelativeHumidity(SHT11VDD_Voltages VDD)
        {
            double c1;
            double c2;
            double c3;

            double t1;
            double t2;

            // Check if we have 8 or 12 bits humidity
            if (((byte) ReadStatusRegister() & 0x01) == 0x01)
            {
                // 8 bit
                c1 = -2.0468F;
                c2 = 0.5872F;
                c3 = -4.0845E-4F;
                t1 = 0.01F;
                t2 = 0.00128F;
            }
            else
            {
                // 12 bit
                c1 = -2.0468F;
                c2 = 0.0367F;
                c3 = -1.5955E-6F;
                t1 = 0.01F;
                t2 = 0.00008F;
            }

            // Read Humidity from SHT11
            double SOrh = ReadHumidityRaw();

            // Calculate relative humidity
            double RHlinear = c1 + (c2 * SOrh) + (c3 * (SOrh * SOrh));

            // Read Temperature for temperature compensation
            double Tc = ReadTemperature(VDD,SHT11TemperatureUnits.Celcius);

            // Calculate temperature compensated humidity
            double RHtrue = ((Tc - 25.0F) * (t1 + (t2 * SOrh))) + RHlinear;

            // Return Relative Humidity
            return RHtrue;
        }

        /// <summary>
        /// Read RAW Unprocessed Humidity
        /// </summary>
        /// <returns>SHT11 Humidity</returns>
        public int ReadHumidityRaw()
        {
            // Send MeasureRelativeHumidity command
            SendCommand(SHT11Commands.MeasureRelativeHumidity);

            // Wait until sensor is ready to send the result
            while (IOProvider.Data == true) { Thread.Sleep(1); }

            // Now Read data
            int readTemp = 0;
            readTemp |= ReadByte(true);     // Bits 15-8

            readTemp = readTemp << 8;
            readTemp |= ReadByte(false);    // Bits 7-0

            // Return result
            return readTemp;
        }

        /// <summary>
        /// Read Temperature 
        /// </summary>
        /// <param name="VDD">VDD Voltage for SHT11</param>
        /// <param name="TemperatureUnit">Temperature units to return</param>
        /// <returns>Temperature in TemperatureUnit units</returns>
        public double ReadTemperature(SHT11VDD_Voltages VDD, SHT11TemperatureUnits TemperatureUnit)
        {
            double readTemp = 0.0F;
            double multplier = 0.0F;

            if (TemperatureUnit == SHT11TemperatureUnits.Celcius)
            {
                switch (VDD)
                {
                    case SHT11VDD_Voltages.VDD_2_5V:
                        readTemp = -39.4F;
                        break;

                    case SHT11VDD_Voltages.VDD_3_5V:
                        readTemp = -39.7F;
                        break;

                    case SHT11VDD_Voltages.VDD_3V:
                        readTemp = -39.6F;
                        break;

                    case SHT11VDD_Voltages.VDD_4V:
                        readTemp = -39.8F;
                        break;

                    case SHT11VDD_Voltages.VDD_5V:
                        readTemp = -40.1F;
                        break;
                }
            }

            if (TemperatureUnit == SHT11TemperatureUnits.Farenheid)
            {
                switch (VDD)
                {
                    case SHT11VDD_Voltages.VDD_2_5V:
                        readTemp = -38.9F;
                        break;

                    case SHT11VDD_Voltages.VDD_3_5V:
                        readTemp = -39.5F;
                        break;

                    case SHT11VDD_Voltages.VDD_3V:
                        readTemp = -39.3F;
                        break;

                    case SHT11VDD_Voltages.VDD_4V:
                        readTemp = -39.6F;
                        break;

                    case SHT11VDD_Voltages.VDD_5V:
                        readTemp = -40.2F;
                        break;
                }
            }



            // Check if we have 12 or 14 bits temperature
            if (((byte) ReadStatusRegister() & 0x01) == 0x01)
            {
                // 12 Bit
                switch (TemperatureUnit)
                {
                    case SHT11TemperatureUnits.Farenheid:
                        multplier = 0.072F;
                        break;

                    case SHT11TemperatureUnits.Celcius:
                        multplier = 0.04F;
                        break;
                }
            }
            else
            {
                // 14 Bit
                switch (TemperatureUnit)
                {
                    case SHT11TemperatureUnits.Farenheid:
                        multplier = 0.018F;
                        break;

                    case SHT11TemperatureUnits.Celcius:
                        multplier = 0.01F;
                        break;
                }

            }

            // Calculate actual temperature
            readTemp += (double)ReadTemperatureRaw() * multplier;

            // Return temperature
            return readTemp;
        }

        /// <summary>
        /// Read RAW temperature value
        /// </summary>
        /// <returns>SHT11 Temperature</returns>
        public int ReadTemperatureRaw()
        {
            // Send MeasureTemperature command
            SendCommand(SHT11Commands.MeasureTemperature);

            // Wait until sensor is ready to send the result
            while (IOProvider.Data == true) { Thread.Sleep(1); }

            // Now Read data
            int readTemp = 0;
            readTemp |= ReadByte(true);     // Bits 15-8

            readTemp = readTemp << 8;
            readTemp |= ReadByte(false);    // Bits 7-0

            // Return result
            return readTemp;
        }


        /// <summary>
        /// Initialize SHT11 Class
        /// </summary>
        /// <param name="IOProvider">IOProvider to use for communication with the SHT11</param>
        public SensirionSHT11(ISHT11_IOProvider IOProvider)
        {
            // Store IOProvider
            this.IOProvider = IOProvider;
            
            // Reset Communications
            ResetCommunication();
        }
    }
}
