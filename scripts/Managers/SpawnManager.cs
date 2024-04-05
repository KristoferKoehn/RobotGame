using Godot;
using MMOTest.Backend;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
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
                case "Mage":
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
                case "Warrior":
                    statsDict = new()
                    {
                        [StatType.HEALTH] = 120,
                        [StatType.MAX_HEALTH] = 120,
                        [StatType.MANA] = 30,
                        [StatType.MAGIC_RESIST] = 13,
                        [StatType.ARMOR] = 20,
                        [StatType.ABILITY_POINTS] = 4,
                        [StatType.CASTING_SPEED] = 12,
                        [StatType.PHYSICAL_DAMAGE] = 35,
                    };
                    //set models
                    //push updated stats to client
                    //tell client to update model
                    break;
                case "Barbarian":
                    statsDict = new()
                    {
                        [StatType.HEALTH] = 110,
                        [StatType.MAX_HEALTH] = 110,
                        [StatType.MANA] = 0,
                        [StatType.MAGIC_RESIST] = 3,
                        [StatType.ARMOR] = 16,
                        [StatType.ABILITY_POINTS] = 0,
                        [StatType.CASTING_SPEED] = 12,
                        [StatType.PHYSICAL_DAMAGE] = 40,
                    };
                    //set models
                    //push updated stats to client
                    //tell client to update model
                    break;
                case "Druid":
                    statsDict = new()
                    {
                        [StatType.HEALTH] = 110,
                        [StatType.MAX_HEALTH] = 110,
                        [StatType.MANA] = 200,
                        [StatType.MAGIC_RESIST] = 15,
                        [StatType.ARMOR] = 7,
                        [StatType.ABILITY_POINTS] = 16,
                        [StatType.CASTING_SPEED] = 10,
                        [StatType.PHYSICAL_DAMAGE] = 10,
                    };
                    //set models
                    //push updated stats to client
                    //tell client to update model
                    break;
                case "Rogue":
                    statsDict = new()
                    {
                        [StatType.HEALTH] = 85,
                        [StatType.MAX_HEALTH] = 85,
                        [StatType.MANA] = 50,
                        [StatType.MAGIC_RESIST] = 10,
                        [StatType.ARMOR] = 12,
                        [StatType.ABILITY_POINTS] = 3,
                        [StatType.CASTING_SPEED] = 12,
                        [StatType.PHYSICAL_DAMAGE] = 45,
                    };
                    //set models
                    //push updated stats to client
                    //tell client to update model
                    break;
                case "Necromancer":
                    statsDict = new()
                    {
                        [StatType.HEALTH] = 100,
                        [StatType.MAX_HEALTH] = 100,
                        [StatType.MANA] = 150,
                        [StatType.MAGIC_RESIST] = 34,
                        [StatType.ARMOR] = 1,
                        [StatType.ABILITY_POINTS] = 35,
                        [StatType.CASTING_SPEED] = 15,
                        [StatType.PHYSICAL_DAMAGE] = 1,
                    };
                    //set models
                    //push updated stats to client
                    //tell client to update model
                    break;
                case "Engineer":
                    statsDict = new()
                    {
                        [StatType.HEALTH] = 100,
                        [StatType.MAX_HEALTH] = 100,
                        [StatType.MANA] = 100,
                        [StatType.MAGIC_RESIST] = 13,
                        [StatType.ARMOR] = 11,
                        [StatType.ABILITY_POINTS] = 14,
                        [StatType.CASTING_SPEED] = 8,
                        [StatType.PHYSICAL_DAMAGE] = 15,
                    };
                    //set models
                    //push updated stats to client
                    //tell client to update model
                    break;
                case "Pete":
                    statsDict = new()
                    {
                        [StatType.HEALTH] = 400,
                        [StatType.MAX_HEALTH] = 400,
                        [StatType.MANA] = 400,
                        [StatType.MAGIC_RESIST] = 50,
                        [StatType.ARMOR] = 50,
                        [StatType.ABILITY_POINTS] = 50,
                        [StatType.CASTING_SPEED] = 1,
                        [StatType.PHYSICAL_DAMAGE] = 50,
                    };//set models
                    //push updated stats to client
                    //tell client to update model
                    break;
                case "Skeleton":
                    statsDict = new()
                    {
                        [StatType.HEALTH] = 35,
                        [StatType.MAX_HEALTH] = 35,
                        [StatType.MANA] = 0,
                        [StatType.MAGIC_RESIST] = 8,
                        [StatType.ARMOR] = 8,
                        [StatType.ABILITY_POINTS] = 8,
                        [StatType.CASTING_SPEED] = 17,
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
            ModelManager.GetInstance().ChangeActorModel(ActorID, classname);

            //no teams by default, change this later somehow
            Vector3 spawnPosition = GetValidSpawnPosition(Teams.NO_TEAM);

            Actor a = ActorManager.GetInstance().GetActor(ActorID);

            //move model to position, controller should follow automatically
            a.ClientModelReference.RpcId(a.ActorMultiplayerAuthority, "MovePlayerToPosition", spawnPosition);
        }



        //get input from player
        //set stat block -full health, stats and stuff
        //push stat block to client
        //assign new model
        //tell client it has new model - connect controller
        //put player in spawn place

    }
}
