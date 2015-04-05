using System.Collections.Generic;

namespace PADIMapNoReduce {
    public interface IMapper {
        IList<KeyValuePair<string, string>> Map(string fileLine);
    }

    public interface IMapperTransfer {
        bool Submit(string inputPath, int nSplits, string outputPath, string className, byte[] code);
    }

    public interface IWorker : IMapperTransfer { }

    public interface IReturnContent {
        string getContent(List<int> splits, string inputPath);
    }

    public interface IClient : IReturnContent { }
}
