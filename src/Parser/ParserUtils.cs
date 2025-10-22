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


    /// <summary>
    /// Used in the turtle and sparql parsers to get the prefix string without the trailing colon.
    /// </summary>
    /// <param name="prefixNs">A prefix name from the antlr parser, f.ex. "ex:"</param>
    /// <returns>Without colon, f.ex. "ex"</returns>
    /// <exception cref="Exception"></exception>
    public static string GetStringExcludingLastColon(string prefixNs)
    {
        if (prefixNs.Length >= 1 && prefixNs[^1] == ':')
        {
            return prefixNs.Substring(0, prefixNs.Length - 1);
        }
        throw new Exception($"Invalid prefix {prefixNs}. Prefix should end with ':'");
    }


    /// <summary>
    /// Used in the sparql parser to get the variable name without leading question mark.
    /// </summary>
    /// <param name="variable">A variable names e.g. "?var"</param>
    /// <returns>Without question mark, f.ex. "var"</returns>
    public static string GetVariableName(string variable)
    {
        if (variable.Length >= 1 && variable[0] == '?')
        {
            return variable.Substring(1);
        }
        throw new Exception($"Invalid variable {variable}. Variable should start with '?'");
    }
    
    
}