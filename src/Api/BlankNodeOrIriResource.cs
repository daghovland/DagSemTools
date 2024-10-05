namespace DagSemTools.Api;

/// <summary>
/// Represents a resource that is identified by an IRI or is a blank node.
/// This is for example the resources allowed in the subject position of a triple
/// (But not in the predicate position, where blank nodes are not allowed)
/// </summary>
public abstract class BlankNodeOrIriResource : Resource
{

}