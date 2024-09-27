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
    public const string Rdf = "http://www.w3.org/1999/02/22-rdf-syntax-ns#";
    public const string Rdfs = "http://www.w3.org/2000/01/rdf-schema#";
    public const string Owl = "http://www.w3.org/2002/07/owl#";
    public const string Xsd = "http://www.w3.org/2001/XMLSchema#";

    public const string RdfType = $"{Rdf}type";
    public const string RdfNil = $"{Rdf}nil";
    public const string RdfFirst = $"{Rdf}first";
    public const string RdfRest = $"{Rdf}rest";

    public const string XsdString = $"{Xsd}string";
    public const string XsdBoolean = $"{Xsd}boolean";
    public const string XsdDecimal = $"{Xsd}decimal";
    public const string XsdFloat = $"{Xsd}float";
    public const string XsdDouble = $"{Xsd}double";
    public const string XsdDuration = $"{Xsd}duration";
    public const string XsdDateTime = $"{Xsd}dateTime";
    public const string XsdTime = $"{Xsd}time";
    public const string XsdDate = $"{Xsd}date";
    public const string XsdInt = $"{Xsd}int";
    public const string XsdInteger = $"{Xsd}integer";
    public const string XsdHexBinary = $"{Xsd}hexBinary";
    public const string XsdBase64Binary = $"{Xsd}base64Binary";
    public const string XsdAnyUri = $"{Xsd}anyURI";

}