///////////////////////////////////////////////////////////////////////////
// TestHarness.cs - Mock Tester for Federation Message-Passing Demo      //
//Author : Zheng Zhan                                                    //
//Language:    C#, Visual Studio 2017                                    //
//Application: Remote Build Server,      CSE681 - SMA  Project4          //
//SUID: 825530128                                                        //
//SU Email: zzhan03@syr.edu                                              //
//Source:Dr. Jim Fawcett                                                 //
///////////////////////////////////////////////////////////////////////////
/*
 *   Module Operations
 *   -----------------
 *   Runs tests by libraries sent from the Build Server. The Test Harness 
 *   load all *.dll files in "../../../TestHarness/Testers", and executes 
 *   tests, then notifies the author of the tests of the results.
 *   
 *   Public Interface
 *   ----------------
 *   loadAndExerciseTesters(testLocation, count)       : Load test libraries and run test
 *   
 *   
 *   NOTE:
 *   class DllLoaderExec contain all methods that you need to run test. But
 *   you need to change testersLocation by yourself. Besides, in line 94, 
 *   
 *   if (t.GetInterface("plus.ITest", true) != null)
 *   
 *   you need to replace your own namespace of your testdrives. Such as:
 *   
 *   if (t.GetInterface("namespace.Itest", true) != null)
 *   
 */
/*
 *   Build Process
 *   -------------
 *   - Required files:   TestHarness.cs, TestHarness.csporj
 * 
 *   Maintenance History
 *   -------------------
 *   ver 1.0 : 06 Dec 2017
 *     - first release
 * 
 */

using System;
using System.IO;
using System.Reflection;
using MessagePassingComm;
using System.Threading;
namespace MsgPassing
{
    public class DllLoaderExec
    {
        public static string testersLocation { get; set; } = @"../../../TestHarness/Testers";
        public static string testLocation { get; set; } = "";
        public static StreamWriter fs { get; set; }
        public static Comm Tcomm { get; set; }
        public static CommMessage msg { get; set; }
        public static bool testresult { get; set; }
        /*----< library binding error event handler >------------------*/
        /*
         *  This function is an event handler for binding errors when
         *  loading libraries.  These occur when a loaded library has
         *  dependent libraries that are not located in the directory
         *  where the Executable is running.
         */
        static Assembly LoadFromComponentLibFolder(object sender, ResolveEventArgs args)
        {
            Console.Write("\n  called binding error event handler");
            string folderPath = testLocation;
            string assemblyPath = Path.Combine(folderPath, new AssemblyName(args.Name).Name + ".dll");
            if (!File.Exists(assemblyPath)) return null;
            Assembly assembly = Assembly.LoadFrom(assemblyPath);
            return assembly;
        }
        //----< load assemblies from testersLocation and run their tests >-----

        public string loadAndExerciseTesters(string testLocation, int count)
        {
            AppDomain currentDomain = AppDomain.CurrentDomain;
            currentDomain.AssemblyResolve += new ResolveEventHandler(LoadFromComponentLibFolder);
            try
            {
                DllLoaderExec loader = new DllLoaderExec();

                // load each assembly found in testersLocation
                string[] files = Directory.GetFiles(testLocation, "*.dll");
                foreach (string file in files)
                {
                    string testname = testLocation + "/testlog" + count + ".txt"; ;
                    fs = new StreamWriter(@testname);

                    Assembly asm = Assembly.LoadFile(Path.GetFullPath(file));
                    string fileName = Path.GetFileName(file);
                    Console.WriteLine("\n  loaded {0}", fileName);
                    fs.WriteLine("\n  loaded {0}", fileName);
                    // exercise each tester found in assembly
                    
                    Type[] types = asm.GetTypes();
                    foreach (Type t in types)
                    {
                        // if type supports ITest interface then run test
                        if (t.GetInterface("plus.ITest", true) != null)
                            if (!loader.runSimulatedTest(t, asm))
                            {
                                Console.WriteLine("\n  test {0} failed to run", t.ToString());
                                fs.WriteLine("\n  test {0} failed to run", t.ToString());
                            }
                    }
                    fs.Flush();
                    fs.Close();
                }
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            return "Simulated Testing completed";
        }
        //
        //----< run tester t from assembly asm >-------------------------------

        bool runSimulatedTest(Type t, Assembly asm)
        {
            try
            {
                Console.WriteLine("\n  attempting to create instance of {0}", t.ToString());
                fs.WriteLine("\n  attempting to create instance of {0}", t.ToString());
                object obj = asm.CreateInstance(t.ToString());

                // announce test
                MethodInfo method = t.GetMethod("say");
                if (method != null)
                    method.Invoke(obj, new object[0]);

                // run test
                bool status = false;
                method = t.GetMethod("test");
                if (method != null)
                    status = (bool)method.Invoke(obj, new object[0]);

                Func<bool, string> act = (bool pass) =>
                {
                    if (pass)
                        return "passed";
                    return "failed";
                };
                testresult = status;
                Console.WriteLine("\n  test {0}", act(status));
                fs.WriteLine("\n  test {0}", act(status));
            }
            catch (Exception ex)
            {
                Console.WriteLine("\n  test failed with message \"{0}\"", ex.Message);
                fs.WriteLine("\n  test failed with message \"{0}\"", ex.Message);
                return false;
            }
            ///////////////////////////////////////////////////////////////////
            //  You would think that the code below should work, but it fails
            //  with invalidcast exception, even though the types are correct.
            //
            //    DllLoaderDemo.ITest tester = (DllLoaderDemo.ITest)obj;
            //    tester.say();
            //    tester.test();
            //
            //  This is a design feature of the .Net loader.  If code is loaded 
            //  from two different sources, then it is considered incompatible
            //  and typecasts fail, even thought types are Liskov substitutable.
            //
            return true;
        }
        //
        //----< extract name of current directory without its parents ---------

        string GuessTestersParentDir()
        {
            string dir = Directory.GetCurrentDirectory();
            int pos = dir.LastIndexOf(Path.DirectorySeparatorChar);
            string name = dir.Remove(0, pos + 1).ToLower();
            if (name == "debug")
                return "../..";
            else
                return ".";
        }
        //----< run demonstration >--------------------------------------------

        static void Main(string[] args){
            //start test and remind users in console
            Console.Write("\n\n ==================================\n");
            Console.Write("             Test Start\n");
            if (!File.Exists(testersLocation)) { Directory.CreateDirectory(testersLocation); }
            Tcomm = new Comm("http://localhost", 8078);
            msg = new CommMessage(CommMessage.MessageType.connect);
            msg.from = "http://localhost:8081/IPluggableComm";
            msg.to = "http://localhost:8080/IPluggableComm";    //mother build process
            Tcomm.postMessage(msg);
            int count = 0;
            while (true){
                msg = Tcomm.getMessage(); msg.show();    //repeat getting msg
                if (msg.from != "http://localhost:8079/IPluggableComm"){
                    count = count + 1;
                    //load test libraries and report test result to Console
                    testLocation = testersLocation + "/test" + msg.info;
                    if (!File.Exists(testLocation)) { Directory.CreateDirectory(testLocation); }
                    DllLoaderExec loader = new DllLoaderExec();
                    string result = loader.loadAndExerciseTesters(testLocation, count);

                    //send testresult back to GUI
                    if (testresult == false){
                        msg.type = CommMessage.MessageType.reply;
                        msg.from = "http://localhost:8078/IPluggableComm";
                        msg.to = "http://localhost:8079/IPluggableComm";
                        msg.command = "testresult";
                        msg.status = "test" + msg.info + "failed";
                        Tcomm.postMessage(msg);
                    }
                    else{
                        msg.type = CommMessage.MessageType.reply;
                        msg.from = "http://localhost:8078/IPluggableComm";
                        msg.to = "http://localhost:8079/IPluggableComm";
                        msg.command = "testresult";
                        msg.status = "test" + msg.info + "passed";
                        Tcomm.postMessage(msg);
                    }
                    string logname = "testlog" + count + ".txt";
                    Thread.Sleep(1000);
                    Tcomm.postFile(logname, testLocation, "../../../RepoStore/logfile");
                    testresult = new bool();
                }
            }
        }
    }
#if (TEST_TESTHARNESS)

    ///////////////////////////////////////////////////////////////////
    // TestTestHarness class

    class TestTestHarness
    {
        static void Main(string[] args)
        {
            Console.Write("\n  Demonstration of TestHarness");
            Console.Write("\n ============================");

            //load test libraries and report test result to Console
            DllLoaderExec loader = new DllLoaderExec();
            string result = loader.loadAndExerciseTesters("../../../TestHarness/Testers", 1);

            Console.Write("class DllLoaderExec include all methods run test, you can use it to test all *.dll files in '../../../TestHarness/Testers'");
        }
    }
#endif
}
