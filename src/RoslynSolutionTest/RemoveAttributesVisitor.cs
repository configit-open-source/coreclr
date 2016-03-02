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

        //public override bool VisitIntoStructuredTrivia => false;

        private readonly HashSet<string> _keepAttrs = new HashSet<string>();

        public RemoveAttributesVisitor( IEnumerable<string> keepAttrs ) {
            foreach ( var i in keepAttrs ) {
                _keepAttrs.Add( i );
            }
        }

        // There is no base type or interface that can provide WithAttributes and AttributeLists....
        private T CleanAttributes<T>(
            T node,
            Func<T, SyntaxList<AttributeListSyntax>> attributeLists,
            Func<T, SyntaxList<AttributeListSyntax>, T> withAttributes ) where T : SyntaxNode {
            if ( !attributeLists( node ).Any() ) {
                return node;
            }

            var attrLists = attributeLists( node ).Select( attrList =>
                SyntaxFactory.AttributeList( new SeparatedSyntaxList<AttributeSyntax>().AddRange(
                    attrList.Attributes.Where( a => _keepAttrs.Contains( a.ToString() ) ) ) ) );

            node = withAttributes( node, new SyntaxList<AttributeListSyntax>().AddRange( attrLists.Where( al => al.Attributes.Any() ) ) );
            return node;
        }


        public override SyntaxNode VisitAttributeList( AttributeListSyntax node ) {
            Debug.Assert( node.Parent == null || node.Parent.IsKind( SyntaxKind.CompilationUnit ) );
            return node;
        }

        public override SyntaxNode VisitMethodDeclaration( MethodDeclarationSyntax node ) {
            return CleanAttributes( node, n => n.AttributeLists, ( n, a ) => n.WithAttributeLists( a ) );
        }

        public override SyntaxNode VisitPropertyDeclaration( PropertyDeclarationSyntax node ) {
            return CleanAttributes( node, n => n.AttributeLists, ( n, a ) => n.WithAttributeLists( a ) );
        }

        public override SyntaxNode VisitFieldDeclaration( FieldDeclarationSyntax node ) {
            return CleanAttributes( node, n => n.AttributeLists, ( n, a ) => n.WithAttributeLists( a ) );
        }

        public override SyntaxNode VisitDelegateDeclaration( DelegateDeclarationSyntax node ) {
            return CleanAttributes( node, n => n.AttributeLists, ( n, a ) => n.WithAttributeLists( a ) );
        }

        public override SyntaxNode VisitConstructorDeclaration( ConstructorDeclarationSyntax node ) {
            return CleanAttributes( node, n => n.AttributeLists, ( n, a ) => n.WithAttributeLists( a ) );
        }

        public override SyntaxNode VisitDestructorDeclaration( DestructorDeclarationSyntax node ) {
            return CleanAttributes( node, n => n.AttributeLists, ( n, a ) => n.WithAttributeLists( a ) );
        }

        public override SyntaxNode VisitConversionOperatorDeclaration( ConversionOperatorDeclarationSyntax node ) {
            return CleanAttributes( node, n => n.AttributeLists, ( n, a ) => n.WithAttributeLists( a ) );
        }

        public override SyntaxNode VisitOperatorDeclaration( OperatorDeclarationSyntax node ) {
            return CleanAttributes( node, n => n.AttributeLists, ( n, a ) => n.WithAttributeLists( a ) );
        }

        public override SyntaxNode VisitIndexerDeclaration( IndexerDeclarationSyntax node ) {
            return CleanAttributes( node, n => n.AttributeLists, ( n, a ) => n.WithAttributeLists( a ) );
        }

        public override SyntaxNode VisitEventDeclaration( EventDeclarationSyntax node ) {
            return CleanAttributes( node, n => n.AttributeLists, ( n, a ) => n.WithAttributeLists( a ) );
        }

        public override SyntaxNode VisitEventFieldDeclaration( EventFieldDeclarationSyntax node ) {
            return CleanAttributes( node, n => n.AttributeLists, ( n, a ) => n.WithAttributeLists( a ) );
        }

        public override SyntaxNode VisitEnumMemberDeclaration( EnumMemberDeclarationSyntax node ) {
            return CleanAttributes( node, n => n.AttributeLists, ( n, a ) => n.WithAttributeLists( a ) );
        }

        public override SyntaxNode VisitEnumDeclaration( EnumDeclarationSyntax node ) {
            var enumDecl = CleanAttributes( node, n => n.AttributeLists, ( n, a ) => n.WithAttributeLists( a ) );

            var result = enumDecl.WithMembers( new SeparatedSyntaxList<EnumMemberDeclarationSyntax>().AddRange(
                enumDecl.Members.Select( m => (EnumMemberDeclarationSyntax) VisitEnumMemberDeclaration( m ) ) ) );
            
            return result;
        }

        public override SyntaxNode VisitClassDeclaration( ClassDeclarationSyntax node ) {
            var classDecl = CleanAttributes( node, n => n.AttributeLists, ( n, a ) => n.WithAttributeLists( a ) );

            return base.VisitClassDeclaration( classDecl );
        }

        public override SyntaxNode VisitStructDeclaration( StructDeclarationSyntax node ) {
            var structDecl = CleanAttributes( node, n => n.AttributeLists, ( n, a ) => n.WithAttributeLists( a ) );

            return base.VisitStructDeclaration( structDecl );
        }

        public override SyntaxNode VisitInterfaceDeclaration( InterfaceDeclarationSyntax node ) {
            var decl = CleanAttributes( node, n => n.AttributeLists, ( n, a ) => n.WithAttributeLists( a ) );

            return base.VisitInterfaceDeclaration( decl );
        }
    }
}
