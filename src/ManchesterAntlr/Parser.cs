/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using AlcTableau;
using AlcTableau.ManchesterAntlr;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using IriTools;

namespace ManchesterAntlr;

public static class Parser
{
    public static ALC.OntologyDocument ParseFile(string filename, IAntlrErrorListener<IToken>? errorListener = null)
    {
        using TextReader textReader = File.OpenText(filename);
        return ParseReader(textReader, errorListener);
    }
    
    public static ALC.OntologyDocument ParseReader(TextReader textReader, Dictionary<string, IriReference> prefixes, IAntlrErrorListener<IToken>? errorListener = null)
    {
        var input = new AntlrInputStream(textReader);
        var lexer = new ManchesterLexer(input);
        CommonTokenStream tokens = new CommonTokenStream(lexer);
        var parser = new ManchesterParser(tokens);
        IAntlrErrorListener<IToken> customErrorListener = new ConsoleErrorListener<IToken>();
        if (errorListener != null)
        {
            parser.RemoveErrorListeners();
            parser.AddErrorListener(errorListener);
            customErrorListener = errorListener;
        }

        IParseTree tree = parser.ontologyDocument();
        var visitor = new ManchesterVisitor(customErrorListener);
        return visitor.Visit(tree);
    }

    public static ALC.OntologyDocument ParseReader(TextReader textReader, IAntlrErrorListener<IToken>? errorListener = null) =>
        ParseReader(textReader, new Dictionary<string, IriReference>(), errorListener);

    public static ALC.OntologyDocument ParseString(string owl, IAntlrErrorListener<IToken>? errorListener = null)
    {
        using TextReader textReader = new StringReader(owl);
        return ParseReader(textReader, errorListener);
    }


}