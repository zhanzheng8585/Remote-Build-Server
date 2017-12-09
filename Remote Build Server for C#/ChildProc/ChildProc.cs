///////////////////////////////////////////////////////////////////////////
//ChildProc - demonstrate creation of multiple .net processes            //
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
*   Every child process run as a small build server which controled by the mother 
*   Build process(SpawnProc.exe). When a child process is create, it will first send
*   a ready message to mother build process, then mother build process give build 
*   information to child process and let child process ask repo to get them. Build 
*   all *.csproj file into *.dll files, build from a given path.
* 
* 
*   Public Interface
*   ----------------
*   - Build()                               : Build all *.csproj file into *.dll files
*   - XMLParse(xmlname)                     : Parse xml file to get build information
*   - ready(portnum, childid)               : send ready message to MotherBuilder
*   - closechannel(portnum)                 : close the current child process
*/
/*
 * Required Files:
 * ---------------
 * - ChildProc.cs      : Create child process and build files
 * 
 * Maintenance History:
 * --------------------
 * ver 1.0 : 06 Dec 2017
 * - first release
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
using MockRepo;
using MessagePassingComm;
using Microsoft.Build.Execution;
using Microsoft.Build.BuildEngine;
using Microsoft.Build.Framework;
using System.Xml.Linq;
namespace ChildProc
{
    /*----< TestElement Class >--------------------------*/

    public class Test
    {
        public string testName { get; set; }
        public string author { get; set; }
        public DateTime timeStamp { get; set; }
        public List<string> testCode { get; set; }
        public void show()
        {
            Console.Write("\n  {0,-12} : {1}", "test name", testName);
            Console.Write("\n  {0,12} : {1}", "author", author);
            Console.Write("\n  {0,12} : {1}", "time stamp", timeStamp);
            foreach (string library in testCode)
            {
                Console.Write("\n  {0,12} : {1}", "library", library);
            }
        }
    }
    /*----< This class contain methods to parse XML file >--------------------------*/

    public class XmlTest
    {
        public XDocument doc_ { get; set; }
        public List<Test> testList_ { get; set; }
        public XmlTest()
        {
            doc_ = new XDocument();
            testList_ = new List<Test>();
        }
        public bool parse(System.IO.Stream xml)
        {
            doc_ = XDocument.Load(xml);
            if (doc_ == null)
                return false;
            string author = doc_.Descendants("author").First().Value;
            Test test = null;

            XElement[] xtests = doc_.Descendants("test").ToArray();
            int numTests = xtests.Count();

            for (int i = 0; i < numTests; ++i)
            {
                test = new Test();
                test.testCode = new List<string>();
                test.author = author;
                test.timeStamp = DateTime.Now;
                test.testName = xtests[i].Attribute("name").Value;
                IEnumerable<XElement> xtestCode = xtests[i].Elements("library");
                foreach (var xlibrary in xtestCode)
                {
                    test.testCode.Add(xlibrary.Value);
                }
                testList_.Add(test);
            }
            return true;
        }
    }
    /*----< ChildProc Process >--------------------------*/

    class ChildProc
    {
        public static string testpath { get; set; } = "../../../RepoStore/task1";
        public static string testersLocation { get; set; } = "../../../TestHarness/Testers";
        public static string Filepath { get; set; } = "";
        public static string XMLname { get; set; } = "";
        public static bool WantToTest { get; set; } = false;
        public static Comm Childcomm { get; set; }
        public static CommMessage msg2 { get; set; }
        public static CommMessage msg3 { get; set; } = new CommMessage(CommMessage.MessageType.reply);
        /*----< MSBuild function, need a build path >--------------------------*/

        public static void Build(string buildpath, string xmlname, int buildnum, string childnum, int portnum)
        {
            Console.Write("\n  start build");
            //get every *.csproj filepath
            string[] FilePath = Directory.GetFiles(buildpath, "*.csproj", SearchOption.AllDirectories);
            foreach (string File in FilePath){

                //show files which will be builded
                Console.WriteLine(File);
                ConsoleLogger logger = new ConsoleLogger();
                FileLogger Logger = new FileLogger();
                Logger.Parameters = @"logfile=../../../ServiceFileStore/" + "Child" + childnum + "buildlog"+ buildnum +".txt";
                //use MSbuild and print out logs on display
                try{
                    Dictionary<string, string> GlobalProperty = new Dictionary<string, string>();
                    string testLocation = testersLocation + "/test" + childnum + "-" + buildnum;
                    //for GlobalProperty setting such as outputpath
                    GlobalProperty.Add("Configuration", "Debug");GlobalProperty.Add("Platform", "Any CPU");
                    GlobalProperty.Add("OutputPath", testLocation);GlobalProperty.Add("OutputType", "Library");
                    BuildRequestData BuildRequest = new BuildRequestData(File, GlobalProperty, null, new string[] { "Rebuild" }, null);
                    BuildParameters bp = new BuildParameters();

                    //print out logs
                    bp.Loggers = new List<ILogger> { logger, Logger};

                    //report build result to the Console
                    BuildResult buildResult = BuildManager.DefaultBuildManager.Build(bp, BuildRequest);
                    if (buildResult.OverallResult == BuildResultCode.Success){
                        msg3.type = CommMessage.MessageType.reply;
                        msg3.from = "http://localhost:" + portnum + "/IPluggableComm";
                        msg3.to = "http://localhost:8079/IPluggableComm";
                        msg3.command = "buildresult";
                        msg3.status = "Build "+ xmlname + " successfully! Waitting ...";
                        Childcomm.postMessage(msg3);     //send build success to GUI      
                        Console.WriteLine("\n  Build successfully! Waitting ...");
                    }
                    else{
                        msg3.type = CommMessage.MessageType.reply;
                        msg3.from = "http://localhost:" + portnum + "/IPluggableComm";
                        msg3.to = "http://localhost:8079/IPluggableComm";
                        msg3.command = "buildresult";
                        msg3.status = "Some problems occur during building " + xmlname + " ...";
                        Childcomm.postMessage(msg3);     //send build fail to GUI
                        Console.WriteLine("\n  Some problems occur during building ...");
                    }
                }
                catch (Exception ex){ Console.Write("\n\n  {0}", ex.Message);}
            }
        }
        /*----< Parse XML file to get build information >--------------------------*/

        public static void XMLParse(string xmlname)
        {
            XMLname = xmlname;
            Childcomm.postFile(xmlname, "../../../RepoStore", Filepath);
            Thread.Sleep(1000);
            string XMLto = Path.Combine(Filepath, xmlname);
            XmlTest demo = new XmlTest();
            try
            {
                FileStream xml = new FileStream(XMLto, FileMode.Open);
                demo.parse(xml);
                foreach (Test test in demo.testList_)
                {
                    test.show();
                    //search all files In Repo by XML file and send
                    foreach (string library in test.testCode)
                    {
                        Childcomm.postFile(library, "../../../RepoStore", Filepath);
                    }
                }
                XDocument doc_ = new XDocument();
                XElement[] xtests = doc_.Descendants("test").ToArray();
            }
            catch (Exception ex)
            {
                Console.Write("\n\n  {0}", ex.Message);
            }
        }
        /*----< Set Console for Child Process >--------------------------*/

        static void setting()
        {
            Console.Title = "ChildProc";
            Console.BackgroundColor = ConsoleColor.White;
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.Write("\n      Child Process    ");
            Console.Write("\n ======================");
        }
        /*----< Send ready message to MotherBuilder >--------------------------*/

        public static void ready(int portnum, string childid)
        {
            msg2 = new CommMessage(CommMessage.MessageType.request);
            msg2.from = "http://localhost:" + portnum + "/IPluggableComm";
            msg2.to = "http://localhost:8080/IPluggableComm";
            msg2.status = "ready";
            msg2.info = childid;
            Childcomm.postMessage(msg2);
        }
        /*----< Close Channel >--------------------------*/

        public static void closechannel(int portnum)
        {
            msg2.type = CommMessage.MessageType.closeReceiver;
            msg2.from = "http://localhost:" + portnum + "/IPluggableComm";
            msg2.from = "http://localhost:" + portnum + "/IPluggableComm";
            Childcomm.postMessage(msg2);
            msg2.type = CommMessage.MessageType.closeSender;
            msg2.from = "http://localhost:" + portnum + "/IPluggableComm";
            msg2.from = "http://localhost:" + portnum + "/IPluggableComm";
            Childcomm.postMessage(msg2);
        }
        /*----< Start Child Process >--------------------------*/

        static void Main(string[] args)
        {
            string filepath = "../../../ServiceFileStore/task" + args[0];
            if (!File.Exists(filepath)) { Directory.CreateDirectory(filepath); }
            setting();
            //assign a channel for current child process
            int portnum = 8081 + Int32.Parse(args[0]); Childcomm = new Comm("http://localhost", portnum);
            Console.Write("\n  child process {0} is using channel {1}, path is {2}\n", Int32.Parse(args[0]), portnum, filepath);
            //send ready message to mother build process
            ready(portnum, args[0]);
            int buildnum = 0;
            //start childloop, continuing get message until closed
            while (true)
            {
                msg2 = Childcomm.getMessage(); msg2.show();    //repeat getting msg
                                                               // start build
                if (msg2.from == "http://localhost:8080/IPluggableComm" && msg2.arguments.Capacity != 0)
                {
                    buildnum = buildnum + 1;
                    Filepath = filepath + "/task" + buildnum;
                    if (!File.Exists(Filepath)) { Directory.CreateDirectory(Filepath); }
                    foreach (string xmlname in msg2.arguments) { XMLParse(xmlname); }
                    Build(Filepath, XMLname, buildnum, args[0], portnum);
                    string logname = "Child" + args[0] + "buildlog" + buildnum + ".txt";
                    Thread.Sleep(1000);
                    Childcomm.postFile(logname, "../../../ServiceFileStore", "../../../RepoStore/logfile");
                    ready(portnum, args[0]);

                    CommMessage msg = new CommMessage(CommMessage.MessageType.testrequest);
                    msg.from = "http://localhost:" + portnum + "/IPluggableComm";
                    msg.to = "http://localhost:8078/IPluggableComm";
                    string test = args[0] + "-" + buildnum.ToString(); msg.info = test;
                    Childcomm.postMessage(msg);
                }
                //if a msg is close type, close this child process
                else if (msg2.type == CommMessage.MessageType.close)
                {
                    closechannel(portnum);
                    break;
                }
            }
        }
    }
#if (TEST_CHILD)
    class testchild
    {
        static void Main(string[] args)
        {
            ChildProc.Build("../../../RepoStore/task1", "", 1, "1", 8082);
            string[] testPath = Directory.GetFiles("../../../RepoStore/task1", "*.dll", SearchOption.AllDirectories);
            Console.WriteLine("This is a test stub process, you have build *.csproj files into *.dll files in {0}", testpath1);
            foreach (string dllfile in testPath)
            {
                Console.WriteLine("{0}", dllfile);
            }
        }
    }
#endif
}