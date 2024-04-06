using Godot;
using MMOTest.Backend;
using System.Collections.Generic;

namespace MMOTest.scripts.Managers
{
    public partial class DeathManager : Node
    {
        private static DeathManager instance = null;
        Dictionary<int, Actor> DeadActors = new Dictionary<int, Actor>();

        private DeathManager() { }

        public static DeathManager GetInstance()
        {
            if (instance == null)
            {
                instance = new DeathManager();
                GameLoop.Root.GetNode<MainLevel>("GameLoop/MainLevel").AddChild(instance);
                instance.Name = "DeathManager";
            }
            return instance;
        }

        /// <summary>
        /// this kills the actor
        /// </summary>
        /// <param name="actor"></param>
        public void AddActor(Actor actor)
        {
            //kill the actor here
            //set dead = true or something. Lock up the controls. dig a grave
            GD.Print("actor ddddied");

            DeadActors.Add(actor.ActorID, actor);


            // for auto respawning
            /*
            ActorTimer at = GD.Load<PackedScene>("res://scenes/utility/ActorTimer.tscn").Instantiate<ActorTimer>();
            at.ActorID = actor.ActorID;
            AddChild(at);
            actor.ClientModelReference.RpcId(actor.ActorMultiplayerAuthority, "AssignDeathState", true);
            at.Start(5);
            at.ActorTimerTimeout += RespawnActor;
            at.Timeout += at.QueueFree;
            */
        }

        public void RespawnActor(int ActorID)
        {
            GD.Print("respawning actor " + ActorID);
            Actor actor = DeadActors[ActorID];
            DeadActors.Remove(ActorID);

            //bring back the boy.
            actor.ClientModelReference.RpcId(actor.ActorMultiplayerAuthority, "AssignDeathState", false);
            SpawnManager.GetInstance().SpawnActor(ActorID);
        }

        public bool IsActorDead(int ActorID)
        {
            if (!DeadActors.ContainsKey(ActorID))
            {
                return false;
            }
            return true;
        }

        public bool IsActorDead(Actor actor)
        {
            if(DeadActors.ContainsKey(actor.ActorID)) { return true; }
            return false;
        }

    }
}
