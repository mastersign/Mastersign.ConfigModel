using System;
using System.Runtime.Serialization;

namespace Mastersign.ConfigModel
{
    [Serializable]
    public abstract class ConfigModelException : Exception
    {
        protected ConfigModelException(string message) : base(message)
        {
        }

        protected ConfigModelException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ConfigModelException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    public abstract class ConfigModelFileNotFoundException : ConfigModelException
    {
        public string FileName { get; private set; }

        protected ConfigModelFileNotFoundException(string message, string fileName, Exception innerException) 
            : base(message, innerException)
        {
            FileName = fileName;
        }
    }

    [Serializable]
    public class ConfigModelLayerNotFoundException : ConfigModelFileNotFoundException
    {
        public ConfigModelLayerNotFoundException(string message, string fileName, Exception innerException)
            : base(message, fileName, innerException)
        {
        }
    }

    [Serializable]
    public class ConfigModelStringSourceNotFoundException : ConfigModelFileNotFoundException
    {
        public string ModelFile { get; private set; }

        public ConfigModelStringSourceNotFoundException(string message, string modelFile, string fileName, Exception innerException)
            : base(message, fileName, innerException)
        {
            ModelFile = modelFile;
        }
    }

    [Serializable]
    public class ConfigModelIncludeNotFoundException : ConfigModelFileNotFoundException
    {
        public string ModelFile { get; private set; }

        public ConfigModelIncludeNotFoundException(string message, string modelFile, string fileName, Exception innerException)
            : base(message, fileName, innerException)
        {
            ModelFile = modelFile;
        }
    }

    [Serializable]
    public class ConfigModelIncludeCycleException : Exception
    {
        public string[] PathCycle { get; set; }

        public ConfigModelIncludeCycleException(string[] cyclicPaths)
            : base("Include cycle detected")
        {
            PathCycle = cyclicPaths;
        }
    }

    [Serializable]
    public abstract class ConfigModelLoadException : ConfigModelException
    {
        public string FileName { get; private set; }

        protected ConfigModelLoadException(string message, string fileName, Exception innerException)
            : base(message, innerException)
        {
            FileName = fileName;
        }
    }

    [Serializable]
    public class ConfigModelLayerLoadException : ConfigModelLoadException
    {
        public ConfigModelLayerLoadException(string message, string fileName, Exception innerException)
            : base(message, fileName, innerException)
        {
        }
    }

    [Serializable]
    public class ConfigModelStringSourceLoadException : ConfigModelLoadException
    {
        public string ModelFile { get; private set; }

        public ConfigModelStringSourceLoadException(string message, string modelFile, string fileName, Exception innerException)
            : base(message, fileName, innerException)
        {
            ModelFile = modelFile;
        }
    }

    [Serializable]
    public class ConfigModelIncludeLoadException : ConfigModelLoadException
    {
        public string ModelFile { get; private set; }

        public ConfigModelIncludeLoadException(string message, string modelFile, string fileName, Exception innerException)
            : base(message, fileName, innerException)
        {
            ModelFile = modelFile;
        }
    }
}
