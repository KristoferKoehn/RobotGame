using Godot;
using MMOTest.Backend;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace MMOTest.scripts.Managers
{
    public partial class SpawnManager : Node
    {

        List<SpawnArea> spawnAreas = new List<SpawnArea>();
        private static SpawnManager instance = null;

        private SpawnManager() { }

        public static SpawnManager GetInstance()
        {
            if (instance == null)
            {
                instance = new SpawnManager();
                GameLoop.Root.GetNode<MainLevel>("GameLoop/MainLevel").AddChild(instance);
                instance.Name = "SpawnManager";
            }
            return instance;
        }

        public void AddSpawnArea(SpawnArea spawnArea)
        {
            spawnAreas.Add(spawnArea);
        }

        public void RemoveSpawnArea(SpawnArea spawnArea)
        {
            spawnAreas.Remove(spawnArea);
        }

        public Vector3 GetValidSpawnPosition(Teams team)
        {
            RandomNumberGenerator rng = new RandomNumberGenerator();
            List<SpawnArea> validAreas = spawnAreas.Where(x => x.Team == team).ToList();
            int index = rng.RandiRange(0, validAreas.Count - 1);

            return validAreas[index].GetValidSpawnPoint();
        }

        public void SpawnActor(int ActorID)
        {
            Actor actor = ActorManager.GetInstance().GetActor(ActorID);
            StatBlock sb = actor.stats;
            AbstractModel model = actor.PuppetModelReference;
            float delta = sb.GetStat(StatType.MAX_HEALTH) - sb.GetStat(StatType.HEALTH);
            JObject b = new JObject
            {
                { "type", "statchange" },
                { "TargetID", ActorID },
                { "SourceID", 1 },
            };

            List<StatProperty> values = new List<StatProperty>
            {
                new StatProperty(StatType.HEALTH, delta)
            };

            b["stats"] = JsonConvert.SerializeObject(values);
            MessageQueue.GetInstance().AddMessage(b);
            //reach into client and turn off death

            actor.ClientModelReference.RpcId(actor.ActorMultiplayerAuthority, "AssignDeathState", false);

            Teams t = (Teams)sb.GetStat(StatType.CTF_TEAM);
            Vector3 spawnPosition = GetValidSpawnPosition(t);

            //move model to position, controller should follow automatically
            actor.ClientModelReference.RpcId(actor.ActorMultiplayerAuthority, "MovePlayerToPosition", spawnPosition);
        }

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        public void SpawnPlayer(string classname, int ActorID)
        {
            int PeerID = Multiplayer.GetRemoteSenderId();
            Dictionary<StatType, float> statsDict;
            switch (classname)
            {
                case "Test":
                    statsDict = new()
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
                    //set models
                    //push updated stats to client
                    //tell client to update model
                    break;
                default:
                    //default andy
                    statsDict = new()
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
                    break;


            }

            StatBlock sb = new StatBlock();
            sb.SetStatBlock(statsDict);

            StatManager.GetInstance().RpcId(PeerID, "AssignStatBlock", sb.SerializeStatBlock(), ActorID);
            StatManager.GetInstance().AssignStatBlock(sb.SerializeStatBlock(), ActorID);
            ModelManager.GetInstance().ChangeActorModel(ActorID, classname);

            //no teams by default, change this later somehow
            Vector3 spawnPosition = GetValidSpawnPosition(Teams.NO_TEAM);

            Actor a = ActorManager.GetInstance().GetActor(ActorID);
            StatManager.GetInstance().SubscribePeerToActor(PeerID, ActorID);
            //move model to position, controller should follow automatically
            a.ClientModelReference.RpcId(a.ActorMultiplayerAuthority, "MovePlayerToPosition", spawnPosition);

        }


        //get input from player
        //set stat block -full health, stats and stuff
        //push stat block to client
        //assign new model
        //tell client it has new model - connect controller
        //put player in spawn place


        public void SpawnEnemy(string values)
        {
            if(Multiplayer.GetUniqueId() > 1)
            {
                GD.Print("Wrong authority on spawn");
                return;
            }

            EnemyModel em = ResourceLoader.Load<PackedScene>("res://scenes/actorScenes/Models/EnemyModel.tscn").Instantiate<EnemyModel>();

            string[] vals = values.Split(',');

            em.Leg = int.Parse(vals[1]);
            em.Body = int.Parse(vals[2]);
            em.BackWeapon = int.Parse(vals[3]);
            em.HandWeapon = int.Parse(vals[4]);


            Actor a = new Actor();
            RandomNumberGenerator rng = new RandomNumberGenerator();
            int ActorID = (int)rng.Randi();
            while (ActorManager.GetInstance().GetActor(ActorID) != null)
            {
                ActorID = (int)rng.Randi();
            }
            a.ActorID = ActorID;

            em.ActorID = ActorID;

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
                [StatType.CTF_TEAM] = (int)Teams.PURPLE_TEAM,
            };
            StatBlock sb = new StatBlock();
            sb.SetStatBlock(statsDict);

            a.stats = sb;
            a.PuppetModelReference = em;


            EnemyController enemyController = ResourceLoader.Load<PackedScene>("res://scenes/actorScenes/Controllers/EnemyController.tscn").Instantiate<EnemyController>();



            SceneOrganizerManager.GetInstance().CurrentLevel.AddChild(enemyController);

            ActorManager.GetInstance().actors[ActorID] = a;

            StatManager.GetInstance().AssignStatBlock(sb.SerializeStatBlock(), ActorID);

            Vector3 spawnPosition = GetValidSpawnPosition((Teams)statsDict[StatType.CTF_TEAM]);
            enemyController.GlobalPosition = spawnPosition;
            enemyController.AttachModel(em);
            GD.Print(spawnPosition);

            SceneOrganizerManager.GetInstance().CurrentLevel.GetNode<Node>("PuppetModels").AddChild(em,forceReadableName:true);
            em.GlobalPosition = spawnPosition;

        }

    }
}
