using Godot;
using System.Collections.Generic;


namespace MMOTest.scripts.Managers
{
    public partial class SceneOrganizerManager : Node
    {

        private static SceneOrganizerManager instance = null;
        public Node3D CurrentLevel = null;
        List<string> sceneNames = new List<string>()
        {

        };

        private SceneOrganizerManager() { }
        public static SceneOrganizerManager GetInstance()
        {
            if (instance == null)
            {
                instance = new SceneOrganizerManager();
                GameLoop.Root.GetNode<GameLoop>("GameLoop").AddChild(instance);
                instance.Name = "SceneOrganizerManager";
            }

            return instance;
        }

        public override void _Ready()
        {
            foreach (string sceneName in sceneNames)
            {
                ResourceLoader.Load<PackedScene>(sceneName, cacheMode: ResourceLoader.CacheMode.Reuse);
            }
        }


        public void SetCurrentLevel(Node3D MainLevel)
        {
            CurrentLevel = MainLevel;
        }

        public Node3D GetCurrentLevel()
        {
            return CurrentLevel;
        }

    }
}
