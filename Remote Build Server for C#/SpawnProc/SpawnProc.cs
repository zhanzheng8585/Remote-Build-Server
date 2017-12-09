///////////////////////////////////////////////////////////////////////////
//SpawnProc - demonstrate creation of multiple .net processes            //
//Author : Zheng Zhan                                                    //
//Language:    C#, Visual Studio 2017                                    //
//Application: Remote Build Server,      CSE681 - SMA  Project4          //
//SUID: 825530128                                                        //
//SU Email: zzhan03@syr.edu                                              //
//Source:Dr. Jim Fawcett                                                 //
///////////////////////////////////////////////////////////////////////////
/*
 * Added references to:
 * - System.ServiceModel
 * - System.Runtime.Serialization
 */
/*
 *   Module Operations
 *   -----------------
 *   A limited set of processes spawned at startup. The build server provides a 
 *   queue of build requests, and each pooled process retrieves a request, processes 
 *   it, sends the build log and, if successful, libraries to the test harness, 
 *   then retrieves another request.It has two Queue for manage msg from repo and
 *   child process, only if both two Queue are not empty, then deQ.
 *   
 *   Public Interface
 *   ----------------
 *   - createProcess(int i)                    : Create i number process
 *   - closechannel()                          : close the MotherBuilder process
 *   
 *   
 *   Required Files:
 *   ---------------
 *   - SpawnProc.cs            : mothre build process
 *
 *   Maintenance History
 *   -------------------
 *   ver 1.0 : 06 Dec 2017
 *     - first release
 * 
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.ServiceModel;
using System.Threading;
using MessagePassingComm;

namespace SpawnProc
{
    class SpawnProc
    {
        public static SWTools.BlockingQueue<CommMessage> BuildReQ { get; set; } = new SWTools.BlockingQueue<CommMessage>();
        public static SWTools.BlockingQueue<string> readyQ { get; set; } = new SWTools.BlockingQueue<string>();
        public static bool WantToTest { get; set; } = false;
        public static CommMessage prcvmsg { get; set; }
        public static Comm Pcomm { get; set; }
        /*----< Creates a specified number of processes on command >--------------------------*/

        public static bool createProcess(int i)
        {
            Process proc = new Process();
            string fileName = "..\\..\\..\\ChildProc\\bin\\debug\\ChildProc.exe";
            string absFileSpec = Path.GetFullPath(fileName);
            Console.Write("\n  attempting to start {0}", absFileSpec);

            //record the number id of this child process
            string commandline = i.ToString();
            try
            {
                Process.Start(fileName, commandline);
            }
            catch (Exception ex)
            {
                Console.Write("\n  {0}", ex.Message);
                return false;
            }
            return true;
        }
        /*----< Set Console for Mother Process >--------------------------*/

        static void setting()
        {
            Console.Title = "SpawnProc";
            Console.Write("\n     Parent Process    ");
            Console.Write("\n ======================");
            Console.WriteLine("\n Waitting msg from child. \n");
            Console.WriteLine(" Requirement 1 achieved, This proj use C#, the .Net Frameowrk, and Visual Studio 2017. \n");
            Console.WriteLine(" Requirement 2 achieved, This proj include a Message-Passing Communication Service built with WCF. \n");
            Console.WriteLine(" Requirement 3 achieved, The Communication Service support accessing build requests by Pool Processes from the mother Builder process, sending and receiving build requests, and sending and receiving files. \n");
            Console.WriteLine(" Requirement 4 achieved, This proj provide a Repository server that supports client browsing to find files to build, builds an XML build request string and sends that and the cited files to the Build Server. \n");
            Console.WriteLine(" Requirement 5 achieved, This proj provide a Process Pool component that creates a specified number of processes on command. \n");
            Console.WriteLine(" Requirement 6 achieved, Pool Processes use Communication prototype to access messages from the mother Builder process. You may simply have them write the message contents to their consoles, demonstrating that they continue to access messages from the shared mother's queue, until shut down. \n");
            Console.WriteLine(" Requirement 7 achieved, Each Pool Process attempt to build each library, cited in a retrieved build request, logging warnings and errors. \n");
            Console.WriteLine(" Requirement 8 achieved, If the build succeeds, it will send a test request and libraries to the Test Harness for execution, and send the build log to the repository. \n");
            Console.WriteLine(" Requirement 9 achieved, The Test Harness attempt to load each test library it receives and execute it. It submits the results of testing to the Repository. \n");
            Console.WriteLine(" Requirement 10 achieved, This proj include a Graphical User Interface, built using WPF. \n");
            Console.WriteLine(" Requirement 11 achieved, The GUI client is a separate process, implemented with WPF and using message-passing communication. It provides 'show files' button to get file lists from the Repository, and select files for packaging into a test library. It provides 'Add library' button to add other test libraries to the build request structure. \n");
            Console.WriteLine(" Requirement 12 achieved, The client send build request structures to the repository for storage and transmission to the Build Server. \n");
            Console.WriteLine(" Requirement 13 achieved, The client shall be able to request the repository to send a build request in its storage to the Build Server for build processing. \n");
        }
        /*----< Close Channel >--------------------------*/

        public static void closechannel()
        {
            prcvmsg.type = CommMessage.MessageType.closeReceiver; //close receiver
            prcvmsg.from = "http://localhost:8080/IPluggableComm";
            Pcomm.postMessage(prcvmsg);
            prcvmsg.type = CommMessage.MessageType.closeSender;   //close sender
            Pcomm.postMessage(prcvmsg);
        }
        /*----< Start Mother Build Process >--------------------------*/

        static void Main(string[] args)
        {
            setting(); Pcomm = new Comm("http://localhost", 8080); int count = 0;
            prcvmsg = new CommMessage(CommMessage.MessageType.connect);

            //start mother build process loop
            while (true)
            {
                prcvmsg = Pcomm.getMessage(); prcvmsg.show(); // repeat get msg
                //if get ready msg from child process, enQ
                if (prcvmsg.status == "ready" && prcvmsg.from != "http://localhost:8081/IPluggableComm")
                {
                    readyQ.enQ(prcvmsg.info); Console.WriteLine("\n Ask child process {0} to retrive files. Waitting for next step.", prcvmsg.info);
                }
                //if get buildreq msg from repo, enQ
                if (prcvmsg.from == "http://localhost:8081/IPluggableComm" && prcvmsg.arguments.Capacity != 0)
                {
                    BuildReQ.enQ(prcvmsg); Console.WriteLine("\n Repo is ready {0}.");
                }
                //if msg is a close msg, close the mother build process
                if (prcvmsg.type == CommMessage.MessageType.close)
                {
                    closechannel(); break;
                }
                //if readyQueue and BuildRequestQueue both are not null, deQ for build
                if (readyQ.size() != 0 && BuildReQ.size() != 0)
                {
                    string childid = readyQ.deQ(); int childnum = 1 + Int32.Parse(childid);
                    CommMessage prcvmsg2 = new CommMessage(CommMessage.MessageType.buildrequest);
                    prcvmsg2 = BuildReQ.deQ();                            //get msg from GUI
                    prcvmsg2.info = childnum.ToString();
                    prcvmsg2.type = CommMessage.MessageType.buildrequest;
                    prcvmsg2.from = "http://localhost:8080/IPluggableComm";
                    prcvmsg2.to = "http://localhost:808" + childnum + "/IPluggableComm";
                    Pcomm.postMessage(prcvmsg2); prcvmsg2.show();
                }
                //create a specified number of child process
                if (prcvmsg.from == "http://localhost:8079/IPluggableComm" && prcvmsg.type != CommMessage.MessageType.close)
                {
                    count = Int32.Parse(prcvmsg.info);
                    for (int i = 1; i <= count; i++)
                    {
                        if (createProcess(i)) Console.Write(" - succeeded");
                        else Console.Write(" - failed");
                    }
                }
            }
        }
    }
#if (TEST_MOTHER)
    class testmother
    {
        static void Main(string[] args)
        {
            SpawnProc.createProcess(5);
            Console.WriteLine("This is a test process, you have create 5 child process");
            SpawnProc.closechannel();
        }
    }
#endif
}