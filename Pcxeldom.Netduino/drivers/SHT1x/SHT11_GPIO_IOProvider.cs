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
using Microsoft.SPOT.Hardware;

namespace ElzeKool.io.sht11_io
{
    /// <summary>
    /// IO Provider for SHT11 using GPIO pins
    /// </summary>
    public class SHT11_GPIO_IOProvider : ISHT11_IOProvider, IDisposable
    {
        // Ports used to communicate with SHT11
        private TristatePort DataPin;
        private OutputPort ClockPin;

        public bool Clock
        {
            get
            {
                // Read from ClockPin
                return ClockPin.Read();
            }

            set
            {
                // Write to ClockPin
                ClockPin.Write(value);
            }
        }

        /// <summary>
        /// Data Pin from SHT11
        /// 
        /// Notice:
        /// When high is written to Data pin, TriStatePort is made an input, on low an output
        /// </summary>
        public bool Data
        {
            get
            {
                // Make DataPin Inactive (Input) and read value
                if (DataPin.Active == true) { DataPin.Active = false; }
                return DataPin.Read();
            }

            set
            {
                // When HIGH make DataPin an input, when low make DataPin an output and make low
                if (value)
                {
                    // Make DataPin Inactive (Input
                    if (DataPin.Active == true) { DataPin.Active = false; }
                }
                else
                {
                    // Make DataPin Active and make low
                    if (DataPin.Active == false) { DataPin.Active = true; }
                    if (DataPin.Active == true) { DataPin.Write(false); }
                }

            }

        }

        /// <summary>
        /// IO Provider for SHT11 using GPIO pins
        /// </summary>
        /// <param name="Data">GPIO Pin for Data</param>
        /// <param name="Clock">GPIO Pin for Clock</param>
        public SHT11_GPIO_IOProvider(Cpu.Pin Data, Cpu.Pin Clock)
        {
            try
            {
                // Data
                DataPin = new TristatePort(Data, true, false, Port.ResistorMode.PullUp);
                if (DataPin.Active == true) { DataPin.Active = false; }

                // Clock
                ClockPin = new OutputPort(Clock, false);
            }
            catch
            {
                throw new Exception("SHT11_GPIO_IOProvider: Failed to initalize Clock and Data pins.");
            }
        }

        /// <summary>
        /// Release IO Pins
        /// </summary>
        public void Dispose()
        {
            try
            {
                DataPin.Dispose();
                ClockPin.Dispose();
            }
            catch
            {
                // Added by Slaven Brumec.
                throw new Exception("SHT11_GPIO_IOProvider: Failed to dispose IO pins.");
            }
        }
    }
}
