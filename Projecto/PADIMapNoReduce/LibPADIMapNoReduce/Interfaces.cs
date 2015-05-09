using System.Collections.Generic;

namespace PADIMapNoReduce {
    public interface IMapper {
        IList<KeyValuePair<string, string>> Map(string fileLine);
    }

    public interface IMapperTransfer {
        void submit(string inputPath, string outputPath, int nSplits, string className, byte[] code, int port);
    }

    public interface IWorker : IMapperTransfer {
        void notify(string workerURL);
        void setURLs(string workerURL, string jobTrackerURL);
        void doMapTask(List<int> splits, string workerURL, string inputPath, string outputPath, byte[] code, string className, int nSplits, int clientPort);
        void printSystemStatus(bool toJobTrackers);
        void printJobsStatus();
        void slowWorker(int seconds);
        void freezeWorker();
        void unfreezeWorker();
        void notifyAvailability(string workerURL, string state);
        void freezeJobTracker();
        void notifyNewJobTracker(string jobTrackerURL);
        Dictionary<string, string> getWorkersStatus();
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
