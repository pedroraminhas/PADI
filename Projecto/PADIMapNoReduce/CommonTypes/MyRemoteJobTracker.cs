using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace PADIMapNoReduce
{
    interface IMap {
       KeyValuePair<String, String> Map(String key, String value);
    }

    class MapImpl : IMap {
        KeyValuePair<String, String> IMap.Map(String key, String value) {
            KeyValuePair<String, String> pair = new KeyValuePair<String, String>("ola", "1");
            return pair;
        }
    }

	public class MyRemoteJobTracker : MarshalByRefObject  {
        int nWorkers = 1;
        public struct Limits
        {
            public int inferiorLimit;
            public int superiorLimit;
        }
        public void Submit(String inputFilePath, int nSplits, String outputPath) {
         //   int nLinesPerWorker;
         //   ArrayList workersLimits = new ArrayList();
         //   if (nSplits % nWorkers == 0){
          //      nLinesPerWorker = nSplits / nWorkers;
           //     Limits limits = new Limits();
            //    limits.inferiorLimit=
          //  }
        //    else

      //  }
    }
}