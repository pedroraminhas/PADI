﻿using System.Collections.Generic;

namespace PADIMapNoReduce {
    public interface IMapper {
        IList<KeyValuePair<string, string>> Map(string fileLine);
    }

    public interface IMapperTransfer {
        bool submit(string inputPath, int nSplits, string outputPath, string className, byte[] code);
    }

    public interface IWorker : IMapperTransfer {
        void notify(string workerURL);
        void doMapTask(List<int> splits, string workerURL, string inputPath, string outputPath, byte[] code, string className);
        void getStatus();
        void slowWorker(int seconds);
        void freezeWorker();
        void unfreezeWorker();
        void freezeJobTracker();
        void unfreezeJobTracker();
    }
    
    public interface IClient {
        string getSplitContent(int splitNumber, string inputPath);
        void sendResult(IList<KeyValuePair<string, string>> result, string outputPath);
    }

    public interface IPuppetMasters {
        void startWorker(string[] parameters);
    }
}
