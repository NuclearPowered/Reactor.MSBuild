using System;

namespace DataMiner
{
    public class DumperException : Exception
    {
        public DumperException(Exception exception) : base("Dumper crashed unexpectedly", exception)
        {
        }
    }
}
