/////////////////////////////////////////////////////////////////////
// IMPCommService.cs - service interface for MessagePassingComm    //
// ver 2.1                                                         //
// Jim Fawcett, CSE681-OnLine, Summer 2017                         //
/////////////////////////////////////////////////////////////////////
/*
 * Added references to:
 * - System.ServiceModel
 * - System.Runtime.Serialization
 */
/*
 * This package provides:
 * ----------------------
 * - ClientEnvironment   : client-side path and address
 * - ServiceEnvironment  : server-side path and address
 * - IMessagePassingComm : interface used for message passing and file transfer
 * - CommMessage         : class representing serializable messages
 * 
 * Required Files:
 * ---------------
 * - IPCommService.cs         : Service interface and Message definition
 * 
 * Maintenance History:
 * --------------------
 * ver 2.0 : 19 Oct 2017
 * - renamed namespace and ClientEnvironment
 * - added verbose property to ClientEnvironment
 * ver 1.0 : 15 Jun 2017
 * - first release
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ServiceModel;
using System.Runtime.Serialization;
using System.Threading;

namespace MessagePassingComm
{
  using Command = String;             // Command is key for message dispatching, e.g., Dictionary<Command, Func<bool>>
  using EndPoint = String;            // string is (ip address or machine name):(port number)
  using Argument = String;      
  using ErrorMessage = String;

    public struct ClientEnvironment
    {
        public static string fileStorage { get; set; } = "../../../RepoStore";
        public static long blockSize { get; set; } = 1024;
        public static string baseAddress { get; set; }
        public static bool verbose { get; set; }
    }

    public struct ServiceEnvironment
    {
        public static string fileStorage { get; set; } = "../../../ServiceFileStore";
        public static string baseAddress { get; set; }
    }

    [ServiceContract(Namespace = "MessagePassingComm")]
    public interface IMessagePassingComm
    {
        /*----< support for message passing >--------------------------*/

        [OperationContract(IsOneWay = true)]
        void postMessage(CommMessage msg);

        // private to receiver so not an OperationContract
        CommMessage getMessage();

        /*----< support for sending file in blocks >-------------------*/

        [OperationContract]
        bool openFileForWrite(string name, string filefrom, string fileto);

        [OperationContract]
        bool writeFileBlock(byte[] block);

        [OperationContract(IsOneWay = true)]
        void closeFile();
    }

    [DataContract]
    public class CommMessage
    {
        public enum MessageType
        {
            [EnumMember]
            connect,           // initial message sent on successfully connecting
            [EnumMember]
            request,           // request for action from receiver
            [EnumMember]
            buildrequest,           // request for action from receiver
            [EnumMember]
            testrequest,           // request for action from receiver
            [EnumMember]
            reply,             // response to a request
            [EnumMember]
            showfiles,             // response to a request
            [EnumMember]
            closeSender,       // close down client
            [EnumMember]
            closeReceiver,     // close down server for graceful termination
            [EnumMember]
            close              // close down server for graceful termination
        }

        /*----< constructor requires message type >--------------------*/

        public CommMessage(MessageType mt)
        {
            type = mt;
        }
        /*----< data members - all serializable public properties >----*/

        [DataMember]
        public MessageType type { get; set; } = MessageType.connect;

        [DataMember]
        public string to { get; set; }

        [DataMember]
        public string from { get; set; }

        [DataMember]
        public string author { get; set; }

        [DataMember]
        public string status { get; set; }

        [DataMember]
        public Command command { get; set; }

        [DataMember]
        public string info { get; set; }

        [DataMember]
        public List<Argument> arguments { get; set; } = new List<Argument>();

        [DataMember]
        public int threadId { get; set; } = Thread.CurrentThread.ManagedThreadId;

        [DataMember]
        public ErrorMessage errorMsg { get; set; } = "no error";

        public void show()
        {
            Console.Write("\n  CommMessage:");
            Console.Write("\n    MessageType : {0}", type.ToString());
            Console.Write("\n    to          : {0}", to);
            Console.Write("\n    from        : {0}", from);
            Console.Write("\n    author      : {0}", author);
            Console.Write("\n    command     : {0}", command);
            Console.Write("\n    info        : {0}", info);
            Console.Write("\n    status      : {0}", status);
            Console.Write("\n    arguments   :");
            if (arguments.Count > 0)
                Console.Write("\n      ");
            foreach (string arg in arguments)
                Console.Write("{0} ", arg);
            Console.Write("\n    ThreadId    : {0}", threadId);
            Console.Write("\n    errorMsg    : {0}\n", errorMsg);
        }
        public CommMessage clone()
        {
            CommMessage msg = new CommMessage(MessageType.request);
            msg.type = type;
            msg.to = to;
            msg.from = from;
            msg.author = author;
            msg.command = command;
            foreach (string arg in arguments)
                msg.arguments.Add(arg);
            return msg;
        }
    }
}
