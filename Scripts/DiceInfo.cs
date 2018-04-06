using System;
[Serializable]
public class DiceInfo
{
    public DiceTypes Type;
    public int Value;
    public enum DiceTypes
    {
        D4,
        D6,
        D8,
        D10,
        D12,
        D20,
        D90
    };
}
