using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;

namespace RoslynSolutionTest {
    class Program {
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

            foreach ( var document in solution.Projects.SelectMany( p => p.Documents.Where( d => d.SupportsSemanticModel ) ) ) {
                Console.WriteLine( "Checking " + document.FilePath );
                var model = document.GetSemanticModelAsync().Result;
                var diag = model.GetDiagnostics();
                if ( diag.Any( d => d.Severity == DiagnosticSeverity.Error ) ) {
                    throw new Exception( string.Join( "\n", diag.Where( e => e.Severity == DiagnosticSeverity.Error ) ) );
                }
            }

            Console.WriteLine( "No errors encountered" );
        }
    }
}
