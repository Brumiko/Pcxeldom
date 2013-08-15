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

namespace ElzeKool.io.sht11_io
{
    /// <summary>
    /// Interface for SHT11
    /// See <see cref="SHT11_GPIO_IOProvider"/> for an implementation
    /// </summary>
    public interface ISHT11_IOProvider
    {
        bool Data
        {
            get;
            set;
        }

        bool Clock
        {
            get;
            set;
        }
    }
}
