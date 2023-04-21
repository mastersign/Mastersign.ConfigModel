using System;

namespace Mastersign.ConfigModel
{
    [Serializable]
    public class StringSourceNotFoundException : Exception
    {
        public string ModelFile { get; private set; }

        public string FileName { get; private set; }

        public StringSourceNotFoundException(string modelFile, string fileName)
            : base($"String source file '{fileName}' not found in config model '{modelFile}'.")
        {
            ModelFile = modelFile;
            FileName = fileName;
        }
    }

    [Serializable]
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
