using System;
using System.Threading;
using Matrix.Backends;
using Matrix.Structures;
using Newtonsoft.Json;

namespace Matrix
{
    public partial class MatrixAPI
    {
        [MatrixSpec(EMatrixSpecApiVersion.R001, EMatrixSpecApi.ClientServer, "get-matrix-client-r0-sync")]
        public void ClientSync(bool ConnectionFailureTimeout = false){
            string url = "/_matrix/client/r0/sync?timeout="+SyncTimeout;
            if (!String.IsNullOrEmpty(syncToken)) {
                url += "&since=" + syncToken;
            }
            MatrixRequestError error = mbackend.Get (url,true, out var response);
            if (error.IsOk) {
                try {
                    MatrixSync sync = JsonConvert.DeserializeObject<MatrixSync> (response.ToString (), event_converter);
                    ProcessSync (sync);
                    IsConnected = true;
                } catch (Exception e) {
                    Console.WriteLine(e.InnerException);
                    throw new MatrixException ("Could not decode sync", e);
                }
            } else if (ConnectionFailureTimeout) {
                IsConnected = false;
                Console.Error.WriteLine ("Couldn't reach the matrix home server during a sync.");
                Console.Error.WriteLine(error.ToString());
                Thread.Sleep (BadSyncTimeout);
            }
            if (RunningInitialSync)
                RunningInitialSync = false;
        }
        
        public void StartSyncThreads(){
            if (poll_thread == null) {
                poll_thread = new Thread (pollThread_Run);
                poll_thread.Start ();
                shouldRun = true;
            } else {
                if (poll_thread.IsAlive) {
                    throw new Exception ("Can't start thread, already running");
                }
                poll_thread.Start ();
            }

        }

        public void StopSyncThreads(){
            shouldRun = false;
            poll_thread.Join ();
            FlushMessageQueue();
        }
    }
}