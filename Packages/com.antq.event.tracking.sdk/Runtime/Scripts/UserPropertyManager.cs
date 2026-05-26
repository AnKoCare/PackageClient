using TW.Utility.DesignPattern;

public delegate void SetUserPropertyFirebase(string propertyName, string property);

public class UserPropertyManager : Singleton<UserPropertyManager>
{
    public static SetUserPropertyFirebase SetUserPropertyFirebase;
}
