using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace PADIMapNoReduce
{
    interface IMap
    {
        KeyValuePair<String, String> Map(String key, String value);
    }

    class MapImpl : IMap
    {
        KeyValuePair<String, String> IMap.Map(String key, String value)
        {
            KeyValuePair<String, String> pair = new KeyValuePair<String, String>("ola", "1");
            return pair;
        }
    }

    public struct Limits
    {
        public int lowerLimit;
        public int upperLimit;
    }

    public class MyRemoteJobTracker : MarshalByRefObject
    {
        int nWorkers = 2;

        private int[] DistributeEqualy(int nSplits, int[] nLinesPerWorker)
        {
            for (int i = 0; i < nWorkers; i++)
                nLinesPerWorker[i] = nSplits / nWorkers;
            return nLinesPerWorker;
        }


        public void Submit(String inputFilePath, int nSplits, String outputPath)
        {
            int[] nSplitsPerWorker = new int[nWorkers];
            DistributeEqualy(nSplits, nSplitsPerWorker);

            if (nSplits % nWorkers != 0)
            {
                int nLinesToDistribute = nSplits % nWorkers;
                for (int i = 0; i < nLinesToDistribute; i++)
                    nSplitsPerWorker[i]++;
            }

            Limits[] workersLimits = new Limits[nWorkers];
            int startingSplit = 0;
            for (int i = 0; i < nWorkers; i++)
            {
                workersLimits[i].lowerLimit = startingSplit;
                workersLimits[i].upperLimit = startingSplit + nSplitsPerWorker[i] - 1;
                startingSplit = workersLimits[i].upperLimit + 1;
            }
        }
    }
}