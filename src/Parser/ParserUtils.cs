namespace DagSemTools.Parser;


/// <summary>
/// 
/// </summary>
public class ParserUtils
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="iriToken"></param>
    /// <returns></returns>
    public static string TrimIri(string iriToken) =>
        iriToken.Trim().Trim('<', '>');
}