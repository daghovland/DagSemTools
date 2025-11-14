/*
    Copyright (C) 2024 Dag Hovland
    This program is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
    This program is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
    You should have received a copy of the GNU General Public License along with this program. If not, see <https://www.gnu.org/licenses/>.
    Contact: hovlanddag@gmail.com
*/

namespace DagSemTools.Api;

/// <summary>
/// Represents a blank node resource.
/// </summary>
public class BlankNodeResource(string name) : Resource
{
    private readonly string _name = name;

    /// <summary>
    /// Blank nodes are equal if they have the same name.
    /// This comparison of course only makes sense inside a single RDF graph.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public override bool Equals(GraphElement? other) =>
        other != null && (ReferenceEquals(this, other) || other is BlankNodeResource bnr && bnr._name == _name);

    /// <summary>
    /// Blank nodes are equal if they have the same name.
    /// This comparison of course only makes sense inside a single RDF graph.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public override bool Equals(object? other) =>
        other != null && other is GraphElement bnr && Equals(bnr);

    /// <inheritdoc />
    public override string ToString() => $"_:{_name}";

    /// <summary>
    /// The hash code is made from the blank node name. 
    /// </summary>
    /// <returns></returns>
    public override int GetHashCode() => _name.GetHashCode();
}