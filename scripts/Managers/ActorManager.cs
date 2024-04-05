using Godot;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using MMOTest.Backend;
using System;
using MMOTest.scripts.Managers;

public partial class ActorManager : Node
{
	private bool host;

	public Dictionary<long, Actor> actors = new Dictionary<long, Actor>();
	static ActorManager instance = null;


	private ActorManager() 
	{
		
	}

	public static ActorManager GetInstance()
	{
		if (instance == null) {
			instance = new ActorManager();
            GameLoop.Root.GetNode<MainLevel>("GameLoop/MainLevel").AddChild(instance);
            instance.Name = "ActorManager";
        }
		return instance;
	}

	public void ApplyHost(bool host)
	{
		this.host = host;
	}

	public void CreateActor(long PeerID, int ActorID, Dictionary<StatType, float> stats)
	{
		Actor actor = new Actor();
		actor.ActorID = ActorID;
		actor.ActorMultiplayerAuthority = PeerID;
		actor.stats = new StatBlock();
		actor.stats.SetStatBlock(stats);
		actors[ActorID] = actor;
	}

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    public void CreateActor(long PeerID, int ActorID)
    {
        Actor actor = new Actor();
        actor.ActorID = ActorID;
        actor.ActorMultiplayerAuthority = PeerID;
        actor.stats = new StatBlock();
        actors[ActorID] = actor;

		if (Multiplayer.GetUniqueId() > 1)
		{
			SceneOrganizerManager.GetInstance().GetCurrentLevel().GetNode<PlayerController>("PlayerController").InitializeUI(ActorID);
		}

    }

    public void CreateActor(AbstractModel player, AbstractModel puppet, long PeerID, int ActorID)
	{
		Actor actor = new Actor();
		actor.ClientModelReference = player;
		actor.PuppetModelReference = puppet;
		actor.ActorMultiplayerAuthority = PeerID;
		actor.ActorID = ActorID;
        // generic stat block
		StatBlock statBlock = new StatBlock();
		Dictionary<StatType, float> statsDict = new()
		{
			[StatType.HEALTH] = 100,
			[StatType.MAX_HEALTH] = 100,
			[StatType.MANA] = 100,
			[StatType.MAGIC_RESIST] = 13,
			[StatType.ARMOR] = 11,
			[StatType.ABILITY_POINTS] = 14,
			[StatType.CASTING_SPEED] = 12,
			[StatType.PHYSICAL_DAMAGE] = 15,
        };
		
		if(actors.Count % 2 == 0)
		{
			statsDict[StatType.CTF_TEAM] = (float)Teams.BLUE_TEAM;
        } else
		{
            statsDict[StatType.CTF_TEAM] = (float)Teams.RED_TEAM;
        }

        statBlock.SetStatBlock(statsDict);
		actor.stats = statBlock;

        actors.Add(ActorID, actor);
		StatManager.GetInstance().SubscribePeerToActor(PeerID, ActorID);

	}

	public void RemoveActor(int ActorID)
	{
		long peerID = GetActor(ActorID).ActorMultiplayerAuthority;
		actors.Remove(ActorID);
		StatManager.GetInstance().PeerLogoutSubscriptionDisconnect(peerID, ActorID);

	}

	public Actor GetActor(int ActorID)
	{
		if (actors.TryGetValue(ActorID, out Actor actor))
		{
			return actor;
		}
		return null;
	}

    public List<Actor> GetActorsFromPeerID(long PeerID)
    {
		List<Actor> actorsList = new List<Actor>();

        foreach (Actor actor in this.actors.Values)
		{
			if (actor.ActorMultiplayerAuthority == PeerID)
			{
				actorsList.Add(actor);
			}
		}
		return actorsList;
    }

}
