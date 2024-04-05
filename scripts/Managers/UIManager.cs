using Godot;
using System.Collections.Generic;

namespace MMOTest.scripts.Managers
{

    public partial class UIManager : Node
    {
        private PackedScene NotificationLabelPackedScene = GD.Load<PackedScene>("res://scenes/utility/NotificationLabel.tscn");
        private static UIManager instance = null;
        private Dictionary<int, PlayerUI> UIs = new Dictionary<int, PlayerUI>();
        private List<int> ActorList = new List<int>();

        private UIManager() { }

        public static UIManager GetInstance()
        {
            if (instance == null)
            {
                instance = new UIManager();
                GameLoop.Root.GetNode<MainLevel>("GameLoop/MainLevel").AddChild(instance);
                instance.Name = "UIManager";
            }

            return instance;
        }

        public void RegisterUI(int ActorID, PlayerUI pUI)
        {
            UIs[ActorID] = pUI;
        }

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        public void RegisterActor(int ActorID)
        {
            ActorList.Add(ActorID);
        }

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        public void PushNotification(int ActorID, string Notification)
        {
            NotificationLabel nl = NotificationLabelPackedScene.Instantiate<NotificationLabel>();
            nl.Text = Notification;
            nl.GlobalPosition = UIs[ActorID].GetViewport().GetVisibleRect().Size * 0.7f;
            UIs[ActorID].AddChild(nl);
        }

        public void NotifyAll(string Notification)
        {
            foreach(int aID in ActorList)
            {
                long pID = ActorManager.GetInstance().GetActor(aID).ActorMultiplayerAuthority;
                RpcId(pID, "PushNotification", aID, Notification);
            }
        }

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        public void UnregisterActor(int ActorID)
        {
            ActorList.Remove(ActorID);
        }
    }

}
