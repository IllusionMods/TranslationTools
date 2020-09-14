using System;

[Flags]
public enum LoadOptions
{
    None = 0,
    LoadNames = 0b1,
    Replace = 0b10,
    Dump = 0b100
}


public static class EnumExtensions
{
#if KK || HS || PH
    public static bool HasFlag(this Enum obj, Enum flag)
    {
        // check if from the same type.
        if (obj.GetType() != flag.GetType())
        {
            throw new ArgumentException("flag is a different type than the current instance.");
        }

        var iFlag = Convert.ToUInt64(flag);

        return (Convert.ToUInt64(obj) & iFlag) == iFlag;
    }
#endif
}
