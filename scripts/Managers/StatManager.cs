using Godot;
using MMOTest.Backend;
using MMOTest.scripts.Managers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;


public partial class StatManager : Node
{
    public static StatManager instance;
    public Dictionary<long, List<int>> StatSubscription = new Dictionary<long, List<int>>();
    private Dictionary<int, Dictionary<StatType, float>> StatChanges = new Dictionary<int, Dictionary<StatType, float>>();
    private StatManager() { }

    public static StatManager GetInstance() 
    {
        if (instance == null)
        {
            instance = new StatManager();
            GameLoop.Root.GetNode<MainLevel>("GameLoop/MainLevel").AddChild(instance);
            instance.Name = "StatManager";
        }
        return instance; 
    }

    /// <summary>
    /// applies stat change and caches all stat changes to be sent to clients on SendCachedStatData function call at the end of message queue processing
    /// </summary>
    /// <param name="deltas"></param>
    /// <param name="targetID"></param>
    public void ApplyStatChange(List<StatProperty> deltas, int targetID)
    {
        //applying stats
        Dictionary<int, Dictionary<StatType, float>> singleChange = new Dictionary<int, Dictionary<StatType, float>>();
        singleChange[targetID] = new Dictionary<StatType, float>();
        foreach( StatProperty sp in deltas )
        {
            singleChange[targetID][sp.StatType] = sp.Value;
        }

        ApplyAllStatChanges(singleChange);
        //end applying stats

        //check if health < 0, notify DeathManager
        StatBlock sb = GetStatBlock(targetID);
        if (sb.GetStat(StatType.HEALTH) <= 0f)
        {
            if (!DeathManager.GetInstance().IsActorDead(targetID))
            {
                JObject job = new JObject
                {
                    { "type", "death"},
                    { "target", targetID},
                    { "source", ""}
                };

                MessageQueue.GetInstance().AddMessageToFront(job);
            }
            //we can't do anything for him.
        }

        //caching stats
        if (!StatChanges.ContainsKey(targetID))
        {
            Dictionary<StatType, float> statDeltas = new Dictionary<StatType, float>();
            StatChanges[targetID] = statDeltas;
        }

        foreach (StatProperty stat in deltas)
        {
            if (StatChanges[targetID].ContainsKey(stat.StatType))
            {
                StatChanges[targetID][stat.StatType] += stat.Value;
            }
            else
            {
                StatChanges[targetID][stat.StatType] = stat.Value;
            }
        }
        //end caching stats
    }

    public void SubscribePeerToActor(long PeerID, int ActorID)
    {
        if (StatSubscription.ContainsKey(PeerID))
        {
            if (!StatSubscription[PeerID].Contains(ActorID))
            {
                StatSubscription[PeerID].Add(ActorID);
            }
        } else
        {
            StatSubscription[PeerID] = new List<int>
            {
                ActorID
            };
        }
    }

    public void UnsubscribePeerToActor(int PeerID, int ActorID)
    {
        if (StatSubscription.ContainsKey(PeerID))
        {
            StatSubscription[PeerID].Remove(ActorID);
        }
    }

    public void UnsubscribeActorFromAllPeers(int ActorID)
    {
        foreach(List<int> lists in StatSubscription.Values)
        {
            lists.Remove(ActorID);
        }
    }

    public void PeerLogoutSubscriptionDisconnect(long PeerID, int ActorID)
    {
        StatSubscription.Remove(PeerID);
        UnsubscribeActorFromAllPeers(ActorID);
    }

    public StatBlock GetStatBlock(int ActorID)
    {
        return ActorManager.GetInstance().GetActor(ActorID).stats;
    }

    /// <summary>
    /// this is called from the client and executed on the server. 
    /// This function calls AssignStatBlock as an RPC from the server to the client with the stat json and the peer that's associated with it.
    /// See AssignStatBlock
    /// </summary>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void RequestStatBlock(int ActorID) 
    {
        long peerID = Multiplayer.GetRemoteSenderId();
        //call next RPC that sends the stat block to the user
        RpcId(peerID, "AssignStatBlock", GetStatBlock(ActorID).SerializeStatBlock(), ActorID);
    }

    /// <summary>
    /// Assigns a statblock to a client-side actor. This can be the client-player or another actor in a client's simulation.
    /// json must be a serialized Dictionary<StatType, float>
    /// </summary>
    /// <param name="jstr">must be a serialized Dictionary<StatType, float></param>
    /// <param name="peerID">PeerID of the requested actor.</param>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void AssignStatBlock(string jstr, int ActorID)
    {
        Dictionary<StatType, float> sb = JsonConvert.DeserializeObject< Dictionary<StatType, float>>(jstr);
        ActorManager.GetInstance().GetActor(ActorID).stats.SetStatBlock(sb);
        GD.Print("Shit got changed");
    }

    /// <summary>
    /// for applying a large dictionary of stat changes to one or more actors
    /// </summary>
    /// <param name="statchanges"></param>
    public void ApplyAllStatChanges(Dictionary<int, Dictionary<StatType, float>> statchanges)
    {
        foreach (int ActorID in statchanges.Keys)
        {
            StatBlock sb = GetStatBlock(ActorID);
            sb.ApplyAllChanges(statchanges[ActorID]);
        }
    }

    /// <summary>
    /// this ships the accumulated stat change dictionary to the clients
    /// </summary>
    public void SendCachedStatData()
    {
        if (StatChanges.Count < 1)
        {
            return;
        }

        foreach (long peerID in StatSubscription.Keys)
        {
            Dictionary<int, Dictionary<StatType, float>> changes = new();
            foreach (int ActorID in StatSubscription[peerID])
            {
                if (StatChanges.ContainsKey(ActorID))
                {
                    changes[ActorID] = StatChanges[ActorID];
                }
            }
            RpcId(peerID, "UpdateClientStatChange", JsonConvert.SerializeObject(changes));
        }

        StatChanges = new Dictionary<int, Dictionary<StatType, float>>();
    }

    /// <summary>
    /// This takes in a jstring of a list of StatProperties describing stat changes
    /// </summary>
    /// <param name="jstatchange">serialized Dictionary<int, Dictionary<StatType, float>>, describing an ActorID and a Dictionary<StatType, float> of stat changes</param>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void UpdateClientStatChange(string jstatchange)
    {
        Dictionary<int, Dictionary<StatType, float>> StatChanges = JsonConvert.DeserializeObject<Dictionary<int, Dictionary<StatType, float>>>(jstatchange);
        ApplyAllStatChanges(StatChanges);
    }
}
