using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebScraper.Types
{
    class OnDriverFailEventArgs
    {
        public bool UseFallbackDriver;

        public OnDriverFailEventArgs(bool useFallbackDriver)
        {
            UseFallbackDriver = useFallbackDriver;
        }
    }
}
