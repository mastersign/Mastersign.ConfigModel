using System;

namespace Mastersign.ConfigModel
{
    public class IncludeCycleException : Exception
    {
        public string[] PathCycle { get; set; }

        public IncludeCycleException(string[] cyclicPaths)
            : base("Include cycle detected")
        {
            PathCycle = cyclicPaths;
        }
    }
}
