/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

using IriTools;

namespace AlcTableau.TurtleAntlr;

/// <summary>
/// Namespaces and IRIs used in the Turtle language.
/// </summary>
public static class Namespaces
{
    internal const string Rdf = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
    internal const string Rdfs = "http://www.w3.org/2000/01/rdf-schema#";
    internal const string Owl = "http://www.w3.org/2002/07/owl#";
    internal const string Xsd = "http://www.w3.org/2001/XMLSchema#";

    internal const string RdfType = $"{Rdf}type";

    internal const string XsdString = $"{Xsd}string";
    internal const string XsdBoolean = $"{Xsd}boolean";
    internal const string XsdDecimal = $"{Xsd}decimal";
    internal const string XsdFloat = $"{Xsd}float";
    internal const string XsdDouble = $"{Xsd}double";
    internal const string XsdDuration = $"{Xsd}duration";
    internal const string XsdDateTime = $"{Xsd}dateTime";
    internal const string XsdTime = $"{Xsd}time";
    internal const string XsdDate = $"{Xsd}date";
    internal const string XsdInt = $"{Xsd}int";
    internal const string XsdInteger = $"{Xsd}integer";
    internal const string XsdHexBinary = $"{Xsd}hexBinary";
    internal const string XsdBase64Binary = $"{Xsd}base64Binary";
    internal const string XsdAnyUri = $"{Xsd}anyURI";

}