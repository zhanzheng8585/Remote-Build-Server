///////////////////////////////////////////////////////////////////////////
//Repo.cs - Mock Repository for Federation Message-Passing               //
//Author : Zheng Zhan                                                    //
//Language:    C#, Visual Studio 2017                                    //
//Application: Core Build Server, CSE681 - SMA                           //
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
 *   Holds all code and documents for the current baseline, along with their 
 *   dependency relationships. Use Windows Communication Foundation (WCF) to send 
 *   files. Can search all the files that needed by client and send them to Build 
 *   Server's directory such as "../../../ServiceFileStore/task1". Repo also can
 *   get FileList of itself and send them by msg to GUI with a click on GUI.
 * 
 *   Public Interface
 *   ----------------
 *   - NavigatorServer()          : it's use for getting remote FileList
 *   - closechannel()             : close repo channel
 */
/*
 *   Required Files:
 *   ---------------
 *   - Repo.cs        : Create child process and build files
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
using System.IO;
using System.Diagnostics;
using System.Xml.Linq;
using System.ServiceModel;
using System.Threading;
using MessagePassingComm;
using Navigator;

namespace MockRepo
{
    using Argument = String;
    public class Repo
    {
        static IFileMgr localFileMgr { get; set; } = null;
        static Dictionary<string, Func<CommMessage, CommMessage>> messageDispatcher =
          new Dictionary<string, Func<CommMessage, CommMessage>>();
        private static IFileMgr fileMgr { get; set; } = null;  // note: Navigator just uses interface declarations
        public static string ServicePath { get; set; } = "../../../ServiceFileStore";
        public static bool WantToTest { get; set; } = false;
        public static string testpath1 { get; set; } = "../../../ServiceFileStore/task1";
        public static string RepoPath { get; set; } = "../../../RepoStore";
        public static List<string> testfiles { get; set; } = new List<string>();
        public static CommMessage msg { get; set; }
        public static Comm Rcomm { get; set; }
        /*----< initialize server processing >-------------------------*/

        public static void NavigatorServer()
        {
            initializeEnvironment();
            localFileMgr = FileMgrFactory.create(FileMgrType.Local);
        }
        /*----< set Environment properties needed by server >----------*/

        static void initializeEnvironment()
        {
            Navigator.Environment.root = Navigator.ClientEnvironment.root;
            Navigator.Environment.address = Navigator.ClientEnvironment.address;
            Navigator.Environment.port = Navigator.ClientEnvironment.port;
            Navigator.Environment.endPoint = Navigator.ClientEnvironment.endPoint;
            Navigator.Environment.logdir = Navigator.ClientEnvironment.logdir;
        }
        /*----< Dispatcher >----------*/

        static void initializeDispatcher()
        {
            //get FileList from ../../../RepoStore
            Func<CommMessage, CommMessage> getTopFiles = (CommMessage msg) =>{
                localFileMgr.currentPath = "";
                CommMessage reply = new CommMessage(CommMessage.MessageType.showfiles);
                reply.to = msg.from;
                reply.from = msg.to;
                reply.command = "getTopFiles";
                reply.arguments = localFileMgr.getFiles().ToList<string>();
                return reply;
            };
            messageDispatcher["getTopFiles"] = getTopFiles;

            //get FileList from ../../../RepoStore/logfile
            Func<CommMessage, CommMessage> getlogfiles = (CommMessage msg) => {
                localFileMgr.currentPath = "";
                CommMessage reply = new CommMessage(CommMessage.MessageType.showfiles);
                reply.to = msg.from;
                reply.from = msg.to;
                reply.command = "getlogfiles";
                reply.arguments = localFileMgr.getlogfiles().ToList<string>();
                return reply;
            };
            messageDispatcher["getlogfiles"] = getlogfiles;
        }
        /*----< Close Channel >--------------------------*/

        public static void closechannel()
        {
            msg.type = CommMessage.MessageType.closeReceiver; //close receiver
            msg.from = "http://localhost:8081/IPluggableComm";
            msg.to = "http://localhost:8081/IPluggableComm";
            Rcomm.postMessage(msg);
            msg.type = CommMessage.MessageType.closeSender;   //close sender
            msg.from = "http://localhost:8081/IPluggableComm";
            msg.to = "http://localhost:8081/IPluggableComm";
            Rcomm.postMessage(msg);
        }
        /*----< Set Console for Repo >--------------------------*/

        static void setting()
        {
            Console.Title = "Mock Repository";
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.Write("\n      Repo Process    ");
            Console.Write("\n =====================");
        }
        /*----< Start Repo Process >--------------------------*/

        static void Main(string[] args){
            NavigatorServer(); initializeDispatcher();
            if (!File.Exists(ServicePath)) { Directory.CreateDirectory(ServicePath); }
            setting(); Rcomm = new Comm("http://localhost", 8081);
            msg = new CommMessage(CommMessage.MessageType.connect);
            msg.from = "http://localhost:8081/IPluggableComm";
            msg.to = "http://localhost:8080/IPluggableComm";    //mother build process
            Rcomm.postMessage(msg);

            //start repoloop
            while (true){
                msg = Rcomm.getMessage(); msg.show();    //repeat getting msg

                //If it's a Build Request msg from GUI, send it to SpawnProc
                if (msg.from == "http://localhost:8079/IPluggableComm" && msg.type != CommMessage.MessageType.showfiles && msg.type != CommMessage.MessageType.close)
                {
                    //send xmlfiles' name to mother build process
                    foreach (string xmlname in msg.arguments)
                    {
                        CommMessage msg2 = new CommMessage(CommMessage.MessageType.buildrequest);
                        msg2.from = "http://localhost:8081/IPluggableComm";
                        msg2.to = "http://localhost:8080/IPluggableComm";
                        msg2.arguments.Add(xmlname); msg2.show();
                        Rcomm.postMessage(msg2);
                    }
                }
                //if it's a close msg, close repo process
                else if (msg.type == CommMessage.MessageType.close){
                    closechannel();
                    break;
                }
                //get FileList and send to GUI, using Dispatcher
                else if (msg.type == CommMessage.MessageType.showfiles)
                {
                    CommMessage reply = messageDispatcher[msg.command](msg);
                    msg.from = "http://localhost:8081/IPluggableComm";
                    msg.to = "http://localhost:8081/IPluggableComm";
                    reply.show(); Rcomm.postMessage(reply);
                }
            }
        }
    }
#if (TEST_REPO)
    class testrepo
    {
        static void Main(string[] args)
        {
            Repo.NavigatorServer();
            Repo.closechannel();
        }
    }
#endif
}