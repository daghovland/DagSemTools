# Datalog

This library can apply stratifiable datalog rules on an Rdf graph.

For stratification, the algorithm from the Alice book is used. 

For rules with variables on the predicate place in the head, we assume all other relations depend on that rule. 
    