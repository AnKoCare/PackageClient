using System.Collections.Generic;
using TW.Utility.DesignPattern;

public delegate void TrackEventFirebase(string eventName, params AnalyticsParameter[] parameters);
public delegate void TrackEventAppsflyer(string eventName, params AnalyticsParameter[] parameters);

public class EventTrackingManager : Singleton<EventTrackingManager>
{
    public static TrackEventFirebase TrackEventFirebase;
    public static TrackEventAppsflyer TrackEventAppsflyer;
}

public class AnalyticsParameter
{
    public string Name { get; set; }

    public int ValueType { get; set; }
    public string StringValue { get; set; }
    public long LongValue { get; set; }
    public double DoubleValue { get; set; }
    public IDictionary<string, object> DicValue { get; set; }
    public IEnumerable<IDictionary<string, object>> EnumerableValue { get; set; }
    public AnalyticsParameter(string parameterName, string parameterValue)
    {
        this.Name = parameterName;
        this.ValueType = 0;
        this.StringValue = parameterValue;
    }

    public AnalyticsParameter(string parameterName, long parameterValue)
    {
        this.Name = parameterName;
        this.ValueType = 1;
        this.LongValue = parameterValue;
    }

    public AnalyticsParameter(string parameterName, double parameterValue)
    {
        this.Name = parameterName;
        this.ValueType = 2;
        this.DoubleValue = parameterValue;
    }

    public AnalyticsParameter(string parameterName, IDictionary<string, object> parameterValue)
    {
        this.Name = parameterName;
        this.ValueType = 3;
        this.DicValue = parameterValue;
    }

    public AnalyticsParameter(
        string parameterName,
        IEnumerable<IDictionary<string, object>> parameterValue)
    {
        this.Name = parameterName;
        this.ValueType = 4;
        this.EnumerableValue = parameterValue;
    }
}