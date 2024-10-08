grammar TurtleDoc;
import TurtleResource;

turtleDoc : statement* EOF;

statement: directive | triples PERIOD;

