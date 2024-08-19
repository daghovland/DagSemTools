using AlcTableau;
using AlcTableau.ManchesterAntlr;
using IriTools;

namespace ManchesterAntlr;

public class RoleVisitor : ManchesterBaseVisitor<ALC.Role>
{
    private IriGrammarVisitor _iriGrammarVisitor;
    public RoleVisitor(IriGrammarVisitor iriGrammarVisitor)
    {
        _iriGrammarVisitor = iriGrammarVisitor;
    }

    public override ALC.Role VisitObjectPropertyExpression(ManchesterParser.ObjectPropertyExpressionContext context) =>
        context.INVERSE() == null
            ? ALC.Role.NewIri(new IriReference(_iriGrammarVisitor.Visit(context.rdfiri())))
            : ALC.Role.NewInverse(new IriReference(_iriGrammarVisitor.Visit(context.rdfiri())));
}