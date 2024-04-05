using Godot;
using System;

public partial class AccountDataManager : Node
{

    static AccountDataManager instance = null;


    private AccountDataManager()
    {

    }

    public static AccountDataManager GetInstance()
    {
        if (instance == null)
        {
            instance = new AccountDataManager();
            GameLoop.Root.GetNode<MainLevel>("GameLoop/MainLevel").AddChild(instance);
            instance.Name = "AccountDataManager";
        }
        return instance;
    }

    public void AssignActorData(int ActorID)
    {

    }
}
