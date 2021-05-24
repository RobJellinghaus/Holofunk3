/// Copyright by Rob Jellinghaus.  All rights reserved.

using System;
using System.Threading;

namespace Holofunk.Core
{
    /// <summary>
    /// Methods which allow asserting that certain threads are active.
    /// </summary>
    /// <remarks>
    /// Used to both document and verify that correct threading policies are being followed.
    /// </remarks>
    public static class ThreadContract
    {
        /// <summary>
        /// The largest number of threads we expect in any thread pool.
        /// </summary>
        private const int s_numThreads = 5;

        /// <summary>
        /// Managed thread Ids of all app thread(s).
        /// </summary>
        private static int[] s_appThreadIds = new int[s_numThreads];

        /// <summary>
        /// Managed thread Ids of all Unity thread(s).
        /// </summary>
        private static int[] s_unityThreadIds = new int[s_numThreads];

        static ThreadContract()
        {
            for (int i = 0; i < s_numThreads; i++)
            {
                s_appThreadIds[i] = s_unityThreadIds[i] = int.MinValue;
            }
        }

        private static void CheckThread(int[] expectedPool, int[] unexpectedPool)
        {
            int currentId = Thread.CurrentThread.ManagedThreadId;
            for (int i = 0; i < expectedPool.Length; i++)
            {
                if (expectedPool[i] == currentId)
                {
                    // we're good
                    return;
                }
            }

            for (int i = 0; i < unexpectedPool.Length; i++)
            {
                if (unexpectedPool[i] == currentId)
                {
                    Contract.Fail("Found thread in wrong pool; thread contract violated");
                }
            }

            // add it to expectedPool
            for (int i = 0; i < expectedPool.Length; i++)
            {
                if (expectedPool[i] == int.MinValue)
                {
                    expectedPool[i] = currentId;
                    return;
                }
            }

            // If any pools do dynamic thread creation, we'll sure find out right here, because no value of s_numThreads will be enough.
            Contract.Fail("Too many threads in expected pool; increase ThreadContract.s_numThreads");
        }

        public static void RequireApp()
        {
            CheckThread(s_appThreadIds, s_unityThreadIds);
        }

        public static void RequireUnity()
        {
            CheckThread(s_unityThreadIds, s_appThreadIds);
        }
    }
}
