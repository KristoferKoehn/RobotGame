using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MMOTest.scripts.Managers
{
    public partial class MessageQueue : Node
    {

        private static MessageQueue instance = null;
        private Queue<JObject> queue;

        private MessageQueue()
        {
            queue = new Queue<JObject>();
        }

        public static MessageQueue GetInstance()
        {
            if (instance == null)
            {
                instance = new MessageQueue();
                GameLoop.Root.GetNode<MainLevel>("GameLoop/MainLevel").AddChild(instance);
                instance.Name = "MessageQueue";
            }
            return instance;
        }

        public void AddMessageToFront(JObject message)
        {
            Queue<JObject> tempQueue = new Queue<JObject>();
            tempQueue.Enqueue(message);
            while(queue.Count > 0)
            {
                tempQueue.Enqueue(queue.Dequeue());
            }
            queue = tempQueue;

        }
        
        public void AddMessage(JObject message)
        {
            //GD.Print("ADDED " +  message.ToString());
            queue.Enqueue(message);
            //queue.
        }

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        public void AddMessage(string message)
        {
            JObject job = JsonConvert.DeserializeObject<JObject>(message);
            queue.Enqueue(job);
        }




        public JObject PopMessage()
        {
            return queue.Dequeue();
        }
        
        public int Count()
        {
            return queue.Count;
        }

    }
}
