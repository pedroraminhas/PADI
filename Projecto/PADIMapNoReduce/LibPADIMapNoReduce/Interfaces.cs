using System.Collections.Generic;
using System.Threading;

namespace PADIMapNoReduce {
    public interface IMapper {
        IList<KeyValuePair<string, string>> Map(string fileLine);
    }

    public interface IMapperTransfer {
        bool submit(string inputPath, int nSplits, string outputPath, string className, byte[] code);
    }

    public interface IWorker : IMapperTransfer {
        void notify(string workerURL);
        void doMapTask(List<int> splits, string workerURL, string inputPath, string outputPath, byte[] code, string className, int nSplits);
        void getStatus();
        void slowWorker(int milisseconds, int workerID);
        Thread[] getMapThreads();
        void freezeWorker();
        void unfreezeWorker();
        void freezeJobTracker();
        void unfreezeJobTracker();
    }
    
    public interface IClient {
        string getSplitContent(int splitNumber, string inputPath, int nSplits);
        void sendResult(IList<KeyValuePair<string, string>> result, string outputPath, string splitIdentifier);
    }

    public interface IPuppetMasters {
        void startWorker(string[] parameters);
    }
}
