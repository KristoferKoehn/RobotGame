using Godot;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using MMOTest.Backend;
using System;
using MMOTest.scripts.Managers;

public partial class RNGSingleton : Node
{
	
	static RNGSingleton instance = null;
	private RandomNumberGenerator rng = new RandomNumberGenerator();

	private RNGSingleton() 
	{
		
	}

	public static RNGSingleton GetInstance()
	{
		if (instance == null) {
			instance = new RNGSingleton();
            GameLoop.Root.GetNode<MainLevel>("GameLoop/MainLevel").AddChild(instance);
            instance.Name = "RNGSingleton";
        }
		return instance;
	}

    public float GetRandomNumber()
    {
        return (float)rng.Randi();
    }

	

}
