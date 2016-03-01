using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace RoslynSolutionTest {

    internal class RemoveAttributesVisitor: CSharpSyntaxRewriter {

        public override bool VisitIntoStructuredTrivia => false;

        private readonly HashSet<string> _keepAttrs = new HashSet<string>();

        public RemoveAttributesVisitor( IEnumerable<string> keepAttrs ) {
            foreach ( var i in keepAttrs ) {
                _keepAttrs.Add( i );
            }
        }

        // There is no base type or interface that can provide WithAttributes and AttributeLists....
        private T ClearAttributesAndMoveTrivia<T>(
            T node,
            Func<T, SyntaxList<AttributeListSyntax>> attributeLists,
            Func<T, SyntaxList<AttributeListSyntax>, T> withAttributes ) where T : SyntaxNode {
            if ( !attributeLists( node ).Any() ) {
                // TODO: Temp to avoid messing up some cases of #if / #endif
                //return node.WithTrailingTrivia( node.GetTrailingTrivia().Add( SyntaxFactory.CarriageReturnLineFeed ) );
                return node;
            }

            //var leadingTrivia = attributeLists( node ).SelectMany( al =>
            //SyntaxFactory.TriviaList( SyntaxFactory.CarriageReturnLineFeed ).AddRange(
            //    al.GetLeadingTrivia().Add( SyntaxFactory.CarriageReturnLineFeed ) ) );

            var attrLists = attributeLists( node ).Select( attrList =>
                SyntaxFactory.AttributeList( new SeparatedSyntaxList<AttributeSyntax>().AddRange(
                    attrList.Attributes.Where( a => _keepAttrs.Contains( a.ToString() ) ) ) ) );

            node = withAttributes( node, new SyntaxList<AttributeListSyntax>().AddRange( attrLists.Where( al => al.Attributes.Any() ) ) );

            //var updatedTrivia = SyntaxFactory.TriviaList( leadingTrivia ).AddRange( node.GetLeadingTrivia() );

            //// TODO: Temp to avoid messing up some cases of #if / #endif
            //var clearAttributesAndMoveTrivia = node.WithLeadingTrivia( updatedTrivia ).WithTrailingTrivia( node.GetTrailingTrivia().Add( SyntaxFactory.CarriageReturnLineFeed ) );
            //return clearAttributesAndMoveTrivia;
            return node;
        }

        public override SyntaxNode Visit( SyntaxNode node ) {
            return base.Visit( node.WithoutTrivia().NormalizeWhitespace() );
        }

        public override SyntaxNode VisitAttributeList( AttributeListSyntax node ) {
            Debug.Assert( node.Parent.IsKind( SyntaxKind.CompilationUnit ) );
            return node;
        }

        public override SyntaxNode VisitMethodDeclaration( MethodDeclarationSyntax node ) {
            return ClearAttributesAndMoveTrivia( node, n => n.AttributeLists, ( n, a ) => n.WithAttributeLists( a ) );
        }

        public override SyntaxNode VisitPropertyDeclaration( PropertyDeclarationSyntax node ) {
            return ClearAttributesAndMoveTrivia( node, n => n.AttributeLists, ( n, a ) => n.WithAttributeLists( a ) );
        }

        public override SyntaxNode VisitFieldDeclaration( FieldDeclarationSyntax node ) {
            return ClearAttributesAndMoveTrivia( node, n => n.AttributeLists, ( n, a ) => n.WithAttributeLists( a ) );
        }

        public override SyntaxNode VisitDelegateDeclaration( DelegateDeclarationSyntax node ) {
            return ClearAttributesAndMoveTrivia( node, n => n.AttributeLists, ( n, a ) => n.WithAttributeLists( a ) );
        }

        public override SyntaxNode VisitConstructorDeclaration( ConstructorDeclarationSyntax node ) {
            return ClearAttributesAndMoveTrivia( node, n => n.AttributeLists, ( n, a ) => n.WithAttributeLists( a ) );
        }

        public override SyntaxNode VisitDestructorDeclaration( DestructorDeclarationSyntax node ) {
            return ClearAttributesAndMoveTrivia( node, n => n.AttributeLists, ( n, a ) => n.WithAttributeLists( a ) );
        }

        public override SyntaxNode VisitConversionOperatorDeclaration( ConversionOperatorDeclarationSyntax node ) {
            return ClearAttributesAndMoveTrivia( node, n => n.AttributeLists, ( n, a ) => n.WithAttributeLists( a ) );
        }

        public override SyntaxNode VisitOperatorDeclaration( OperatorDeclarationSyntax node ) {
            return ClearAttributesAndMoveTrivia( node, n => n.AttributeLists, ( n, a ) => n.WithAttributeLists( a ) );
        }

        public override SyntaxNode VisitIndexerDeclaration( IndexerDeclarationSyntax node ) {
            return ClearAttributesAndMoveTrivia( node, n => n.AttributeLists, ( n, a ) => n.WithAttributeLists( a ) );
        }

        public override SyntaxNode VisitEventDeclaration( EventDeclarationSyntax node ) {
            return ClearAttributesAndMoveTrivia( node, n => n.AttributeLists, ( n, a ) => n.WithAttributeLists( a ) );
        }

        public override SyntaxNode VisitEventFieldDeclaration( EventFieldDeclarationSyntax node ) {
            return ClearAttributesAndMoveTrivia( node, n => n.AttributeLists, ( n, a ) => n.WithAttributeLists( a ) );
        }

        public override SyntaxNode VisitEnumMemberDeclaration( EnumMemberDeclarationSyntax node ) {
            //Console.WriteLine( "Visit: " + node.ToFullString() );
            return ClearAttributesAndMoveTrivia( node, n => n.AttributeLists, ( n, a ) => n.WithAttributeLists( a ) );
        }

        public override SyntaxNode VisitEnumDeclaration( EnumDeclarationSyntax node ) {
            var enumDecl = ClearAttributesAndMoveTrivia( node, n => n.AttributeLists, ( n, a ) => n.WithAttributeLists( a ) );

            var result = enumDecl.WithMembers( new SeparatedSyntaxList<EnumMemberDeclarationSyntax>().AddRange(
                enumDecl.Members.Select( m => (EnumMemberDeclarationSyntax) VisitEnumMemberDeclaration( m ) ) ) );

            return result;
        }

        public override SyntaxNode VisitClassDeclaration( ClassDeclarationSyntax node ) {
            var classDecl = ClearAttributesAndMoveTrivia( node, n => n.AttributeLists, ( n, a ) => n.WithAttributeLists( a ) );

            return base.VisitClassDeclaration( classDecl );
        }

        public override SyntaxNode VisitStructDeclaration( StructDeclarationSyntax node ) {
            var structDecl = ClearAttributesAndMoveTrivia( node, n => n.AttributeLists, ( n, a ) => n.WithAttributeLists( a ) );

            return base.VisitStructDeclaration( structDecl );
        }

        public override SyntaxNode VisitInterfaceDeclaration( InterfaceDeclarationSyntax node ) {
            var decl = ClearAttributesAndMoveTrivia( node, n => n.AttributeLists, ( n, a ) => n.WithAttributeLists( a ) );

            return base.VisitInterfaceDeclaration( decl );
        }
    }
}
