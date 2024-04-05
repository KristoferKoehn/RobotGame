using Godot;
using Newtonsoft.Json;
using System.Collections.Generic;

public enum StatType
{
    HEALTH,
    MAX_HEALTH,
    HEALTH_REGEN,

    STRENGTH,
    PHYSICAL_DAMAGE,
    ARMOR,

    DEXTERITY,
    ATTACK_SPEED,
    MOVE_SPEED,

    INTELLIGENCE,
    ABILITY_POINTS,
    MAGIC_RESIST,
    MANA,
    CASTING_SPEED,

    
    CTF_TEAM

}




namespace MMOTest.Backend
{

    public class StatProperty
    {
        public StatType StatType { get; set; }
        public float Value { get; set; }

        public StatProperty()
        {

        }

        public StatProperty(StatType statType, float statValue)
        {
            this.StatType = statType;
            this.Value = statValue;
        }
    }


    public class StatBlock
    {

        private Dictionary<StatType, float> statblock = new Dictionary<StatType, float>();
        //private JObject statblock = new JObject();
        public StatBlock() { }

        public void SetStat(StatType statType, float value)
        {
            statblock[statType] = value;
        }

        public float GetStat(StatType statType)
        {
            return statblock.ContainsKey(statType) ? statblock[statType] : 0f;
        }

        public void SetStatBlock(Dictionary<StatType, float> sb)
        {
            statblock = sb;
        }

        public string SerializeStatBlock()
        {
            return JsonConvert.SerializeObject(statblock);
        }

        public void ApplyAllChanges(Dictionary<StatType, float> sb)
        {
            foreach (StatType statType in sb.Keys)
            {
                
                statblock[statType] = GetStat(statType) + sb[statType];
                GD.Print(statType.ToString() + " set to " + statblock[statType]);

            }
        }
    }
}