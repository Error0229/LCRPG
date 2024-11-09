public class Skill
{
    public string Description;
    public int Duration;
    public string EffectType;
    public int EffectValue;
    public float Probability;
    public int SkillID;
    public string SkillName;
    public SkillType SkillType;

    public Skill(int skillID, string skillName, string effectType, int effectValue, int duration, float probability,
        string description)
    {
        SkillID = skillID;
        SkillType = (SkillType)skillID;
        SkillName = skillName;
        EffectType = effectType;
        EffectValue = effectValue;
        Duration = duration;
        Probability = probability;
        Description = description;
    }
}