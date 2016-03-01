using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace RoslynSolutionTest {
    class Program {
        public static Solution RemoveAttributes( Solution solution ) {
            foreach ( var doc in solution.Projects.SelectMany( p => p.Documents ).Where( d => d.SupportsSyntaxTree ) ) {
                var tree = doc.GetSyntaxTreeAsync().Result;

                var newDoc = doc.WithSyntaxRoot( new RemoveAttributesVisitor( new[] { "Flags" } ).Visit( tree.GetRoot() ) );
                File.WriteAllText( newDoc.FilePath, newDoc.GetTextAsync().Result.ToString() );

                solution = newDoc.Project.Solution;

            }

            return solution;
        }

        static void Main( string[] args ) {

            var ws = MSBuildWorkspace.Create();
            Solution solution;

            Console.WriteLine( "Opening " + args[0] );
            if ( args[0].EndsWith( ".csproj" ) ) {
                var proj = ws.OpenProjectAsync( args[0] ).Result;
                solution = proj.Solution;
            }
            else {
                solution = ws.OpenSolutionAsync( args[0] ).Result;
            }

            solution = RemoveAttributes( solution );

            //foreach ( var doc in solution.Projects.SelectMany( p => p.Documents ).Where( d => d.SupportsSyntaxTree ) ) {
            //    File.WriteAllText( doc.FilePath, doc.GetTextAsync().Result.ToString() );
            //}

                //foreach ( var document in solution.Projects.SelectMany( p => p.Documents.Where( d => d.SupportsSemanticModel ) ) ) {
                //    Console.WriteLine( "Checking " + document.FilePath );
                //    var model = document.GetSemanticModelAsync().Result;
                //    var diag = model.GetDiagnostics();
                //    if ( diag.Any( d => d.Severity == DiagnosticSeverity.Error ) ) {
                //        throw new Exception( string.Join( "\n", diag.Where( e => e.Severity == DiagnosticSeverity.Error ) ) );
                //    }
                //}

                //Console.WriteLine( "No errors encountered" );
            }
    }
}
