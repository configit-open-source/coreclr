using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynSolutionTest {
    internal class TriviaRemover: CSharpSyntaxRewriter {
        public override bool VisitIntoStructuredTrivia => true;

        public override SyntaxNode Visit( SyntaxNode node ) {
            if ( node == null ) {
                return null;
            }
            var withoutTrivia = base.Visit( node ).WithoutTrivia();

            return withoutTrivia.Parent == null ?
                withoutTrivia.ReplaceTokens( withoutTrivia.DescendantTokens(),
                ( t0, t1 ) => t1.WithLeadingTrivia().WithTrailingTrivia() ).NormalizeWhitespace() : withoutTrivia;
        }

        //public override SyntaxNode VisitEndIfDirectiveTrivia( EndIfDirectiveTriviaSyntax node ) {
        //    return null;
        //}

        //public override SyntaxNode VisitElseDirectiveTrivia( ElseDirectiveTriviaSyntax node ) {
        //    return null;
        //}

        //public override SyntaxNode VisitElifDirectiveTrivia( ElifDirectiveTriviaSyntax node ) {
        //    return null;
        //}

        //public override SyntaxNode VisitIfDirectiveTrivia( IfDirectiveTriviaSyntax node ) {
        //    return null;
        //}
    }
}
