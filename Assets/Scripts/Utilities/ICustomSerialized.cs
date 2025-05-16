using JToken = Newtonsoft.Json.Linq.JToken;

public interface ICustomSerialized
{

    /// <summary>
    /// Serializes the object into a JToken.
    /// <br />HEAVILY encouraged to create an implicit JToken operator redirecting to this for easier/faster conversion.
    /// </summary>
    /// <param name="name">Optional name for if serialized as a full Json Property</param>
    /// <returns>The Json representation.</returns>
    public JToken Serialize(string name = null);
    /// <summary>
    /// Deserializes a JToken and populates this object with its data.
    /// </summary>
    /// <param name="Data">The Json representation to be Deserialized.</param>
    public void Deserialize(JToken Data);

}